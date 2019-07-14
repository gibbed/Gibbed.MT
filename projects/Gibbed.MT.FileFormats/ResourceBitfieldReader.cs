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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gibbed.MT.FileFormats
{
    internal class ResourceBitfieldReader<T> : IBitfieldReader<T>
    {
        public delegate Expression FieldExpression(Expression reader, Expression value);

        private readonly List<KeyValuePair<int, Delegate>> _Pairs;

        public ResourceBitfieldReader()
        {
            this._Pairs = new List<KeyValuePair<int, Delegate>>();
        }

        public Expression BuildExpression(Expression reader, Expression target)
        {
            var bitCount = this._Pairs.Sum(p => p.Key);
            var valueType = GetTypeForBitCount(bitCount);

            var value = Expression.Variable(valueType, "value");

            var blocks = new List<Expression>();
            blocks.Add(Expression.Assign(
                value,
                Expression.Call(reader, typeof(ResourceReader).GetMethod("ReadU" + bitCount))));

            var fieldBitIndex = 0;
            foreach (var pair in this._Pairs)
            {
                var fieldBitCount = pair.Key;
                var fieldAction = pair.Value;
                var fieldType = fieldAction.Method.GetParameters()[1].ParameterType;

                var dummy = Expression.Variable(fieldType, "field_" + fieldBitIndex);
                var block = Expression.Block(
                    new[] { dummy },
                    fieldType == typeof(bool)
                        ? Expression.Call(
                            reader,
                            typeof(ResourceReader).GetMethod(
                                "ExtractBit",
                                BindingFlags.Instance | BindingFlags.NonPublic,
                                null,
                                new[]
                                {
                                    valueType,
                                    typeof(int),
                                    fieldType.MakeByRefType()
                                },
                                null),
                            value,
                            Expression.Constant(fieldBitIndex),
                            dummy)
                        : Expression.Call(
                            reader,
                            typeof(ResourceReader).GetMethod(
                                "ExtractBits",
                                BindingFlags.Instance | BindingFlags.NonPublic,
                                null,
                                new[]
                                {
                                    valueType,
                                    typeof(int),
                                    typeof(int),
                                    fieldType.MakeByRefType()
                                },
                                null),
                            value,
                            Expression.Constant(fieldBitIndex),
                            Expression.Constant(fieldBitCount),
                            dummy),
                    Expression.Call(
                        fieldAction.Target == null
                            ? null
                            : Expression.Constant(fieldAction.Target),
                        fieldAction.Method,
                        target,
                        dummy));
                blocks.Add(block);
                fieldBitIndex += fieldBitCount;
            }

            return Expression.Block(new[] { value }, blocks);
        }

        private static Type GetTypeForBitCount(int bitCount)
        {
            switch (bitCount)
            {
                case 8:
                {
                    return typeof(byte);
                }
                case 16:
                {
                    return typeof(ushort);
                }
                case 32:
                {
                    return typeof(uint);
                }
                case 64:
                {
                    return typeof(ulong);
                }
            }
            throw new NotSupportedException();
        }

        public IBitfieldReader<T> FieldB8(Action<T, bool> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(1, action));
            return this;
        }

        public IBitfieldReader<T> FieldS8(int bitCount, Action<T, sbyte> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldU8(int bitCount, Action<T, byte> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldS16(int bitCount, Action<T, short> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldU16(int bitCount, Action<T, ushort> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldS32(int bitCount, Action<T, int> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldU32(int bitCount, Action<T, uint> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldS64(int bitCount, Action<T, long> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }

        public IBitfieldReader<T> FieldU64(int bitCount, Action<T, ulong> action)
        {
            this._Pairs.Add(new KeyValuePair<int, Delegate>(bitCount, action));
            return this;
        }
    }
}
