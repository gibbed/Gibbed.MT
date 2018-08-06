/* Copyright (c) 2018 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gibbed.IO;
using Gibbed.MT.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;

namespace Gibbed.MT.Unpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = false;
            string currentProject = null;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "p|project=", "override current project", v => currentProject = v },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_arc [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPath = Path.GetFullPath(extras[0]);
            var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            var manager = ProjectData.Manager.Load(currentProject);
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var cryptoKey = manager.GetSetting("crypto_key", null);
            var archiveVersion = manager.GetSetting("archive_version", (ushort)7);
            var compressionScheme = manager.GetSetting("archive_compression_scheme", CompressionScheme.None);
            var sameSizeIsUncompressed = manager.GetSetting("archive_same_size_is_uncompressed", false);

            var knownFileTypes = new KnownFileTypes();

            if (manager.ActiveProject != null)
            {
                var knownFileTypesPath = Path.Combine(manager.ActiveProject.ListsPath, "archive file types.cfg");
                if (File.Exists(knownFileTypesPath) == true)
                {
                    knownFileTypes.Load(knownFileTypesPath);
                }
            }

            using (var input = File.OpenRead(inputPath))
            {
                var archive = new ArchiveFile(cryptoKey);
                archive.Deserialize(input);

                if (archive.Version != archiveVersion)
                {
                    Console.WriteLine("Warning: file version does not match project version");
                }

                long current = 0;
                long total = archive.Entries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var entry in archive.Entries)
                {
                    current++;

                    var entryName = entry.Name;

                    if (knownFileTypes.Contains(entry.TypeHash) == false)
                    {
                        entryName += ".UNK#" + entry.TypeHash.ToString("X8");
                    }
                    else
                    {
                        entryName += knownFileTypes.GetExtension(entry.TypeHash) ??
                                     "." + knownFileTypes.GetName(entry.TypeHash);
                    }

                    var entryPath = Path.Combine(outputPath, entryName);
                    if (overwriteFiles == false &&
                        File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine(
                            "[{0}/{1}] {2}",
                            current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                            total,
                            entryName);
                    }

                    input.Seek(entry.Offset, SeekOrigin.Begin);

                    var entryDirectory = Path.GetDirectoryName(entryPath);
                    if (entryDirectory != null)
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        input.Seek(entry.Offset, SeekOrigin.Begin);

                        Stream data, temp = null;
                        if (archive.IsEncrypted == true)
                        {
                            var blowfish = archive.GetBlowfish();
                            var inputBytes = input.ReadBytes((int)entry.CompressedSize);
                            blowfish.Decrypt(inputBytes, 0, inputBytes, 0, inputBytes.Length);
                            data = temp = new MemoryStream(inputBytes, false);
                        }
                        else
                        {
                            data = input;
                        }

                        if (compressionScheme == CompressionScheme.None ||
                            (sameSizeIsUncompressed == true && entry.CompressedSize == entry.UncompressedSize))
                        {
                            output.WriteFromStream(data, entry.CompressedSize);
                        }
                        else if (compressionScheme == CompressionScheme.Zlib ||
                                 compressionScheme == CompressionScheme.ZlibHeaderless)
                        {
                            var headerless = compressionScheme == CompressionScheme.ZlibHeaderless;
                            using (var zlib = new InflaterInputStream(data, new Inflater(headerless)))
                            {
                                zlib.IsStreamOwner = false;
                                output.WriteFromStream(zlib, entry.UncompressedSize);
                            }
                        }
                        else if (compressionScheme == CompressionScheme.XCompress)
                        {
                            var compressedBytes = data is MemoryStream
                                                      ? ((MemoryStream)data).GetBuffer()
                                                      : input.ReadBytes((int)entry.CompressedSize);
                            var uncompressedBytes = new byte[entry.UncompressedSize];

                            using (var context = new XCompression.DecompressionContext(0x8000))
                            {
                                var compressedSize = compressedBytes.Length;
                                var uncompressedSize = uncompressedBytes.Length;

                                var result = context.Decompress(
                                    compressedBytes,
                                    0,
                                    ref compressedSize,
                                    uncompressedBytes,
                                    0,
                                    ref uncompressedSize);
                                if (result != XCompression.ErrorCode.None)
                                {
                                    throw new InvalidOperationException();
                                }

                                if (uncompressedSize != uncompressedBytes.Length ||
                                    compressedSize != compressedBytes.Length)
                                {
                                    throw new InvalidOperationException();
                                }

                                output.WriteBytes(uncompressedBytes);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        if (temp != null)
                        {
                            temp.Dispose();
                        }
                    }
                }
            }
        }
    }
}
