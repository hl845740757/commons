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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 提供常用集合类型的Codec
/// </summary>
public static class MoreCollectionCodecs
{
    #region 特殊集合

    /// <summary>
    /// <see cref="Stack{T}"/>不是<see cref="ICollection{T}"/>的子类......
    /// 具体类型不支持读取为不可变集合 —— 队列这种对象也不是拿来查询数据的。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StackCodec<T> : IDsonCodec<Stack<T>>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in Stack<T> inst, Type declaredType, ObjectStyle style) {
            // 重复编码以避免Itr装箱
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Count);
            foreach (T item in inst) {
                writer.WriteObject<T>(null, in item);
            }
            writer.WriteEndArray();
        }

        public Stack<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            List<T> list = EnumerableCodec<T>.ReadAsList(reader);
            // Stack并未实现ICollection接口，另外我们需要保持与序列化之前相同的顺序，需要将list反向转换为Stack
            Stack<T> result = new Stack<T>(list.Count);
            for (int idx = list.Count - 1; idx >= 0; idx--) {
                result.Push(list[idx]);
            }
            return result;
        }
    }

    /// <summary>
    /// <see cref="Queue{T}"/>也不是<see cref="ICollection{T}"/>的子类...
    /// 具体类型不支持读取为不可变集合 —— 队列这种对象也不是拿来查询数据的。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueCodec<T> : IDsonCodec<Queue<T>>
    {
        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in Queue<T> inst, Type declaredType, ObjectStyle style) {
            // 重复编码以避免Itr装箱
            writer.WriteStartArray(style, inst.GetType(), declaredType, inst.Count);
            foreach (T item in inst) {
                writer.WriteObject<T>(null, in item);
            }
            writer.WriteEndArray();
        }

        public Queue<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // Queue重复编码，避免不必要的拷贝
            int count = reader.ReadStartArray();
            Queue<T> result = new Queue<T>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>(null);
                result.Enqueue(value);
            }
            reader.ReadEndArray();
            return result;
        }
    }

    #endregion

    #region 特化List

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IList<T> ToImmutable<T>(IList<T> list, Type declaredType) {
        // 需要确保ImmutableList能赋值给声明类型
        if (declaredType.IsGenericType) {
            if (declaredType.GetGenericTypeDefinition() == typeof(ImmutableList<>)
                || declaredType.GetGenericTypeDefinition() == typeof(ISet<>)
                || declaredType.GetGenericTypeDefinition() == typeof(IGenericSet<>)) {
                return ImmutableList<T>.CreateRange(list);
            }
        }
        return list;
    }

    public class IntListCodec : IDsonCodec<IList<int>>
    {
        private readonly Type typeInfo;

        public IntListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<int> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteInt(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<int> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<int> result = new List<int>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int value = reader.ReadInt(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class LongListCodec : IDsonCodec<IList<long>>
    {
        private readonly Type typeInfo;

        public LongListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<long> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteLong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<long> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<long> result = new List<long>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long value = reader.ReadLong(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class FloatListCodec : IDsonCodec<IList<float>>
    {
        private readonly Type typeInfo;

        public FloatListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<float> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteFloat(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<float> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<float> result = new List<float>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                float value = reader.ReadFloat(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class DoubleListCodec : IDsonCodec<IList<double>>
    {
        private readonly Type typeInfo;

        public DoubleListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<double> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteDouble(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<double> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<double> result = new List<double>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                double value = reader.ReadDouble(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class BoolListCodec : IDsonCodec<IList<bool>>
    {
        private readonly Type typeInfo;

        public BoolListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<bool> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteBool(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<bool> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<bool> result = new List<bool>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                bool value = reader.ReadBool(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class StringListCodec : IDsonCodec<IList<string>>
    {
        private readonly Type typeInfo;

        public StringListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<string> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteString(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<string> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<string> result = new List<string>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string value = reader.ReadString(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class UIntListCodec : IDsonCodec<IList<uint>>
    {
        private readonly Type typeInfo;

        public UIntListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<uint> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteUInt(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<uint> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<uint> result = new List<uint>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint value = reader.ReadUInt(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    public class ULongListCodec : IDsonCodec<IList<ulong>>
    {
        private readonly Type typeInfo;

        public ULongListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public bool AutoStartEnd => false;

        public void WriteObject(IDsonObjectWriter writer, in IList<ulong> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(style, typeInfo, declaredType, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteULong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public IList<ulong> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray();
            IList<ulong> result = new List<ulong>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong value = reader.ReadULong(null);
                result.Add(value);
            }
            reader.ReadEndArray();
            //
            return reader.Options.readAsImmutable
                ? ToImmutable(result, declaredType)
                : result;
        }
    }

    #endregion
}
}