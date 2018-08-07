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

namespace Gibbed.MT.FileFormats
{
    public interface IBitfieldWriter<T>
    {
        IBitfieldWriter<T> FieldB8(Func<T, bool> action);
        IBitfieldWriter<T> FieldS8(int bitCount, Func<T, sbyte> action);
        IBitfieldWriter<T> FieldU8(int bitCount, Func<T, byte> action);
        IBitfieldWriter<T> FieldS16(int bitCount, Func<T, short> action);
        IBitfieldWriter<T> FieldU16(int bitCount, Func<T, ushort> action);
        IBitfieldWriter<T> FieldS32(int bitCount, Func<T, int> action);
        IBitfieldWriter<T> FieldU32(int bitCount, Func<T, uint> action);
        IBitfieldWriter<T> FieldS64(int bitCount, Func<T, long> action);
        IBitfieldWriter<T> FieldU64(int bitCount, Func<T, ulong> action);
    }
}
