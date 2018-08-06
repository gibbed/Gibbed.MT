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

namespace Gibbed.MT.FileFormats.Archive
{
    public class KnownFileTypes
    {
        private readonly Dictionary<uint, string> _Names = new Dictionary<uint, string>();
        private readonly Dictionary<uint, string> _Extensions = new Dictionary<uint, string>();

        public bool Contains(uint hash)
        {
            return this._Names.ContainsKey(hash);
        }

        public string GetName(uint hash)
        {
            if (this._Names.ContainsKey(hash) == false)
            {
                return null;
            }

            return this._Names[hash];
        }

        public string GetExtension(uint hash)
        {
            if (this._Extensions.ContainsKey(hash) == false)
            {
                return null;
            }

            return this._Extensions[hash];
        }

        public void Load(string path)
        {
            string text;
            using (var input = File.OpenRead(path))
            {
                var reader = new StreamReader(input);
                text = reader.ReadToEnd();
            }

            this._Names.Clear();
            this._Extensions.Clear();

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

                    var id = parts[0];
                    id = id.Trim();

                    uint givenHash;
                    if (
                        uint.TryParse(id, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out givenHash) ==
                        false)
                    {
                        throw new FormatException("failed to parse hash");
                    }

                    if (parts.Length == 1)
                    {
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

                    var actualHash = key.HashCrc32() & 0x7FFFFFFF;

                    if (givenHash != actualHash)
                    {
                        throw new FormatException("given hash does not match actual hash for " + key + " (" +
                                                  givenHash.ToString("X8") + " vs " + actualHash.ToString("X8") + ")");
                    }

                    if (this._Names.ContainsKey(actualHash) == true)
                    {
                        throw new FormatException("duplicate hash: " + key + " and " + this._Names[actualHash] + " (" +
                                                  actualHash.ToString("X8") + ")");
                    }

                    this._Names.Add(actualHash, key);

                    if (string.IsNullOrEmpty(value) == false)
                    {
                        if (this._Extensions.ContainsValue(value) == true)
                        {
                            throw new FormatException("duplicate extension: " + value);
                        }

                        this._Extensions.Add(actualHash, value);
                    }
                }
            }
        }
    }
}
