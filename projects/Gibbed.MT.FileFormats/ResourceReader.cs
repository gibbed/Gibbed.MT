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
using System.Linq.Expressions;
using Gibbed.IO;

namespace Gibbed.MT.FileFormats
{
    public class ResourceReader : IResourceReader
    {
        private readonly Stream _Stream;
        private readonly Endian _Endian;
        private readonly Dictionary<string, object> _BitfieldCache;

        public ResourceReader(Stream stream, Endian endian)
        {
            this._Stream = stream;
            this._Endian = endian;
            this._BitfieldCache = new Dictionary<string, object>();
        }

        public void Read(byte[] buffer, int index, int count)
        {
            this._Stream.Read(buffer, index, count);
        }

        public byte[] ReadBytes()
        {
            throw new NotImplementedException();
        }

        public sbyte ReadS8()
        {
            return this._Stream.ReadValueS8();
        }

        public byte ReadU8()
        {
            return this._Stream.ReadValueU8();
        }

        public short ReadS16()
        {
            return this._Stream.ReadValueS16(this._Endian);
        }

        public ushort ReadU16()
        {
            return this._Stream.ReadValueU16(this._Endian);
        }

        public int ReadS32()
        {
            return this._Stream.ReadValueS32(this._Endian);
        }

        public uint ReadU32()
        {
            return this._Stream.ReadValueU32(this._Endian);
        }

        public float ReadF32()
        {
            return this._Stream.ReadValueF32(this._Endian);
        }

        public double ReadF64()
        {
            return this._Stream.ReadValueF64(this._Endian);
        }

        internal void ExtractBit(ushort bits, int bit, out bool value)
        {
            var shift = this._Endian == Endian.Little
                            ? bit
                            : 16 - bit - 1;
            value = (bits & (1 << shift)) != 0;
        }

        internal void ExtractBits(ushort bits, int bit, int count, out sbyte value)
        {
            short dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (sbyte)dummy;
        }

        internal void ExtractBits(ushort bits, int bit, int count, out byte value)
        {
            ushort dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (byte)dummy;
        }

        internal void ExtractBits(ushort bits, int bit, int count, out short value)
        {
            ushort dummy;
            ExtractBits(bits, bit, count, out dummy);
            if ((dummy & (ushort)(1u << (count - 1))) != 0)
            {
                dummy |= (ushort)(((ushort)(1u << (16 - count)) - 1) << count);
            }
            value = (short)dummy;
        }

        internal void ExtractBits(ushort bits, int bit, int count, out ushort value)
        {
            var shift = this._Endian == Endian.Little
                            ? bit
                            : 16 - bit - count;
            var mask = (ushort)((1u << count) - 1u);
            value = (ushort)(bits >> shift);
            value &= mask;
        }

        internal void ExtractBit(uint bits, int bit, out bool value)
        {
            var shift = this._Endian == Endian.Little
                            ? bit
                            : 32 - bit - 1;
            value = (bits & (1 << shift)) != 0;
        }

        internal void ExtractBits(uint bits, int bit, int count, out sbyte value)
        {
            int dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (sbyte)dummy;
        }

        internal void ExtractBits(uint bits, int bit, int count, out byte value)
        {
            uint dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (byte)dummy;
        }

        internal void ExtractBits(uint bits, int bit, int count, out short value)
        {
            int dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (short)dummy;
        }

        internal void ExtractBits(uint bits, int bit, int count, out ushort value)
        {
            uint dummy;
            ExtractBits(bits, bit, count, out dummy);
            value = (ushort)dummy;
        }

        internal void ExtractBits(uint bits, int bit, int count, out int value)
        {
            uint dummy;
            ExtractBits(bits, bit, count, out dummy);
            if ((dummy & (1u << (count - 1))) != 0)
            {
                dummy |= ((1u << (32 - count)) - 1) << count;
            }
            value = (int)dummy;
        }

        internal void ExtractBits(uint bits, int bit, int count, out uint value)
        {
            var shift = this._Endian == Endian.Little
                            ? bit
                            : 32 - bit - count;
            var mask = (1u << count) - 1u;
            value = bits >> shift;
            value &= mask;
        }

        public void ReadBitfield<T>(T target, Action<IBitfieldReader<T>> bitfieldBuilder, string cacheName)
        {
            object cached;
            if (this._BitfieldCache.TryGetValue(cacheName, out cached) == false)
            {
                var bitfield = new ResourceBitfieldReader<T>();
                bitfieldBuilder(bitfield);
                var readerParameter = Expression.Parameter(typeof(ResourceReader), "reader");
                var targetParameter = Expression.Parameter(typeof(T), "target");
                var lambda = Expression.Lambda<Action<ResourceReader, T>>(
                    bitfield.BuildExpression(readerParameter, targetParameter),
                    readerParameter,
                    targetParameter);
                var read = lambda.Compile();
                this._BitfieldCache[cacheName] = read;
                read(this, target);
            }
            else
            {
                ((Action<ResourceReader, T>)cached)(this, target);
            }
        }
    }
}
