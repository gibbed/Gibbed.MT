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
using Gibbed.IO;
using Gibbed.MT.FileFormats;

namespace Gibbed.DragonsDogma.ResourceFormats
{
    public class ItemList : IResource
    {
        public const uint Signature = 0x324C5449; // 'ITL2'
        public const uint VersionExpansion = 20121105; // 2012-11-05

        #region Fields
        private readonly List<Item> _Entries;
        #endregion

        public ItemList()
        {
            this._Entries = new List<Item>();
        }

        #region Properties
        public List<Item> Entries
        {
            get { return this._Entries; }
        }
        #endregion

        public static Endian? DetectEndian(Stream stream)
        {
            var magic = stream.ReadValueU32(Endian.Little);
            stream.Seek(-4, SeekOrigin.Current);
            if (magic == Signature)
            {
                return Endian.Little;
            }
            if (magic.Swap() == Signature)
            {
                return Endian.Big;
            }
            return null;
        }

        public void Load(IResourceReader reader)
        {
            var magic = reader.ReadU32();
            if (magic != Signature)
            {
                throw new FormatException();
            }

            var version = reader.ReadU32();
            if (version != VersionExpansion)
            {
                throw new FormatException();
            }

            var count = reader.ReadU32();
            reader.ReadU32();

            var entries = new Item[count];
            for (uint i = 0; i < count; i++)
            {
                var item = entries[i] = new Item();
                item.Load(reader);
            }

            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        public void Save(IResourceWriter writer)
        {
            writer.WriteU32(Signature);
            writer.WriteU32(VersionExpansion);
            writer.WriteS32(this.Entries.Count);
            writer.WriteU32(0u);
            foreach (var entry in this._Entries)
            {
                entry.Save(writer);
            }
        }
    }
}
