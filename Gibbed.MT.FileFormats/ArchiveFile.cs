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
using System.IO;
using System.Text;
using Gibbed.IO;
using Gibbed.MT.FileFormats.Archive;

namespace Gibbed.MT.FileFormats
{
    public class ArchiveFile
    {
        public const uint Signature = 0x00435241; // 'ARC\0'

        public readonly UnknownFlagPosition UnknownFlagPosition;

        public Endian Endian;
        public ushort Version;
        public List<Entry> Entries = new List<Entry>();

        public ArchiveFile(UnknownFlagPosition unknownFlagPosition)
        {
            if (unknownFlagPosition != UnknownFlagPosition.Upper &&
                unknownFlagPosition != UnknownFlagPosition.Lower)
            {
                throw new ArgumentException("unknown flag position must be upper or lower", "unknownFlagPosition");
            }

            this.UnknownFlagPosition = unknownFlagPosition;
        }

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature &&
                magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU16(endian);
            if (version != 7 &&
                version != 8 &&
                version != 17)
            {
                throw new FormatException();
            }

            var fileCount = input.ReadValueU16(endian);

            this.Entries.Clear();
            for (ushort i = 0; i < fileCount; i++)
            {
                var entry = new Entry();
                entry.Name = input.ReadString(64, true, Encoding.ASCII);
                entry.TypeHash = input.ReadValueU32(endian);
                entry.CompressedSize = input.ReadValueU32(endian);
                var uncompressedSize = input.ReadValueU32(endian);
                entry.Offset = input.ReadValueU32(endian);

                if (this.UnknownFlagPosition == UnknownFlagPosition.Lower)
                {
                    entry.UncompressedSize = (uncompressedSize & 0xFFFFFFF8) >> 3;
                    entry.UnknownFlags = (UnknownFlags)((uncompressedSize & 0x00000007) >> 0);
                }
                else if (this.UnknownFlagPosition == UnknownFlagPosition.Upper)
                {
                    entry.UncompressedSize = (uncompressedSize & 0x1FFFFFFF) >> 0;
                    entry.UnknownFlags = (UnknownFlags)((uncompressedSize & 0xE0000000) >> 29);
                }

                if (entry.UnknownFlags != UnknownFlags.Unknown1)
                {
                    throw new FormatException();
                }

                this.Entries.Add(entry);
            }

            this.Version = version;
            this.Endian = endian;
        }
    }
}
