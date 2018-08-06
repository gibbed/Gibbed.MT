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
using Constants = Gibbed.MT.FileFormats.BlowfishConstants;

namespace Gibbed.MT.FileFormats
{
    public sealed class Blowfish : ICloneable
    {
        public const int MaximumKeyLength = 56;
        public const int BlockSize = 8;

        private const int _PCount = 18;
        private const int _SCount = 256;

        private uint[] _P = new uint[_PCount];
        private uint[] _S0 = new uint[_SCount];
        private uint[] _S1 = new uint[_SCount];
        private uint[] _S2 = new uint[_SCount];
        private uint[] _S3 = new uint[_SCount];

        public Blowfish()
        {
            Initialize(null, 0, 0);
        }

        public Blowfish(byte[] buffer, int offset, int count)
        {
            Initialize(buffer, offset, count);
        }

        public void Initialize(byte[] buffer, int offset, int count)
        {
            Array.Copy(Constants.InitialP, 0, this._P, 0, Constants.InitialP.Length);
            Array.Copy(Constants.InitialS0, 0, this._S0, 0, Constants.InitialS0.Length);
            Array.Copy(Constants.InitialS1, 0, this._S1, 0, Constants.InitialS1.Length);
            Array.Copy(Constants.InitialS2, 0, this._S2, 0, Constants.InitialS2.Length);
            Array.Copy(Constants.InitialS3, 0, this._S3, 0, Constants.InitialS3.Length);

            if (count == 0)
            {
                return;
            }

            int start = offset;
            int end = offset + count;

            for (int i = 0; i < _PCount; i++)
            {
                uint build = 0;
                for (int j = 0; j < 4; j++)
                {
                    build = (build << 8) | buffer[offset];

                    offset++;
                    if (offset == end)
                    {
                        offset = start;
                    }
                }

                this._P[i] ^= build;
            }

            uint hi = 0;
            uint lo = 0;

            var work = new byte[BlockSize];

            var box = this._P;
            for (int i = 0; i < _PCount; i += 2)
            {
                EncryptBlock(hi, lo, out hi, out lo, work);
                box[i + 0] = hi;
                box[i + 1] = lo;
            }

            box = this._S0;
            for (int i = 0; i < _SCount; i += 2)
            {
                EncryptBlock(hi, lo, out hi, out lo, work);
                box[i + 0] = hi;
                box[i + 1] = lo;
            }

            box = this._S1;
            for (int i = 0; i < _SCount; i += 2)
            {
                EncryptBlock(hi, lo, out hi, out lo, work);
                box[i + 0] = hi;
                box[i + 1] = lo;
            }

            box = this._S2;
            for (int i = 0; i < _SCount; i += 2)
            {
                EncryptBlock(hi, lo, out hi, out lo, work);
                box[i + 0] = hi;
                box[i + 1] = lo;
            }

            box = this._S3;
            for (int i = 0; i < _SCount; i += 2)
            {
                EncryptBlock(hi, lo, out hi, out lo, work);
                box[i + 0] = hi;
                box[i + 1] = lo;
            }
        }

        private void EncryptBlock(uint hi, uint lo, out uint outHi, out uint outLo, byte[] work)
        {
            work[3] = (byte)(hi >> 24);
            work[2] = (byte)(hi >> 16);
            work[1] = (byte)(hi >> 8);
            work[0] = (byte)(hi >> 0);
            work[7] = (byte)(lo >> 24);
            work[6] = (byte)(lo >> 16);
            work[5] = (byte)(lo >> 8);
            work[4] = (byte)(lo >> 0);

            Encrypt(work, 0, work, 0, BlockSize);

            outHi = (((uint)work[3]) << 24) |
                    (((uint)work[2]) << 16) |
                    (((uint)work[1]) << 8) |
                    (((uint)work[0]) << 0);
            outLo = (((uint)work[7]) << 24) |
                    (((uint)work[6]) << 16) |
                    (((uint)work[5]) << 8) |
                    (((uint)work[4]) << 0);
        }

        public int Encrypt(byte[] sourceArray,
                           int sourceIndex,
                           byte[] destinationArray,
                           int destinationIndex,
                           int length)
        {
            length &= ~(BlockSize - 1);

            var p = this._P;
            var s0 = this._S0;
            var s1 = this._S1;
            var s2 = this._S2;
            var s3 = this._S3;

            int end = sourceIndex + length;
            while (sourceIndex < end)
            {
                uint hi = (((uint)sourceArray[sourceIndex + 3]) << 24) |
                          (((uint)sourceArray[sourceIndex + 2]) << 16) |
                          (((uint)sourceArray[sourceIndex + 1]) << 8) |
                          (((uint)sourceArray[sourceIndex + 0]) << 0);
                uint lo = (((uint)sourceArray[sourceIndex + 7]) << 24) |
                          (((uint)sourceArray[sourceIndex + 6]) << 16) |
                          (((uint)sourceArray[sourceIndex + 5]) << 8) |
                          (((uint)sourceArray[sourceIndex + 4]) << 0);

                hi ^= p[0];
                for (int i = 1; i <= 16; i += 2)
                {
                    lo ^= (((s0[(byte)(hi >> 24)] +
                             s1[(byte)(hi >> 16)]) ^
                            s2[(byte)(hi >> 8)]) +
                           s3[(byte)hi >> 0]) ^
                          p[i + 0];
                    hi ^= (((s0[(byte)(lo >> 24)] +
                             s1[(byte)(lo >> 16)]) ^
                            s2[(byte)(lo >> 8)]) +
                           s3[(byte)lo >> 0]) ^
                          p[i + 1];
                }
                lo ^= p[17];

                destinationArray[destinationIndex + 3] = (byte)(lo >> 24);
                destinationArray[destinationIndex + 2] = (byte)(lo >> 16);
                destinationArray[destinationIndex + 1] = (byte)(lo >> 8);
                destinationArray[destinationIndex + 0] = (byte)(lo >> 0);
                destinationArray[destinationIndex + 7] = (byte)(hi >> 24);
                destinationArray[destinationIndex + 6] = (byte)(hi >> 16);
                destinationArray[destinationIndex + 5] = (byte)(hi >> 8);
                destinationArray[destinationIndex + 4] = (byte)(hi >> 0);

                sourceIndex += 8;
                destinationIndex += 8;
            }

            return length;
        }

        public int Decrypt(byte[] sourceArray,
                           int sourceIndex,
                           byte[] destinationArray,
                           int destinationIndex,
                           int length)
        {
            length &= ~(BlockSize - 1);

            var p = this._P;
            var s0 = this._S0;
            var s1 = this._S1;
            var s2 = this._S2;
            var s3 = this._S3;

            int end = sourceIndex + length;
            while (sourceIndex < end)
            {
                uint hi = (((uint)sourceArray[sourceIndex + 3]) << 24) |
                          (((uint)sourceArray[sourceIndex + 2]) << 16) |
                          (((uint)sourceArray[sourceIndex + 1]) << 8) |
                          (((uint)sourceArray[sourceIndex + 0]) << 0);
                uint lo = (((uint)sourceArray[sourceIndex + 7]) << 24) |
                          (((uint)sourceArray[sourceIndex + 6]) << 16) |
                          (((uint)sourceArray[sourceIndex + 5]) << 8) |
                          (((uint)sourceArray[sourceIndex + 4]) << 0);

                hi ^= p[17];
                for (int i = 16; i >= 1; i -= 2)
                {
                    lo ^= (((s0[(byte)(hi >> 24)] +
                             s1[(byte)(hi >> 16)]) ^
                            s2[(byte)(hi >> 8)]) +
                           s3[(byte)hi >> 0]) ^
                          p[i - 0];
                    hi ^= (((s0[(byte)(lo >> 24)] +
                             s1[(byte)(lo >> 16)]) ^
                            s2[(byte)(lo >> 8)]) +
                           s3[(byte)lo >> 0]) ^
                          p[i - 1];
                }
                lo ^= p[0];

                destinationArray[destinationIndex + 3] = (byte)(lo >> 24);
                destinationArray[destinationIndex + 2] = (byte)(lo >> 16);
                destinationArray[destinationIndex + 1] = (byte)(lo >> 8);
                destinationArray[destinationIndex + 0] = (byte)(lo >> 0);
                destinationArray[destinationIndex + 7] = (byte)(hi >> 24);
                destinationArray[destinationIndex + 6] = (byte)(hi >> 16);
                destinationArray[destinationIndex + 5] = (byte)(hi >> 8);
                destinationArray[destinationIndex + 4] = (byte)(hi >> 0);

                sourceIndex += 8;
                destinationIndex += 8;
            }

            return length;
        }

        public object Clone()
        {
            return new Blowfish()
            {
                _P = (uint[])this._P.Clone(),
                _S0 = (uint[])this._S0.Clone(),
                _S1 = (uint[])this._S1.Clone(),
                _S2 = (uint[])this._S2.Clone(),
                _S3 = (uint[])this._S3.Clone(),
            };
        }
    }
}
