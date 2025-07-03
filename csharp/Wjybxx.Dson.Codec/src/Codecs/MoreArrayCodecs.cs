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
        public void WriteObject(IDsonObjectWriter writer, in byte[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteBinary(null, Binary.CopyFrom(inst)); // 默认拷贝
        }

        public byte[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            Binary binary = reader.ReadBinary(reader.CurrentName);
            return binary.UnsafeBuffer;
        }
    }

    public class IntArrayCodec : IDsonCodec<int[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in int[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteInt(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public int[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<int> result = new List<int>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int value = reader.ReadInt(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class LongArrayCodec : IDsonCodec<long[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in long[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteLong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public long[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<long> result = new List<long>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long value = reader.ReadLong(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class FloatArrayCodec : IDsonCodec<float[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in float[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteFloat(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public float[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<float> result = new List<float>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                float value = reader.ReadFloat(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class DoubleArrayCodec : IDsonCodec<double[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in double[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteDouble(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public double[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<double> result = new List<double>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                double value = reader.ReadDouble(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class BoolArrayCodec : IDsonCodec<bool[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in bool[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteBool(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public bool[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<bool> result = new List<bool>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                bool value = reader.ReadBool(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class StringArrayCodec : IDsonCodec<string[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in string[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteString(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public string[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<string> result = new List<string>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string value = reader.ReadString(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class UIntArrayCodec : IDsonCodec<uint[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in uint[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteUInt(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public uint[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<uint> result = new List<uint>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint value = reader.ReadUInt(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    public class ULongArrayCodec : IDsonCodec<ulong[]>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in ulong[] inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Length);
            for (int i = 0; i < inst.Length; i++) {
                writer.WriteULong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public ulong[] ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // count非精确值，不可以直接创建数组
            int count = reader.ReadStartArray();
            List<ulong> result = new List<ulong>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong value = reader.ReadULong(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            return result.ToArray();
        }
    }

    #endregion
}
}