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
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;

namespace Gibbed.MT.FileFormats
{
    public class ArchiveFile
    {
        public const uint Signature = 0x00435241; // 'ARC\0'
        public const uint SignatureEncrypted = 0x43435241; // 'ARCC'

        private readonly Blowfish _Blowfish;

        private Endian _Endian;
        private ushort _Version;
        private bool _IsEncrypted;
        private readonly List<Entry> _Entries;

        public ArchiveFile()
            : this(null)
        {
        }

        public ArchiveFile(string key)
        {
            this._Version = 7;
            this._Entries = new List<Entry>();

            if (key != null)
            {
                var keyBytes = Encoding.ASCII.GetBytes(key);
                var blowfish = new Blowfish(keyBytes, 0, keyBytes.Length);
                this._Blowfish = blowfish;
            }
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public ushort Version
        {
            get { return this._Version; }
            set { this._Version = value; }
        }

        public bool IsEncrypted
        {
            get { return this._IsEncrypted; }
            set { this._IsEncrypted = value; }
        }

        public List<Entry> Entries
        {
            get { return this._Entries; }
        }

        public static int ComputeHeaderSize(int count)
        {
            return 8 + count * 80;
        }

        public Blowfish GetBlowfish()
        {
            return this._Blowfish == null ? null : (Blowfish)this._Blowfish.Clone();
        }

        public void Serialize(Stream output)
        {
            var endian = this._Endian;
            var isEncrypted = this._IsEncrypted;

            byte[] indexBytes;
            using (var data = new MemoryStream())
            {
                foreach (var entry in this._Entries.OrderBy(e => e.Offset))
                {
                    uint flags;

                    if (endian == Endian.Little)
                    {
                        flags = 0;
                        flags |= (entry.UncompressedSize & 0x1FFFFFFFu) << 0;
                        flags |= ((uint)entry.Quality & 7u) << 29;
                    }
                    else if (endian == Endian.Big)
                    {
                        flags = 0;
                        flags |= (entry.UncompressedSize & 0x1FFFFFFFu) << 3;
                        flags |= ((uint)entry.Quality & 7u) << 0;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    data.WriteString(entry.Name, 64, Encoding.ASCII);
                    data.WriteValueU32(entry.TypeHash, endian);
                    data.WriteValueU32(entry.CompressedSize, endian);
                    data.WriteValueU32(flags, endian);
                    data.WriteValueU32(entry.Offset, endian);
                }

                data.Flush();
                indexBytes = data.ToArray();
            }

            output.WriteValueU32(isEncrypted == false ? Signature : SignatureEncrypted, endian);
            uint versionAndCount = (ushort)this._Version;
            versionAndCount |= (uint)this._Entries.Count << 16;
            output.WriteValueU32(versionAndCount, endian);

            if (isEncrypted == true)
            {
                var blowfish = this.GetBlowfish();
                blowfish.Encrypt(indexBytes, 0, indexBytes, 0, indexBytes.Length);
            }

            output.WriteBytes(indexBytes);
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);

            Endian endian;
            bool isEncrypted;

            if (magic == Signature || magic.Swap() == Signature)
            {
                endian = magic == Signature ? Endian.Little : Endian.Big;
                isEncrypted = false;
            }
            else if (magic == SignatureEncrypted || magic.Swap() == SignatureEncrypted)
            {
                endian = magic == SignatureEncrypted ? Endian.Little : Endian.Big;
                isEncrypted = true;
            }
            else
            {
                throw new FormatException("bad magic");
            }

            if (isEncrypted == true && this._Blowfish == null)
            {
                throw new InvalidOperationException();
            }

            var version = input.ReadValueU16(endian);
            if (version != 7 && version != 8 && version != 17)
            {
                throw new FormatException();
            }

            var count = input.ReadValueU16(endian);

            var indexBytes = input.ReadBytes(80 * count);
            if (isEncrypted == true)
            {
                var blowfish = this.GetBlowfish();
                blowfish.Decrypt(indexBytes, 0, indexBytes, 0, indexBytes.Length);
            }

            var entries = new Entry[count];
            using (var indexData = new MemoryStream(indexBytes, false))
            {
                for (int i = 0; i < count; i++)
                {
                    Entry entry;
                    entry.Name = indexData.ReadString(64, true, Encoding.ASCII);
                    entry.TypeHash = indexData.ReadValueU32(endian);
                    entry.CompressedSize = indexData.ReadValueU32(endian);
                    var flags = indexData.ReadValueU32(endian);
                    entry.Offset = indexData.ReadValueU32(endian);

                    if (endian == Endian.Little)
                    {
                        entry.Quality = (EntryQuality)((flags >> 29) & 0x7);
                        entry.UncompressedSize = (flags >> 0) & 0x1FFFFFFFu;
                    }
                    else if (endian == Endian.Big)
                    {
                        entry.Quality = (EntryQuality)((flags >> 0) & 0x7);
                        entry.UncompressedSize = (flags >> 3) & 0x1FFFFFFFu;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    entries[i] = entry;
                }
            }

            this._Endian = endian;
            this._Version = version;
            this._IsEncrypted = isEncrypted;
            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        public struct Entry
        {
            public string Name;
            public uint TypeHash;
            public uint Offset;
            public uint CompressedSize;
            public uint UncompressedSize;
            public EntryQuality Quality;

            public override string ToString()
            {
                return this.Name ?? base.ToString();
            }
        }

        public enum EntryQuality
        {
            Lowest = 0,
            Low,
            Normal,
            High,
            Highest,
            StreamLow,
            StreamHigh,
            Invalid,
        }
    }
}
