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
    public class UInt32Codec : IDsonCodec<uint>, IKeyCodec<uint>
    {
        public string EncodeKey(uint value, SerializeFeatures features) {
            if ((int)value < 0) features |= SerializeFeatures.NumberHex;
            return features.ToNumberStyle().ToString((int)value).Value;
        }

        public uint DecodeKey(string keyString) {
            return (uint)DsonTexts.ParseInt32(keyString);
        }

        public void WriteObject(IDsonObjectWriter writer, uint inst, Type declaredType, SerializeFeatures features) {
            if ((int)inst < 0) features |= SerializeFeatures.NumberHex;
            if (declaredType != typeof(uint)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt((int)inst, features);
        }

        public uint ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (uint)reader.ReadInt(features);
        }
    }

    public class UInt64Codec : IDsonCodec<ulong>, IKeyCodec<ulong>
    {
        public string EncodeKey(ulong value, SerializeFeatures features) {
            if ((long)value < 0) features |= SerializeFeatures.NumberHex;
            return features.ToNumberStyle().ToString((long)value).Value;
        }

        public ulong DecodeKey(string keyString) {
            return (ulong)DsonTexts.ParseInt64(keyString);
        }

        public void WriteObject(IDsonObjectWriter writer, ulong inst, Type declaredType, SerializeFeatures features) {
            if ((long)inst < 0) features |= SerializeFeatures.NumberHex;
            if (declaredType != typeof(ulong)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteLong((long)inst, features);
        }

        public ulong ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (ulong)reader.ReadLong(features);
        }
    }

    public class ShortCodec : IDsonCodec<short>, IKeyCodec<short>
    {
        public string EncodeKey(short value, SerializeFeatures features) {
            return features.ToNumberStyle().ToString((int)value).Value;
        }

        public short DecodeKey(string keyString) {
            return (short)DsonTexts.ParseInt32(keyString);
        }

        public void WriteObject(IDsonObjectWriter writer, short inst, Type declaredType, SerializeFeatures features) {
            if (declaredType != typeof(short)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt(inst, features);
        }

        public short ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (short)reader.ReadInt(features);
        }
    }

    public class UShortCodec : IDsonCodec<ushort>, IKeyCodec<ushort>
    {
        public string EncodeKey(ushort value, SerializeFeatures features) {
            return features.ToNumberStyle().ToString((int)value).Value;
        }

        public ushort DecodeKey(string keyString) {
            return (ushort)DsonTexts.ParseInt32(keyString);
        }

        public void WriteObject(IDsonObjectWriter writer, ushort inst, Type declaredType, SerializeFeatures features) {
            if (declaredType != typeof(ushort)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt(inst, features);
        }

        public ushort ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (ushort)reader.ReadInt(features);
        }
    }

    public class ByteCodec : IDsonCodec<byte>
    {
        public void WriteObject(IDsonObjectWriter writer, byte inst, Type declaredType, SerializeFeatures features) {
            if (declaredType != typeof(byte)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt(inst, features); // c# byte是无符号数
        }

        public byte ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (byte)reader.ReadInt(features);
        }
    }

    public class SByteCodec : IDsonCodec<sbyte>
    {
        public void WriteObject(IDsonObjectWriter writer, sbyte inst, Type declaredType, SerializeFeatures features) {
            if (declaredType != typeof(sbyte)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt(inst, features);
        }

        public sbyte ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (sbyte)reader.ReadInt(features);
        }
    }

    public class CharCodec : IDsonCodec<char>
    {
        public void WriteObject(IDsonObjectWriter writer, char inst, Type declaredType, SerializeFeatures features) {
            if (declaredType != typeof(char)) {
                features |= SerializeFeatures.NumberTyped;
            }
            writer.WriteInt(inst, features);
        }

        public char ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
            return (char)reader.ReadInt(features);
        }
    }
}
}