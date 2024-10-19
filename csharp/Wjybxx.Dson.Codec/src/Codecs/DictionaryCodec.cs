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
/// 字典通用编解码器
/// 
/// </summary>
public class DictionaryCodec<K, V> : IDsonCodec<IDictionary<K, V>>
{
    private readonly Type encoderType; // KV应当和encoderType的泛型参数相同，因为Codec就是根据encoderType的泛型参数构建的
    private readonly Func<IDictionary<K, V>>? factory;
    private readonly FactoryKind factoryKind; // 处理默认情况
    private readonly KeyKind keyKind;

    /// <summary>
    /// 动态构建Codec时调用
    /// </summary>
    /// <param name="encoderType"></param>
    /// <param name="factory"></param>
    public DictionaryCodec(Type encoderType, Func<IDictionary<K, V>>? factory = null) {
        this.encoderType = encoderType;
        this.factory = factory;
        if (factory == null) {
            this.factoryKind = ComputeFactoryKind(encoderType);
        }
        // 缓存key的类型
        if (typeof(K) == typeof(int)) {
            keyKind = KeyKind.Int32;
        } else if (typeof(K) == typeof(long)) {
            keyKind = KeyKind.Int64;
        } else if (typeof(K) == typeof(uint)) {
            keyKind = KeyKind.Uint32;
        } else if (typeof(K) == typeof(ulong)) {
            keyKind = KeyKind.Uint64;
        } else {
            keyKind = KeyKind.Generic;
        }
    }

    private enum FactoryKind
    {
        Unknown,
        LinkedDictionary,
        ConcurrentDictionary,
    }

    private enum KeyKind
    {
        Generic,
        Int32,
        Int64,
        Uint32,
        Uint64
    }

    private static FactoryKind ComputeFactoryKind(Type typeInfo) {
        if (typeInfo == typeof(LinkedDictionary<K, V>)
            || typeInfo == typeof(IGenericDictionary<K, V>)) {
            return FactoryKind.LinkedDictionary;
        }
        if (typeInfo == typeof(ConcurrentDictionary<K, V>)) {
            return FactoryKind.ConcurrentDictionary;
        }
        // IDictionary接口类型根据配置决定
        return FactoryKind.Unknown;
    }

    /** 字典需要自行控制start/end，和是否写为数组 */
    public bool AutoStartEnd => false;

    public Type GetEncoderType() => encoderType;

    /** <see cref="encoderType"/>一定是用户declaredType的子类型，因此创建实例时不依赖declaredType */
    private IDictionary<K, V> NewDictionary() {
        if (factory != null) return factory.Invoke();
        return factoryKind switch
        {
            FactoryKind.LinkedDictionary => new LinkedDictionary<K, V>(),
            FactoryKind.ConcurrentDictionary => new ConcurrentDictionary<K, V>(),
            _ => new Dictionary<K, V>()
        };
    }

    protected virtual IDictionary<K, V> ToImmutable(IDictionary<K, V> dictionary) {
        return ImmutableLinkedDictionary<K, V>.CreateRange(dictionary);
    }

    public void WriteObject(IDsonObjectWriter writer, ref IDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
        if (writer.Options.writeMapAsDocument) {
            if (keyKind == KeyKind.Int32) { // 这里转换字符串必定丢失类型，因此是判断实际类型(K)是安全的
                WriteDictionaryInt(writer, (IDictionary<int, V>)inst, declaredType, style);
            } else if (keyKind == KeyKind.Int64) {
                WriteDictionaryLong(writer, (IDictionary<long, V>)inst, declaredType, style);
            } else {
                WriteDictionaryObject(writer, inst, declaredType, style);
            }
        } else {
            Type keyDeclaredType = typeof(K);
            Type valDeclaredType = typeof(V);
            if (keyKind == KeyKind.Int32) {
                // int2object
                IDictionary<int, V> int2ObjDic = (IDictionary<int, V>)inst;
                writer.WriteStartArray(style, encoderType, declaredType);
                foreach (KeyValuePair<int, V> pair in int2ObjDic) {
                    writer.WriteInt(null, pair.Key);
                    writer.WriteObject(null, pair.Value, valDeclaredType);
                }
                writer.WriteEndArray();
            } else if (keyKind == KeyKind.Int64) {
                // long2object
                IDictionary<long, V> long2ObjDic = (IDictionary<long, V>)inst;
                writer.WriteStartArray(style, encoderType, declaredType);
                foreach (KeyValuePair<long, V> pair in long2ObjDic) {
                    writer.WriteLong(null, pair.Key);
                    writer.WriteObject(null, pair.Value, valDeclaredType);
                }
                writer.WriteEndArray();
            } else {
                // generic
                writer.WriteStartArray(style, encoderType, declaredType);
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteObject(null, pair.Key, keyDeclaredType);
                    writer.WriteObject(null, pair.Value, valDeclaredType);
                }
                writer.WriteEndArray();
            }
        }
    }

    public IDictionary<K, V> ReadObject(IDsonObjectReader reader, Func<IDictionary<K, V>>? factory = null) {
        IDictionary<K, V> result = factory != null ? factory() : NewDictionary();
        if (reader.Options.writeMapAsDocument) {
            if (keyKind == KeyKind.Int32) {
                ReadDictionaryInt(reader, (IDictionary<int, V>)result);
            } else if (keyKind == KeyKind.Int64) {
                ReadDictionaryLong(reader, (IDictionary<long, V>)result);
            } else {
                ReadDictionaryObject(reader, result);
            }
        } else {
            Type keyDeclaredType = typeof(K);
            Type valDeclaredType = typeof(V);
            if (keyKind == KeyKind.Int32) {
                // int2object
                IDictionary<int, V> int2ObjDic = (IDictionary<int, V>)result;
                reader.ReadStartArray();
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    int key = reader.ReadInt(null);
                    V value = reader.ReadObject<V>(null, valDeclaredType);
                    int2ObjDic[key] = value;
                }
                reader.ReadEndArray();
            } else if (keyKind == KeyKind.Int64) {
                // long2object
                IDictionary<long, V> long2ObjDic = (IDictionary<long, V>)result;
                reader.ReadStartArray();
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    long key = reader.ReadLong(null);
                    V value = reader.ReadObject<V>(null, valDeclaredType);
                    long2ObjDic[key] = value;
                }
                reader.ReadEndArray();
            } else {
                // generic
                reader.ReadStartArray();
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    K key = reader.ReadObject<K>(null, keyDeclaredType);
                    V value = reader.ReadObject<V>(null, valDeclaredType);
                    result[key] = value;
                }
                reader.ReadEndArray();
            }
        }
        return reader.Options.readAsImmutable ? ToImmutable(result) : result;
    }

    #region write

    // 通过重复编码避免拆装箱
    private void WriteDictionaryObject(IDsonObjectWriter writer, IDictionary<K, V> inst,
                                       Type declaredType, ObjectStyle style) {
        Type valDeclaredType = typeof(V);
        writer.WriteStartObject(style, encoderType, declaredType);
        foreach (KeyValuePair<K, V> pair in inst) {
            string keyString = writer.EncodeKey(pair.Key);
            V value = pair.Value;
            if (value == null) {
                // 字典写为普通对象时，必须写入null，否则containsKey会异常；要强制写入null，必须先写入name
                writer.WriteName(keyString);
                writer.WriteNull(keyString);
            } else {
                writer.WriteObject(keyString, value, valDeclaredType);
            }
        }
        writer.WriteEndObject();
    }

    private void WriteDictionaryInt(IDsonObjectWriter writer, IDictionary<int, V> inst,
                                    Type declaredType, ObjectStyle style) {
        Type valDeclaredType = typeof(V);
        writer.WriteStartObject(style, encoderType, declaredType);
        foreach (KeyValuePair<int, V> pair in inst) {
            string keyString = pair.Key.ToString();
            V value = pair.Value;
            if (value == null) {
                // 字典写为普通对象时，必须写入null，否则containsKey会异常；要强制写入null，必须先写入name
                writer.WriteName(keyString);
                writer.WriteNull(keyString);
            } else {
                writer.WriteObject(keyString, value, valDeclaredType);
            }
        }
        writer.WriteEndObject();
    }

    private void WriteDictionaryLong(IDsonObjectWriter writer, IDictionary<long, V> inst,
                                     Type declaredType, ObjectStyle style) {
        Type valDeclaredType = typeof(V);
        writer.WriteStartObject(style, encoderType, declaredType);
        foreach (KeyValuePair<long, V> pair in inst) {
            string keyString = pair.Key.ToString();
            V value = pair.Value;
            if (value == null) {
                // 字典写为普通对象时，必须写入null，否则containsKey会异常；要强制写入null，必须先写入name
                writer.WriteName(keyString);
                writer.WriteNull(keyString);
            } else {
                writer.WriteObject(keyString, value, valDeclaredType);
            }
        }
        writer.WriteEndObject();
    }

    #endregion

    #region read

    // 通过重复编码避免拆装箱
    private void ReadDictionaryObject(IDsonObjectReader reader, IDictionary<K, V> result) {
        Type valDeclaredType = typeof(V);
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            K key = reader.DecodeKey<K>(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    private void ReadDictionaryInt(IDsonObjectReader reader, IDictionary<int, V> result) {
        Type valDeclaredType = typeof(V);
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            int key = int.Parse(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    private void ReadDictionaryLong(IDsonObjectReader reader, IDictionary<long, V> result) {
        Type valDeclaredType = typeof(V);
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            long key = long.Parse(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    #endregion
}
}