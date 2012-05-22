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

namespace Gibbed.MT.FileFormats
{
    public static class StringHelpers
    {
        public static uint HashCrc32(this string input)
        {
            return input.HashCrc32(0xFFFFFFFF);
        }

        public static uint HashCrc32(this string input, uint seed)
        {
            var sum = seed;
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (char t in input) // ReSharper restore LoopCanBeConvertedToQuery
            {
                sum = Crc32.Table[(sum ^ t) & 0xFF] ^ (sum >> 8);
            }
            return sum;
        }
    }
}
