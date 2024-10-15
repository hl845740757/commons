#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 更多基础类型Codec
/// </summary>
public static class MorePrimitiveCodecs
{
    public class UInt32Codec : IDsonCodec<uint>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref uint inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(uint)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, (int)inst, WireType.Uint, numberStyle);
        }

        public uint ReadObject(IDsonObjectReader reader, Func<uint>? factory = null) {
            return (uint)reader.ReadInt(null);
        }
    }

    public class UInt64Codec : IDsonCodec<ulong>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref ulong inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(ulong)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteLong(null, (long)inst, WireType.Uint, numberStyle);
        }

        public ulong ReadObject(IDsonObjectReader reader, Func<ulong>? factory = null) {
            return (ulong)reader.ReadLong(null);
        }
    }

    public class ShortCodec : IDsonCodec<short>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref short inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(short)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, inst, WireType.Sint, numberStyle);
        }

        public short ReadObject(IDsonObjectReader reader, Func<short>? factory = null) {
            return (short)reader.ReadInt(null);
        }
    }

    public class UShortCodec : IDsonCodec<ushort>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref ushort inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(ushort)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, inst, WireType.Uint, numberStyle);
        }

        public ushort ReadObject(IDsonObjectReader reader, Func<ushort>? factory = null) {
            return (ushort)reader.ReadInt(null);
        }
    }

    public class ByteCodec : IDsonCodec<byte>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref byte inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(byte)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, inst, WireType.Uint, numberStyle); // c# byte是无符号数
        }

        public byte ReadObject(IDsonObjectReader reader, Func<byte>? factory = null) {
            return (byte)reader.ReadInt(null);
        }
    }

    public class SByteCodec : IDsonCodec<sbyte>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref sbyte inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(sbyte)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, inst, WireType.Sint, numberStyle);
        }

        public sbyte ReadObject(IDsonObjectReader reader, Func<sbyte>? factory = null) {
            return (sbyte)reader.ReadInt(null);
        }
    }

    public class CharCodec : IDsonCodec<char>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, ref char inst, Type declaredType, ObjectStyle style) {
            INumberStyle numberStyle = declaredType == typeof(char)
                ? NumberStyles.Unsigned
                : NumberStyles.TypedUnsigned;
            writer.WriteInt(null, inst, WireType.Uint, numberStyle);
        }

        public char ReadObject(IDsonObjectReader reader, Func<char>? factory = null) {
            return (char)reader.ReadInt(null);
        }
    }
}
}