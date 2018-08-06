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
using System.Linq;
using Gibbed.IO;
using Gibbed.MT.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;

namespace Gibbed.MT.Pack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool encrypt = false;
            bool verbose = false;
            bool showHelp = false;
            string currentProject = null;

            var options = new OptionSet()
            {
                { "e|encrypt", "encrypt archive", v => encrypt = v != null },
                { "v|verbose", "show verbose messages", v => verbose = v != null },
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

            if (extras.Count < 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ output_arc input_directory+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Pack files from input directories into a archive.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPaths = new List<string>();
            string outputPath;

            if (extras.Count == 1)
            {
                inputPaths.Add(extras[0]);
                outputPath = Path.ChangeExtension(extras[0], ".arc");
            }
            else
            {
                outputPath = Path.ChangeExtension(extras[0], ".arc");
                inputPaths.AddRange(extras.Skip(1));
            }

            var manager = ProjectData.Manager.Load(currentProject);
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var cryptoKey = manager.GetSetting("crypto_key", null);
            var archiveEndian = manager.GetSetting("archive_endian", Endian.Little);
            var archiveVersion = manager.GetSetting("archive_version", (ushort)7);
            var compressionScheme = manager.GetSetting("archive_compression_scheme", CompressionScheme.None);

            if (string.IsNullOrEmpty(cryptoKey) == true && encrypt == true)
            {
                Console.WriteLine("Error: cannot enable encryption with no crypto key.");
                return;
            }

            var knownFileTypes = new KnownFileTypes();

            if (manager.ActiveProject != null)
            {
                var knownFileTypesPath = Path.Combine(manager.ActiveProject.ListsPath, "file types.cfg");
                if (File.Exists(knownFileTypesPath) == true)
                {
                    knownFileTypes.Load(knownFileTypesPath);
                }
            }

            var pendingEntries = new SortedDictionary<string, PendingEntry>();

            if (verbose == true)
            {
                Console.WriteLine("Finding files...");
            }

            foreach (var relativePath in inputPaths)
            {
                string inputPath = Path.GetFullPath(relativePath);

                if (inputPath.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) == true)
                {
                    inputPath = inputPath.Substring(0, inputPath.Length - 1);
                }

                foreach (string path in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                {
                    string fullPath = Path.GetFullPath(path);

                    string partPath = fullPath.Substring(inputPath.Length + 1)
                                              .Replace(Path.DirectorySeparatorChar, '\\')
                                              .Replace(Path.AltDirectorySeparatorChar, '\\');

                    var key = partPath.ToLowerInvariant();

                    if (pendingEntries.ContainsKey(key) == true)
                    {
                        Console.WriteLine("Ignoring duplicate of {0}: {1}", partPath, fullPath);

                        if (verbose == true)
                        {
                            Console.WriteLine("  Previously added from: {0}", pendingEntries[key]);
                        }

                        continue;
                    }

                    pendingEntries[key] = new PendingEntry(fullPath, partPath);
                }
            }

            using (var output = File.Create(outputPath))
            {
                var archive = new ArchiveFile(cryptoKey)
                {
                    Endian = archiveEndian,
                    Version = archiveVersion,
                    IsEncrypted = encrypt,
                };

                output.Seek(ArchiveFile.ComputeHeaderSize(pendingEntries.Count), SeekOrigin.Begin);

                if (verbose == true)
                {
                    Console.WriteLine("Writing file data...");
                }

                long current = 0;
                long total = pendingEntries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var pendingEntry in pendingEntries.Select(kv => kv.Value))
                {
                    var entry = new ArchiveFile.Entry();

                    var partPath = pendingEntry.PartPath;
                    var fullPath = pendingEntry.FullPath;

                    current++;

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                          total,
                                          partPath);
                    }

                    using (var input = File.OpenRead(fullPath))
                    {
                        entry.Name = Path.ChangeExtension(partPath, null);

                        var extension = Path.GetExtension(partPath) ?? "";

                        var typeHash = knownFileTypes.GetHashFromExtension(extension);
                        if (typeHash.HasValue == false)
                        {
                            if (extension.StartsWith(".UNK#") == false)
                            {
                                throw new InvalidOperationException();
                            }

                            typeHash = uint.Parse(extension.Substring(5), NumberStyles.AllowHexSpecifier);
                        }

                        entry.TypeHash = typeHash.Value;
                        entry.Offset = (uint)output.Position;
                        entry.UncompressedSize = (uint)input.Length;

                        uint compressedSize;
                        using (var temp = new MemoryStream())
                        {
                            if (compressionScheme == CompressionScheme.None)
                            {
                                temp.WriteFromStream(input, input.Length);
                                temp.Flush();
                                temp.Position = 0;
                            }
                            else if (compressionScheme == CompressionScheme.Zlib ||
                                     compressionScheme == CompressionScheme.ZlibHeaderless)
                            {
                                var headerless = compressionScheme == CompressionScheme.ZlibHeaderless;
                                int compressionLevel = Deflater.BEST_COMPRESSION;
                                var deflater = new Deflater(compressionLevel, headerless);
                                using (var zlib = new DeflaterOutputStream(temp, deflater))
                                {
                                    zlib.IsStreamOwner = false;
                                    zlib.WriteFromStream(input, input.Length);
                                    zlib.Finish();
                                }
                                temp.Flush();
                                temp.Position = 0;
                            }
                            else if (compressionScheme == CompressionScheme.XCompress)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }

                            if (encrypt == true)
                            {
                                temp.SetLength(temp.Length.Align(8));
                                var blowfish = archive.GetBlowfish();
                                var tempBytes = temp.GetBuffer();
                                blowfish.Encrypt(tempBytes, 0, tempBytes, 0, tempBytes.Length);
                            }

                            compressedSize = (uint)temp.Length;
                            output.WriteFromStream(temp, temp.Length);
                        }

                        entry.CompressedSize = compressedSize;

                        entry.Quality = ArchiveFile.EntryQuality.Normal;
                        archive.Entries.Add(entry);
                    }
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing header...");
                }

                output.Seek(0, SeekOrigin.Begin);
                archive.Serialize(output);

                if (verbose == true)
                {
                    Console.WriteLine("Done!");
                }
            }
        }

        private struct PendingEntry
        {
            public string FullPath;
            public string PartPath;

            public PendingEntry(string fullPath, string partPath)
            {
                this.FullPath = fullPath;
                this.PartPath = partPath;
            }
        }
    }
}
