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
using Gibbed.MT.FileFormats;

namespace Gibbed.DragonsDogma.ResourceFormats
{
    public class Item : IResource
    {
        public short Strength { get; set; } // 0
        public short Magick { get; set; } // 0
        public short Unknown00_22 { get; set; } // 0
        public short Defenses { get; set; } // 4
        public short MagickDefenses { get; set; } // 4
        public byte Unknown04_20 { get; set; }
        public byte Unknown04_27 { get; set; }
        public short StaggerPower { get; set; } // 8
        public short KnockdownPower { get; set; } // 8
        public byte Unknown08_20 { get; set; }
        public byte Unknown08_27 { get; set; }
        public uint Unknown0C_0 { get; set; }
        public short Unknown0C_20 { get; set; }
        public byte Unknown0C_30 { get; set; }
        public byte Unknown10_0 { get; set; }
        public byte Unknown10_5 { get; set; }
        public short Oil { get; set; } // 10
        public short Unknown10_20 { get; set; }
        public bool Unknown10_30 { get; set; }
        public bool Unknown10_31 { get; set; }
        public sbyte SlashStrength { get; set; } // 14
        public sbyte BludgeoningStrength { get; set; } // 15
        public sbyte PiercingResistance { get; set; } // 16
        public sbyte StrikingResistance { get; set; } // 17
        public sbyte Fire { get; set; } // 18
        public sbyte Ice { get; set; } // 19
        public sbyte Lightning { get; set; } // 1A
        public sbyte Holy { get; set; } // 1B
        public sbyte Dark { get; set; } // 1C
        public byte Unknown1D { get; set; }
        public byte Unknown1E { get; set; }
        public byte Unknown1F { get; set; }
        public byte Unknown20 { get; set; }
        public byte Unknown21 { get; set; }
        public sbyte KnockdownResistance { get; set; } // 22
        public sbyte StaggerResistance { get; set; } // 23
        public byte Unknown24 { get; set; }
        public sbyte PoisonResistance { get; set; } // 25
        public sbyte TorporResistance { get; set; } // 26
        public sbyte BlindnessResistance { get; set; } // 27
        public sbyte SleepResistance { get; set; } // 28
        public byte Unknown29 { get; set; }
        public byte Unknown2A { get; set; }
        public sbyte PossessionResistance { get; set; } // 2B
        public sbyte SilenceResistance { get; set; } // 2C
        public sbyte SkillStiflingResistance { get; set; } // 2D
        public sbyte CurseResistance { get; set; } // 2E
        public sbyte PetrificationResistance { get; set; }
        public sbyte LoweredStrengthResistance { get; set; }
        public sbyte LoweredDefenseResistance { get; set; }
        public sbyte LoweredMagickResistance { get; set; }
        public sbyte LoweredMagickDefenseResistance { get; set; }
        public ushort Unknown34 { get; set; }
        public ushort RequirementFlags { get; set; } // 36
        public byte Unknown36_12 { get; set; }

        public bool Unknown36_0
        {
            get { return (this.RequirementFlags & 1) != 0; }
            set
            {
                if (value == true)
                {
                    this.RequirementFlags |= 1;
                }
                else
                {
                    this.RequirementFlags &= 0xFFFE; // ~1u
                }
            }
        }

        public ItemVocations RequiredVocations
        {
            get { return (ItemVocations)((this.RequirementFlags >> 1) & 0x1FF); }
            set
            {
                this.RequirementFlags &= 0xFC01; //~(0x1FFu << 1);
                this.RequirementFlags |= (ushort)(((ushort)value & 0x1FF) << 1);
            }
        }

        public ItemGender RequiredGender
        {
            get { return (ItemGender)((this.RequirementFlags >> 10) & 0x3); }
            set
            {
                this.RequirementFlags &= 0xF3FF; //~(0x3u << 10);
                this.RequirementFlags |= (ushort)(((byte)value & 0x3) << 10);
            }
        }

        public byte Category { get; set; } // 38
        public byte Subcategory { get; set; } // 38
        public short Unknown38_10 { get; set; }
        public byte Element { get; set; } // 38
        public byte Unknown38_26 { get; set; }
        public short Id { get; set; } // 3C
        public short Unknown3C_13 { get; set; }
        public byte Unknown3C_26 { get; set; }
        public uint ModelFlags { get; set; } // 40

        public int ModelId
        {
            get { return (int)(this.ModelFlags & 0xFFFFFFu) << 24 >> 24; }
            set
            {
                this.ModelFlags &= ~0xFFFFFFu;
                this.ModelFlags |= (uint)value & 0xFFFFFFu;
            }
        }

        public byte ModelType
        {
            get { return (byte)((this.ModelFlags >> 24) & 0xFFu); }
            set
            {
                this.ModelFlags &= ~0xFF000000u;
                this.ModelFlags |= (value & 0xFFu) << 24;
            }
        }

        public float Weight { get; set; } // 44
        public int Price { get; set; } // 48
        public int Value { get; set; } // 4C
        public uint Unknown50 { get; set; }
        public float Unknown54 { get; set; }
        public bool Unknown58_0 { get; set; }
        public bool Unknown58_1 { get; set; }
        public bool Unknown58_2 { get; set; }
        public bool Unknown58_3 { get; set; }
        public bool Unknown58_4 { get; set; }
        public bool Unknown58_5 { get; set; }
        public bool Unknown58_6 { get; set; }
        public bool Unknown58_7 { get; set; }
        public bool Unknown58_8 { get; set; }
        public bool Unknown58_9 { get; set; }
        public bool Unknown58_10 { get; set; }
        public bool Unknown58_11 { get; set; }
        public bool Unknown58_12 { get; set; }
        public bool Unknown58_13 { get; set; }
        public bool Unknown58_14 { get; set; }
        public bool Unknown58_15 { get; set; }
        public bool Unknown58_16 { get; set; }
        public bool Unknown58_17 { get; set; }
        public bool Unknown58_18 { get; set; }
        public bool Unknown58_19 { get; set; }
        public bool Unknown58_20 { get; set; }
        public bool Unknown58_21 { get; set; }
        public bool Unknown58_22 { get; set; }
        public bool Unknown58_23 { get; set; }
        public bool Unknown58_24 { get; set; }
        public bool Unknown58_25 { get; set; }
        public bool Unknown58_26 { get; set; }
        public bool Unknown58_27 { get; set; }
        public bool Unknown58_28 { get; set; }
        public bool Unknown58_29 { get; set; }
        public bool Unknown58_30 { get; set; }
        public bool Unknown58_31 { get; set; }
        public uint Unknown5C { get; set; }
        public ushort Unknown60 { get; set; }
        public ushort Unknown62 { get; set; }
        public ushort Unknown64 { get; set; }
        public ushort ExpectedLevel { get; set; } // 66
        public byte Unknown68 { get; set; }
        public byte Unknown69 { get; set; }
        public byte Unknown6A { get; set; }
        public byte Unknown6B { get; set; }
        public byte Unknown6C { get; set; }
        public byte Unknown6D { get; set; }
        public byte Unknown6E { get; set; }
        public byte Unknown6F { get; set; }
        public ushort RecoverHealth { get; set; } // 70
        public ushort RecoverStamina { get; set; } // 72
        public float Unknown74 { get; set; }
        public uint Unknown78 { get; set; }
        public ushort Unknown7C { get; set; }
        public byte Unknown7E { get; set; }
        public byte Unknown7F { get; set; }

        public void Load(IResourceReader reader)
        {
            reader.ReadBitfield(
                this,
                bf => bf.FieldS16(11, (t, v) => t.Strength = v)
                        .FieldS16(11, (t, v) => t.Magick = v)
                        .FieldS16(10, (t, v) => t.Unknown00_22 = v),
                "raw00");
            reader.ReadBitfield(
                this,
                bf => bf.FieldS16(10, (t, v) => t.Defenses = v)
                        .FieldS16(10, (t, v) => t.MagickDefenses = v)
                        .FieldU8(7, (t, v) => t.Unknown04_20 = v)
                        .FieldU8(5, (t, v) => t.Unknown04_27 = v),
                "raw04");
            reader.ReadBitfield(
                this,
                bf => bf.FieldS16(10, (t, v) => t.StaggerPower = v)
                        .FieldS16(10, (t, v) => t.KnockdownPower = v)
                        .FieldU8(7, (t, v) => t.Unknown08_20 = v)
                        .FieldU8(5, (t, v) => t.Unknown08_20 = v),
                "raw08");
            reader.ReadBitfield(
                this,
                bf => bf.FieldU32(20, (t, v) => t.Unknown0C_0 = v)
                        .FieldS16(10, (t, v) => t.Unknown0C_20 = v)
                        .FieldU8(2, (t, v) => t.Unknown0C_30 = v),
                "raw0C");
            reader.ReadBitfield(
                this,
                bf => bf.FieldU8(5, (t, v) => t.Unknown10_0 = v)
                        .FieldU8(5, (t, v) => t.Unknown10_5 = v)
                        .FieldS16(10, (t, v) => t.Oil = v)
                        .FieldS16(10, (t, v) => t.Unknown10_20 = v)
                        .FieldB8((t, v) => t.Unknown10_30 = v)
                        .FieldB8((t, v) => t.Unknown10_31 = v),
                "raw10");
            this.SlashStrength = reader.ReadS8();
            this.BludgeoningStrength = reader.ReadS8();
            this.PiercingResistance = reader.ReadS8();
            this.StrikingResistance = reader.ReadS8();
            this.Fire = reader.ReadS8();
            this.Ice = reader.ReadS8();
            this.Lightning = reader.ReadS8();
            this.Holy = reader.ReadS8();
            this.Dark = reader.ReadS8();
            this.Unknown1D = reader.ReadU8();
            this.Unknown1E = reader.ReadU8();
            this.Unknown1F = reader.ReadU8();
            this.Unknown20 = reader.ReadU8();
            this.Unknown21 = reader.ReadU8();
            this.KnockdownResistance = reader.ReadS8();
            this.StaggerResistance = reader.ReadS8();
            this.Unknown24 = reader.ReadU8();
            this.PoisonResistance = reader.ReadS8();
            this.TorporResistance = reader.ReadS8();
            this.BlindnessResistance = reader.ReadS8();
            this.SleepResistance = reader.ReadS8();
            this.Unknown29 = reader.ReadU8();
            this.Unknown2A = reader.ReadU8();
            this.PossessionResistance = reader.ReadS8();
            this.SilenceResistance = reader.ReadS8();
            this.SkillStiflingResistance = reader.ReadS8();
            this.CurseResistance = reader.ReadS8();
            this.PetrificationResistance = reader.ReadS8();
            this.LoweredStrengthResistance = reader.ReadS8();
            this.LoweredDefenseResistance = reader.ReadS8();
            this.LoweredMagickResistance = reader.ReadS8();
            this.LoweredMagickDefenseResistance = reader.ReadS8();
            this.Unknown34 = reader.ReadU16();
            reader.ReadBitfield(
                this,
                bf => bf.FieldU16(12, (t, v) => t.RequirementFlags = v)
                        .FieldU8(4, (t, v) => t.Unknown36_12 = v),
                "raw36");
            reader.ReadBitfield(
                this,
                bf => bf.FieldU8(5, (t, v) => t.Category = v)
                        .FieldU8(5, (t, v) => t.Subcategory = v)
                        .FieldS16(9, (t, v) => t.Unknown38_10 = v)
                        .FieldU8(7, (t, v) => t.Element = v)
                        .FieldU8(6, (t, v) => t.Unknown38_26 = v),
                "raw38");
            reader.ReadBitfield(
                this,
                bf => bf.FieldS16(13, (t, v) => t.Id = v)
                        .FieldS16(13, (t, v) => t.Unknown3C_13 = v)
                        .FieldU8(6, (t, v) => t.Unknown3C_26 = v),
                "raw3C");
            this.ModelFlags = reader.ReadU32();
            this.Weight = reader.ReadF32();
            this.Price = reader.ReadS32();
            this.Value = reader.ReadS32();
            this.Unknown50 = reader.ReadU32();
            this.Unknown54 = reader.ReadF32();
            reader.ReadBitfield(
                this,
                bf => bf.FieldB8((t, v) => t.Unknown58_0 = v)
                        .FieldB8((t, v) => t.Unknown58_1 = v)
                        .FieldB8((t, v) => t.Unknown58_2 = v)
                        .FieldB8((t, v) => t.Unknown58_3 = v)
                        .FieldB8((t, v) => t.Unknown58_4 = v)
                        .FieldB8((t, v) => t.Unknown58_5 = v)
                        .FieldB8((t, v) => t.Unknown58_6 = v)
                        .FieldB8((t, v) => t.Unknown58_7 = v)
                        .FieldB8((t, v) => t.Unknown58_8 = v)
                        .FieldB8((t, v) => t.Unknown58_9 = v)
                        .FieldB8((t, v) => t.Unknown58_10 = v)
                        .FieldB8((t, v) => t.Unknown58_11 = v)
                        .FieldB8((t, v) => t.Unknown58_12 = v)
                        .FieldB8((t, v) => t.Unknown58_13 = v)
                        .FieldB8((t, v) => t.Unknown58_14 = v)
                        .FieldB8((t, v) => t.Unknown58_15 = v)
                        .FieldB8((t, v) => t.Unknown58_16 = v)
                        .FieldB8((t, v) => t.Unknown58_17 = v)
                        .FieldB8((t, v) => t.Unknown58_18 = v)
                        .FieldB8((t, v) => t.Unknown58_19 = v)
                        .FieldB8((t, v) => t.Unknown58_20 = v)
                        .FieldB8((t, v) => t.Unknown58_21 = v)
                        .FieldB8((t, v) => t.Unknown58_22 = v)
                        .FieldB8((t, v) => t.Unknown58_23 = v)
                        .FieldB8((t, v) => t.Unknown58_24 = v)
                        .FieldB8((t, v) => t.Unknown58_25 = v)
                        .FieldB8((t, v) => t.Unknown58_26 = v)
                        .FieldB8((t, v) => t.Unknown58_27 = v)
                        .FieldB8((t, v) => t.Unknown58_28 = v)
                        .FieldB8((t, v) => t.Unknown58_29 = v)
                        .FieldB8((t, v) => t.Unknown58_30 = v)
                        .FieldB8((t, v) => t.Unknown58_31 = v),
                "raw58");
            this.Unknown5C = reader.ReadU32();
            this.Unknown60 = reader.ReadU16();
            this.Unknown62 = reader.ReadU16();
            this.Unknown64 = reader.ReadU16();
            this.ExpectedLevel = reader.ReadU16();
            this.Unknown68 = reader.ReadU8();
            this.Unknown69 = reader.ReadU8();
            this.Unknown6A = reader.ReadU8();
            this.Unknown6B = reader.ReadU8();
            this.Unknown6C = reader.ReadU8();
            this.Unknown6D = reader.ReadU8();
            this.Unknown6E = reader.ReadU8();
            this.Unknown6F = reader.ReadU8();
            this.RecoverHealth = reader.ReadU16();
            this.RecoverStamina = reader.ReadU16();
            this.Unknown74 = reader.ReadF32();
            this.Unknown78 = reader.ReadU32();
            this.Unknown7C = reader.ReadU16();
            this.Unknown7E = reader.ReadU8();
            this.Unknown7F = reader.ReadU8();
        }

        public void Save(IResourceWriter writer)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("{0}", this.Id);
        }
    }
}
