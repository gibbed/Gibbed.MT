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
using System.IO;
using System.Text;
using Gibbed.DragonsDogma.ResourceFormats;
using Gibbed.IO;
using Gibbed.MT.FileFormats;
using NDesk.Options;
using Newtonsoft.Json;

namespace Gibbed.DragonsDogma.ConvertItemList
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private enum Mode
        {
            Unknown,
            Export,
            Import,
        }

        private static void SetOption<T>(string v, ref T option, T value)
        {
            if (v != null)
            {
                option = value;
            }
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            Endian? overrideEndian = null;
            bool showHelp = false;

            var options = new OptionSet()
            {
                // ReSharper disable AccessToModifiedClosure
                { "e|export", "convert to JSON", v => SetOption(v, ref mode, Mode.Export) },
                { "i|import", "convert from JSON", v => SetOption(v, ref mode, Mode.Import) },
                { "l|little-endian", "little endian mode", v => SetOption(v, ref overrideEndian, Endian.Little) },
                { "b|big-endian", "big endian mode", v => SetOption(v, ref overrideEndian, Endian.Big) },
                { "h|help", "show this message and exit", v => showHelp = v != null },
                // ReSharper restore AccessToModifiedClosure
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

            if (mode == Mode.Unknown && extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);

                if (extension != null &&
                    extension.ToLowerInvariant() == ".json")
                {
                    mode = Mode.Import;
                }
                else
                {
                    mode = Mode.Export;
                }
            }

            if (extras.Count < 1 || extras.Count > 2 ||
                showHelp == true ||
                mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ [-e] input_itl  [output_json]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ [-i] input_json [output_itl]", GetExecutableName());
                Console.WriteLine("Convert an item list to and from JSON format.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (mode == Mode.Export)
            {
                string inputPath = extras[0];
                string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".json");

                Endian endian;
                ItemList itemList;
                using (var input = File.OpenRead(inputPath))
                {
                    if (overrideEndian == null)
                    {
                        var detectedEndian = ItemList.DetectEndian(input);
                        if (detectedEndian == null)
                        {
                            throw new FormatException();
                        }
                        endian = detectedEndian.Value;
                    }
                    else
                    {
                        endian = overrideEndian.Value;
                    }

                    var reader = new ResourceReader(input, endian);
                    itemList = new ItemList();
                    itemList.Load(reader);
                }

                var serializer = new JsonSerializer();
                serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                using (var stringWriter = new StringWriter())
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 2;
                    writer.IndentChar = ' ';

                    serializer.Serialize(
                        writer,
                        new ItemFile()
                        {
                            Endian = endian,
                            Items = itemList.Entries,
                        });

                    writer.Flush();
                    stringWriter.Flush();

                    File.WriteAllText(outputPath, stringWriter.ToString(), Encoding.UTF8);
                }
            }
            else if (mode == Mode.Import)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [JsonObject]
        private class ItemFile
        {
            [JsonProperty("endian")]
            public Endian Endian { get; set; }

            [JsonProperty("items")]
            public IEnumerable<Item> Items { get; set; }
        }
    }
}
