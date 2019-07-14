/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.MT.FileFormats
{
    public class KnownFileTypes
    {
        private readonly Dictionary<uint, string> _HashToNames = new Dictionary<uint, string>();
        private readonly Dictionary<uint, string> _HashToExtensions = new Dictionary<uint, string>();
        private readonly Dictionary<string, uint> _ExtensionToHashes = new Dictionary<string, uint>();

        public bool Contains(uint hash)
        {
            return this._HashToNames.ContainsKey(hash);
        }

        public uint? GetHashFromExtension(string extension)
        {
            if (this._ExtensionToHashes.ContainsKey(extension) == false)
            {
                return null;
            }

            return this._ExtensionToHashes[extension];
        }

        public string GetName(uint hash)
        {
            if (this._HashToNames.ContainsKey(hash) == false)
            {
                return null;
            }

            return this._HashToNames[hash];
        }

        public string GetExtension(uint hash)
        {
            if (this._HashToExtensions.ContainsKey(hash) == false)
            {
                return null;
            }

            return this._HashToExtensions[hash];
        }

        public void Load(string path)
        {
            string text;
            using (var input = File.OpenRead(path))
            {
                var reader = new StreamReader(input);
                text = reader.ReadToEnd();
            }

            this._HashToNames.Clear();
            this._HashToExtensions.Clear();
            this._ExtensionToHashes.Clear();

            var whitespace = new[]
            {
                ' ', '\t'
            };

            using (var reader = new StringReader(text))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    var comment = line.IndexOf('#');
                    if (comment >= 0)
                    {
                        line = line.Substring(0, comment);
                    }

                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) == true)
                    {
                        continue;
                    }

                    var parts = line.Split(whitespace, 3);
                    if (parts.Length < 1 || parts.Length > 3)
                    {
                        throw new FormatException();
                    }

                    var hashText = parts[0];
                    hashText = hashText.Trim();

                    if (parts.Length == 1)
                    {
                        uint dummy;
                        if (uint.TryParse(hashText,
                                          NumberStyles.AllowHexSpecifier,
                                          CultureInfo.InvariantCulture,
                                          out dummy) == false)
                        {
                            throw new FormatException("failed to parse hash");
                        }

                        continue;
                    }

                    var value = parts.Length > 2 ? parts[1] : null;
                    var key = parts.Length > 2 ? parts[2] : parts[1];

                    key = key.Trim();

                    if (value != null)
                    {
                        value = value.Trim();
                    }

                    if (string.IsNullOrEmpty(key) == true)
                    {
                        throw new FormatException("empty key");
                    }

                    var actualHash = Crc32.Compute(key) & 0x7FFFFFFFu;

                    if (hashText != "????????")
                    {
                        uint givenHash;
                        if (uint.TryParse(hashText,
                                          NumberStyles.AllowHexSpecifier,
                                          CultureInfo.InvariantCulture,
                                          out givenHash) == false)
                        {
                            throw new FormatException("failed to parse hash");
                        }

                        if (givenHash != actualHash)
                        {
                            throw new FormatException("given hash does not match actual hash for " + key + " (" +
                                                      givenHash.ToString("X8") + " vs " + actualHash.ToString("X8") +
                                                      ")");
                        }
                    }

                    if (this._HashToNames.ContainsKey(actualHash) == true)
                    {
                        throw new FormatException("duplicate hash: " + key + " and " + this._HashToNames[actualHash] + " (" +
                                                  actualHash.ToString("X8") + ")");
                    }

                    this._HashToNames.Add(actualHash, key);

                    if (string.IsNullOrEmpty(value) == false)
                    {
                        if (this._HashToExtensions.ContainsValue(value) == true)
                        {
                            throw new FormatException("duplicate extension: " + value);
                        }

                        this._HashToExtensions.Add(actualHash, value);
                        this._ExtensionToHashes.Add(value, actualHash);
                    }
                }
            }
        }
    }
}
