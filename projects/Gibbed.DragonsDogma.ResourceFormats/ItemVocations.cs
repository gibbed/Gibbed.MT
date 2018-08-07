﻿/* Copyright (c) 2018 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.DragonsDogma.ResourceFormats
{
    [Flags]
    public enum ItemVocations : ushort
    {
        None = 0,

        Fighter = 1 << 0,
        Strider = 1 << 1,
        Mage = 1 << 2,
        MysticKnight = 1 << 3,
        Assassin = 1 << 4,
        MagickArcher = 1 << 5,
        Warrior = 1 << 6,
        Ranger = 1 << 7,
        Sorcerer = 1 << 8,

        All = Fighter | Strider | Mage |
              MysticKnight | Assassin | MagickArcher |
              Warrior | Ranger | Sorcerer,
    }
}
