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
        public void WriteObject(IDsonObjectWriter writer, ref Stack<T> inst, Type declaredType, ObjectStyle style) {
            Type eleDeclaredType = typeof(T);
            // 重复编码以避免Itr装箱
            foreach (T item in inst) {
                writer.WriteObject<T>(null, in item, eleDeclaredType);
            }
        }

        public Stack<T> ReadObject(IDsonObjectReader reader, Func<Stack<T>>? factory = null) {
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
        public void WriteObject(IDsonObjectWriter writer, ref Queue<T> inst, Type declaredType, ObjectStyle style) {
            Type eleDeclaredType = typeof(T);
            // 重复编码以避免Itr装箱
            foreach (T item in inst) {
                writer.WriteObject<T>(null, in item, eleDeclaredType);
            }
        }

        public Queue<T> ReadObject(IDsonObjectReader reader, Func<Queue<T>>? factory = null) {
            Type eleDeclaredType = typeof(T);
            // Queue重复编码，避免不必要的拷贝
            Queue<T> result = new Queue<T>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>(null, eleDeclaredType);
                result.Enqueue(value);
            }
            return result;
        }
    }

    #endregion

    #region 特化List

    public class IntListCodec : IDsonCodec<IList<int>>
    {
        private readonly Type typeInfo;

        public IntListCodec(Type typeInfo) {
            this.typeInfo = typeInfo;
        }

        public Type GetEncoderType() => typeInfo;

        public void WriteObject(IDsonObjectWriter writer, ref IList<int> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteInt(null, inst[i]);
            }
        }

        public IList<int> ReadObject(IDsonObjectReader reader, Func<IList<int>>? factory = null) {
            IList<int> result = new List<int>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int value = reader.ReadInt(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<int>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<long> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteLong(null, inst[i]);
            }
        }

        public IList<long> ReadObject(IDsonObjectReader reader, Func<IList<long>>? factory = null) {
            IList<long> result = new List<long>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long value = reader.ReadLong(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<long>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<float> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteFloat(null, inst[i]);
            }
        }

        public IList<float> ReadObject(IDsonObjectReader reader, Func<IList<float>>? factory = null) {
            IList<float> result = new List<float>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                float value = reader.ReadFloat(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<float>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<double> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteDouble(null, inst[i]);
            }
        }

        public IList<double> ReadObject(IDsonObjectReader reader, Func<IList<double>>? factory = null) {
            IList<double> result = new List<double>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                double value = reader.ReadDouble(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<double>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<bool> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteBool(null, inst[i]);
            }
        }

        public IList<bool> ReadObject(IDsonObjectReader reader, Func<IList<bool>>? factory = null) {
            IList<bool> result = new List<bool>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                bool value = reader.ReadBool(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<bool>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<string> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteString(null, inst[i]);
            }
        }

        public IList<string> ReadObject(IDsonObjectReader reader, Func<IList<string>>? factory = null) {
            IList<string> result = new List<string>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string value = reader.ReadString(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<string>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<uint> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteUint(null, inst[i]);
            }
        }

        public IList<uint> ReadObject(IDsonObjectReader reader, Func<IList<uint>>? factory = null) {
            IList<uint> result = new List<uint>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint value = reader.ReadUint(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<uint>.CreateRange(result)
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

        public void WriteObject(IDsonObjectWriter writer, ref IList<ulong> inst, Type declaredType, ObjectStyle style) {
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteUlong(null, inst[i]);
            }
        }

        public IList<ulong> ReadObject(IDsonObjectReader reader, Func<IList<ulong>>? factory = null) {
            IList<ulong> result = new List<ulong>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong value = reader.ReadUlong(null);
                result.Add(value);
            }
            return reader.Options.readAsImmutable
                ? ImmutableList<ulong>.CreateRange(result)
                : result;
        }
    }

    #endregion
}
}