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
        public void WriteObject(IDsonObjectWriter writer, Stack<T> inst, Type declaredType, SerializeFeatures features) {
            SerializeFeatures selfFeatures = features.ErasureElementFeatures();
            SerializeFeatures elementFeatures = features.GetElementFeatures();
            // 重复编码以避免Itr装箱
            writer.WriteStartArray(inst.GetType(), declaredType, selfFeatures, inst.Count);
            foreach (T item in inst) {
                writer.WriteObject(in item, elementFeatures);
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
        public void WriteObject(IDsonObjectWriter writer, Queue<T> inst, Type declaredType, SerializeFeatures features) {
            SerializeFeatures selfFeatures = features.ErasureElementFeatures();
            SerializeFeatures elementFeatures = features.GetElementFeatures();
            // 重复编码以避免Itr装箱
            writer.WriteStartArray(inst.GetType(), declaredType, selfFeatures, inst.Count);
            foreach (T item in inst) {
                writer.WriteObject(in item, elementFeatures);
            }
            writer.WriteEndArray();
        }

        public Queue<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            // Queue重复编码，避免不必要的拷贝
            int count = reader.ReadStartArray().count;
            Queue<T> result = new Queue<T>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>();
                result.Enqueue(value);
            }
            reader.ReadEndArray();
            return result;
        }
    }

    public class SmallDynamicArrayCodec<T> : IDsonCodec<SmallDynamicArray<T>> where T : class
    {
        public void WriteObject(IDsonObjectWriter writer, SmallDynamicArray<T> inst, Type declaredType, SerializeFeatures features) {
            SerializeFeatures selfFeatures = features.ErasureElementFeatures();
            SerializeFeatures elementFeatures = features.GetElementFeatures();
            writer.WriteStartArray(inst.GetType(), declaredType, selfFeatures, inst.ElementCount);
            inst.BeginItr();
            try {
                for (int i = 0, len = inst.Length; i < len; i++) {
                    T item = inst[i];
                    if (item != null) {
                        writer.WriteObject(item, elementFeatures);
                    }
                }
            }
            finally {
                inst.EndItr();
            }
            writer.WriteEndArray();
        }

        public SmallDynamicArray<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray().count;
            SmallDynamicArray<T> result = new SmallDynamicArray<T>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>();
                result.Add(value);
            }
            reader.ReadEndArray();
            return result;
        }
    }

    public class DynamicArrayCodec<T> : IDsonCodec<DynamicArray<T>> where T : class
    {
        public void WriteObject(IDsonObjectWriter writer, DynamicArray<T> inst, Type declaredType, SerializeFeatures features) {
            SerializeFeatures selfFeatures = features.ErasureElementFeatures();
            SerializeFeatures elementFeatures = features.GetElementFeatures();
            writer.WriteStartArray(inst.GetType(), declaredType, selfFeatures, inst.ElementCount);
            inst.BeginItr();
            try {
                for (int i = 0, len = inst.Length; i < len; i++) {
                    T item = inst[i];
                    if (item != null) {
                        writer.WriteObject(item, elementFeatures);
                    }
                }
            }
            finally {
                inst.EndItr();
            }
            writer.WriteEndArray();
        }

        public DynamicArray<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
            int count = reader.ReadStartArray().count;
            DynamicArray<T> result = new DynamicArray<T>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>();
                result.Add(value);
            }
            reader.ReadEndArray();
            return result;
        }
    }

    #endregion
}
}