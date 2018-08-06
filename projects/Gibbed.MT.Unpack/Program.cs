/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
using Gibbed.MT.FileFormats.Archive;
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
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                    },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                    },
                {
                    "h|help",
                    "show this message and exit",
                    v => showHelp = v != null
                    },
                {
                    "p|project=",
                    "override current project",
                    v => currentProject = v
                    },
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_rcf [output_dir]", GetExecutableName());
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

            var compressionScheme = manager.GetSetting("archive_compression_scheme", CompressionScheme.None);

            var kft = new KnownFileTypes();

            if (manager.ActiveProject != null)
            {
                var kftPath = Path.Combine(manager.ActiveProject.ListsPath, "archive_file_types.cfg");
                if (File.Exists(kftPath) == true)
                {
                    kft.Load(kftPath);
                }
            }

            using (var input = File.OpenRead(inputPath))
            {
                var archive = new ArchiveFile(manager.GetSetting("archive_unknown_flag_position", UnknownFlagPosition.Lower));
                archive.Deserialize(input);

                long current = 0;
                long total = archive.Entries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var entry in archive.Entries)
                {
                    current++;

                    var entryName = entry.Name;

                    if (kft.Contains(entry.TypeHash) == false)
                    {
                        entryName += ".UNK#" + entry.TypeHash.ToString("X8");
                    }
                    else
                    {
                        entryName += kft.GetExtension(entry.TypeHash) ?? "." + kft.GetName(entry.TypeHash);
                    }

                    var entryPath = Path.Combine(outputPath, entryName);
                    if (overwriteFiles == false &&
                        File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
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

                        if (compressionScheme == CompressionScheme.None)
                        {
                            output.WriteFromStream(input, entry.CompressedSize);
                        }
                        else if (compressionScheme == CompressionScheme.Zlib)
                        {
                            if (entry.CompressedSize == entry.UncompressedSize)
                            {
                                output.WriteFromStream(input, entry.UncompressedSize);
                            }
                            else
                            {
                                using (var temp = input.ReadToMemoryStream((int)entry.CompressedSize))
                                {
                                    var zlib = new InflaterInputStream(temp);
                                    output.WriteFromStream(zlib, entry.UncompressedSize);
                                }
                            }
                        }
                        else if (compressionScheme == CompressionScheme.ZlibHeaderless)
                        {
                            if (entry.CompressedSize == entry.UncompressedSize)
                            {
                                output.WriteFromStream(input, entry.UncompressedSize);
                            }
                            else
                            {
                                using (var temp = input.ReadToMemoryStream((int)entry.CompressedSize))
                                {
                                    var zlib = new InflaterInputStream(temp, new Inflater(false));
                                    output.WriteFromStream(zlib, entry.UncompressedSize);
                                }
                            }
                        }
                        else if (compressionScheme == CompressionScheme.XCompress)
                        {
                            if (entry.CompressedSize == entry.UncompressedSize)
                            {
                                output.WriteFromStream(input, entry.UncompressedSize);
                            }
                            else
                            {
                                var compressed = input.ReadBytes((int)entry.CompressedSize);
                                var uncompressed = new byte[entry.UncompressedSize];

                                using (var context = new XCompression.DecompressionContext(0x8000))
                                {
                                    var compressedSize = compressed.Length;
                                    var uncompressedSize = uncompressed.Length;

                                    if (
                                        context.Decompress(compressed,
                                                           0,
                                                           ref compressedSize,
                                                           uncompressed,
                                                           0,
                                                           ref uncompressedSize) != XCompression.ErrorCode.None)
                                    {
                                        throw new InvalidOperationException();
                                    }

                                    if (uncompressedSize != uncompressed.Length ||
                                        compressedSize != compressed.Length)
                                    {
                                        throw new InvalidOperationException();
                                    }

                                    output.WriteBytes(uncompressed);
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            }
        }
    }
}
