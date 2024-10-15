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
using System.Collections.Generic;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 为基本类型数组提供定制化的Codec
/// </summary>
public static class MoreArrayCodecs
{
    #region 特化数组

    /** 字节数组需要转Binary */
    public class ByteArrayCodec : IDsonCodec<byte[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref byte[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteBinary(null, Binary.CopyFrom(inst)); // 默认拷贝
        }

        public byte[] ReadObject(IDsonObjectReader reader, Func<byte[]>? factory = null) {
            Binary binary = reader.ReadBinary(reader.CurrentName);
            return binary.UnsafeBuffer;
        }
    }

    public class IntArrayCodec : IDsonCodec<int[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref int[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteInt(null, inst[i]);
            }
        }

        public int[] ReadObject(IDsonObjectReader reader, Func<int[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<int> result = new List<int>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int value = reader.ReadInt(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class LongArrayCodec : IDsonCodec<long[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref long[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteLong(null, inst[i]);
            }
        }

        public long[] ReadObject(IDsonObjectReader reader, Func<long[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<long> result = new List<long>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long value = reader.ReadLong(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class FloatArrayCodec : IDsonCodec<float[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref float[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteFloat(null, inst[i]);
            }
        }

        public float[] ReadObject(IDsonObjectReader reader, Func<float[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<float> result = new List<float>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                float value = reader.ReadFloat(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class DoubleArrayCodec : IDsonCodec<double[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref double[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteDouble(null, inst[i]);
            }
        }

        public double[] ReadObject(IDsonObjectReader reader, Func<double[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<double> result = new List<double>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                double value = reader.ReadDouble(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class BoolArrayCodec : IDsonCodec<bool[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref bool[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteBool(null, inst[i]);
            }
        }

        public bool[] ReadObject(IDsonObjectReader reader, Func<bool[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<bool> result = new List<bool>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                bool value = reader.ReadBool(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class StringArrayCodec : IDsonCodec<string[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref string[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteString(null, inst[i]);
            }
        }

        public string[] ReadObject(IDsonObjectReader reader, Func<string[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<string> result = new List<string>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string value = reader.ReadString(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class UIntArrayCodec : IDsonCodec<uint[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref uint[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteUint(null, inst[i]);
            }
        }

        public uint[] ReadObject(IDsonObjectReader reader, Func<uint[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<uint> result = new List<uint>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint value = reader.ReadUint(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    public class ULongArrayCodec : IDsonCodec<ulong[]>
    {
        public void WriteObject(IDsonObjectWriter writer, ref ulong[] inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteUlong(null, inst[i]);
            }
        }

        public ulong[] ReadObject(IDsonObjectReader reader, Func<ulong[]>? factory = null) {
            // 由于长度未知，只能先存储为List再转...
            List<ulong> result = new List<ulong>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong value = reader.ReadUlong(null);
                result.Add(value);
            }
            return result.ToArray();
        }
    }

    #endregion
}
}