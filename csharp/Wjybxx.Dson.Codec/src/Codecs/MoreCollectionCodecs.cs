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
using System.Collections.Concurrent;
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
    #region 集合

    public class ListCodec<T> : CollectionCodec, IDsonCodec<List<T>>
    {
        private static readonly Func<List<T>> _factory = () => new List<T>();

        public void WriteObject(IDsonObjectWriter writer, ref List<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public List<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<T>>? factory = null) {
            return (List<T>)CollectionCodec.ReadCollection(reader, declaredType, factory ?? _factory);
        }
    }

    public class IListCodec<T> : CollectionCodec, IDsonCodec<IList<T>>
    {
        private static readonly Func<IList<T>> _factory = () => new List<T>();

        public void WriteObject(IDsonObjectWriter writer, ref IList<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public IList<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<IList<T>>? factory = null) {
            return (IList<T>)CollectionCodec.ReadCollection(reader, declaredType, factory ?? _factory);
        }
    }

    public class HashSetCodec<T> : CollectionCodec, IDsonCodec<HashSet<T>>
    {
        private static readonly Func<HashSet<T>> _factory = () => new HashSet<T>();

        public void WriteObject(IDsonObjectWriter writer, ref HashSet<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public HashSet<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<HashSet<T>>? factory = null) {
            return (HashSet<T>)CollectionCodec.ReadCollection(reader, declaredType, factory ?? _factory);
        }
    }

    public class ISetCodec<T> : CollectionCodec, IDsonCodec<ISet<T>>
    {
        private static readonly Func<ISet<T>> _factory = () => new HashSet<T>();

        public void WriteObject(IDsonObjectWriter writer, ref ISet<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public ISet<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<ISet<T>>? factory = null) {
            return (ISet<T>)CollectionCodec.ReadCollection(reader, declaredType, factory ?? _factory);
        }
    }

    public class LinkedHashSetCodec<T> : CollectionCodec, IDsonCodec<LinkedHashSet<T>>
    {
        private static readonly Func<LinkedHashSet<T>> _factory = () => new LinkedHashSet<T>();

        public void WriteObject(IDsonObjectWriter writer, ref LinkedHashSet<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public LinkedHashSet<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<LinkedHashSet<T>>? factory = null) {
            return (LinkedHashSet<T>)CollectionCodec.ReadCollection(reader, declaredType, factory ?? _factory);
        }
    }

    #endregion

    #region 字典

    public class DictionaryCodec<K, V> : DictionaryCodec, IDsonCodec<Dictionary<K, V>>
    {
        private static readonly Func<Dictionary<K, V>>? _factory = () => new Dictionary<K, V>();

        public void WriteObject(IDsonObjectWriter writer, ref Dictionary<K, V> inst, Type declaredType, ObjectStyle style) {
            WriteDictionary(writer, inst, declaredType, style);
        }

        public Dictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<Dictionary<K, V>>? factory = null) {
            return (Dictionary<K, V>)ReadDictionary(reader, declaredType, factory ?? _factory);
        }
    }

    public class LinkedDictionaryCodec<K, V> : DictionaryCodec, IDsonCodec<LinkedDictionary<K, V>>
    {
        private static readonly Func<LinkedDictionary<K, V>> _factory = () => new LinkedDictionary<K, V>();

        public void WriteObject(IDsonObjectWriter writer, ref LinkedDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
            DictionaryCodec.WriteDictionary(writer, inst, declaredType, style);
        }

        public LinkedDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<LinkedDictionary<K, V>>? factory = null) {
            return (LinkedDictionary<K, V>)DictionaryCodec.ReadDictionary(reader, declaredType, factory ?? _factory);
        }
    }

    public class ConcurrentDictionaryCodec<K, V> : DictionaryCodec, IDsonCodec<ConcurrentDictionary<K, V>>
    {
        private static readonly Func<ConcurrentDictionary<K, V>> _factory = () => new ConcurrentDictionary<K, V>();

        public void WriteObject(IDsonObjectWriter writer, ref ConcurrentDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
            DictionaryCodec.WriteDictionary(writer, inst, declaredType, style);
        }

        public ConcurrentDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<ConcurrentDictionary<K, V>>? factory = null) {
            return (ConcurrentDictionary<K, V>)DictionaryCodec.ReadDictionary(reader, declaredType, factory ?? _factory);
        }
    }

    #endregion

    #region 特殊集合

    /// <summary>
    /// <see cref="Stack{T}"/>不是<see cref="ICollection{T}"/>的子类......
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StackCodec<T> : IDsonCodec<Stack<T>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref Stack<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public Stack<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<Stack<T>>? factory = null) {
            IList<T> list = (IList<T>)CollectionCodec.ReadCollection<T>(reader, declaredType);
            // Stack并未实现ICollection接口，另外我们需要保持与序列化之前相同的顺序，需要将list反向转换为Stack
            Stack<T> result = factory != null ? factory.Invoke() : new Stack<T>(list.Count);
            for (int idx = list.Count - 1; idx >= 0; idx--) {
                result.Push(list[idx]);
            }
            return result;
        }
    }

    /// <summary>
    /// <see cref="Queue{T}"/>也不是<see cref="ICollection{T}"/>的子类...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueCodec<T> : IDsonCodec<Queue<T>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref Queue<T> inst, Type declaredType, ObjectStyle style) {
            CollectionCodec.WriteCollection(writer, inst, declaredType, style);
        }

        public Queue<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<Queue<T>>? factory = null) {
            Type[] genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
            Type eleDeclaredType = genericTypeArguments.Length > 0 ? declaredType.GenericTypeArguments[0] : typeof(object);
            // Queue重复编码，避免不必要的拷贝
            Queue<T> result = factory != null ? factory.Invoke() : new Queue<T>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>(null, eleDeclaredType);
                result.Enqueue(value);
            }
            return result;
        }
    }

    #endregion

    #region 特化List

    public class IntListCodec : CollectionCodec, IDsonCodec<List<int>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<int> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteInt(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<int> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<int>>? factory = null) {
            List<int> result = new List<int>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int value = reader.ReadInt(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class LongListCodec : CollectionCodec, IDsonCodec<List<long>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<long> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteLong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<long> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<long>>? factory = null) {
            List<long> result = new List<long>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long value = reader.ReadLong(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class FloatListCodec : CollectionCodec, IDsonCodec<List<float>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<float> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteFloat(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<float> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<float>>? factory = null) {
            List<float> result = new List<float>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                float value = reader.ReadFloat(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class DoubleListCodec : CollectionCodec, IDsonCodec<List<double>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<double> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteDouble(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<double> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<double>>? factory = null) {
            List<double> result = new List<double>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                double value = reader.ReadDouble(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class BoolListCodec : CollectionCodec, IDsonCodec<List<bool>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<bool> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteBool(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<bool> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<bool>>? factory = null) {
            List<bool> result = new List<bool>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                bool value = reader.ReadBool(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class StringListCodec : CollectionCodec, IDsonCodec<List<string>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<string> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteString(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<string> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<string>>? factory = null) {
            List<string> result = new List<string>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string value = reader.ReadString(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class UIntListCodec : CollectionCodec, IDsonCodec<List<uint>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<uint> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteUint(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<uint> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<uint>>? factory = null) {
            List<uint> result = new List<uint>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint value = reader.ReadUint(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class ULongListCodec : CollectionCodec, IDsonCodec<List<ulong>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<ulong> inst, Type declaredType, ObjectStyle style) {
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteUlong(null, inst[i]);
            }
            writer.WriteEndArray();
        }

        public List<ulong> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<ulong>>? factory = null) {
            List<ulong> result = new List<ulong>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong value = reader.ReadUlong(null);
                result.Add(value);
            }
            return result;
        }
    }

    public class ObjectListCodec : CollectionCodec, IDsonCodec<List<object>>
    {
        public void WriteObject(IDsonObjectWriter writer, ref List<object> inst, Type declaredType, ObjectStyle style) {
            Type eleType = typeof(object);
            writer.WriteStartArray(inst, declaredType, style);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteObject(null, inst[i], eleType);
            }
            writer.WriteEndArray();
        }

        public List<object> ReadObject(IDsonObjectReader reader, Type declaredType, Func<List<object>>? factory = null) {
            Type eleType = typeof(object);
            List<object> result = new List<object>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                object value = reader.ReadObject<object>(null, eleType);
                result.Add(value);
            }
            return result;
        }
    }

    #endregion
}
}