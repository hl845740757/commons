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
/// 字典类的Codec应当继承该类
/// </summary>
public abstract class DictionaryCodec
{
    /** 字典需要自行控制start/end，和是否写为数组 */
    public virtual bool AutoStartEnd => false;
    public virtual bool IsWriteAsArray => false;

    public static void WriteDictionary<K, V>(IDsonObjectWriter writer, in IDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
        Type[]? genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type keyDeclaredType = genericTypeArguments.Length == 2 ? genericTypeArguments[0] : typeof(object);
        Type valDeclaredType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object);

        if (writer.Options.writeMapAsDocument) {
            if (typeof(K) == typeof(int)) { // 这里转换字符串必定丢失类型，因此是判断实际类型(K)是安全的
                WriteDictionaryInt(writer, (IDictionary<int, V>)inst, declaredType, valDeclaredType, style);
            } else if (typeof(K) == typeof(long)) {
                WriteDictionaryLong(writer, (IDictionary<long, V>)inst, declaredType, valDeclaredType, style);
            } else {
                WriteDictionaryObject(writer, inst, declaredType, valDeclaredType, style);
            }
        } else {
            // TODO 这里做优化需要判断-keyDeclaredType
            writer.WriteStartArray(inst, declaredType, style);
            foreach (KeyValuePair<K, V> pair in inst) {
                writer.WriteObject(null, pair.Key, keyDeclaredType);
                writer.WriteObject(null, pair.Value, valDeclaredType);
            }
            writer.WriteEndArray();
        }
    }

    public static IDictionary<K, V> ReadDictionary<K, V>(IDsonObjectReader reader, Type declaredType, Func<IDictionary<K, V>>? factory = null) {
        Type[] genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type keyDeclaredType = genericTypeArguments.Length > 0 ? genericTypeArguments[0] : typeof(object);
        Type valDeclaredType = genericTypeArguments.Length > 0 ? genericTypeArguments[1] : typeof(object);

        IDictionary<K, V> result = NewDictionary(reader, declaredType, factory);
        if (reader.Options.writeMapAsDocument) {
            if (typeof(K) == typeof(int)) {
                ReadDictionaryInt(reader, (IDictionary<int, V>)result, valDeclaredType);
            } else if (typeof(K) == typeof(long)) {
                ReadDictionaryLong(reader, (IDictionary<long, V>)result, valDeclaredType);
            } else {
                ReadDictionaryObject(reader, result, valDeclaredType);
            }
        } else {
            reader.ReadStartArray();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = reader.ReadObject<K>(null, keyDeclaredType);
                V value = reader.ReadObject<V>(null, valDeclaredType);
                result[key] = value;
            }
            reader.ReadEndArray();
        }
        CollectionConverter collectionConverter = reader.Options.collectionConverter;
        if (collectionConverter != null) {
            result = collectionConverter.ConvertDictionary(declaredType, result);
        }
        return result;
    }

    #region write

    // 通过重复编码避免拆装箱
    private static void WriteDictionaryObject<K, V>(IDsonObjectWriter writer, IDictionary<K, V> inst,
                                                    Type declaredType, Type valDeclaredType, ObjectStyle style) {
        writer.WriteStartObject(in inst, declaredType, style);
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

    private static void WriteDictionaryInt<V>(IDsonObjectWriter writer, IDictionary<int, V> inst,
                                              Type declaredType, Type valDeclaredType, ObjectStyle style) {
        writer.WriteStartObject(in inst, declaredType, style);
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

    private static void WriteDictionaryLong<V>(IDsonObjectWriter writer, IDictionary<long, V> inst,
                                               Type declaredType, Type valDeclaredType, ObjectStyle style) {
        writer.WriteStartObject(in inst, declaredType, style);
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
    private static void ReadDictionaryObject<K, V>(IDsonObjectReader reader, IDictionary<K, V> result, Type valDeclaredType) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            K key = reader.DecodeKey<K>(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    private static void ReadDictionaryInt<V>(IDsonObjectReader reader, IDictionary<int, V> result, Type valDeclaredType) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            int key = int.Parse(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    private static void ReadDictionaryLong<V>(IDsonObjectReader reader, IDictionary<long, V> result, Type valDeclaredType) {
        reader.ReadStartObject();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string keyString = reader.ReadName();
            long key = long.Parse(keyString);
            V value = reader.ReadObject<V>(keyString, valDeclaredType);
            result[key] = value;
        }
        reader.ReadEndObject();
    }

    private static IDictionary<K, V> NewDictionary<K, V>(IDsonObjectReader reader, Type declaredType, Func<IDictionary<K, V>>? factory = null) {
        if (factory != null) return factory.Invoke();
        if (declaredType.IsGenericType) {
            Type genericTypeDefinition = declaredType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(Dictionary<,>)) {
                return new Dictionary<K, V>();
            }
            if (genericTypeDefinition == typeof(ConcurrentDictionary<,>)) {
                return new ConcurrentDictionary<K, V>();
            }
        }
        return reader.Options.weakOrder ? new Dictionary<K, V>() : new LinkedDictionary<K, V>();
    }

    #endregion
}

/// <summary>
/// 字典编解码器
/// </summary>
/// <typeparam name="K"></typeparam>
/// <typeparam name="V"></typeparam>
public class IDictionaryCodec<K, V> : DictionaryCodec, IDsonCodec<IDictionary<K, V>>
{
    private static readonly Func<IDictionary<K, V>>? _factory = () => new Dictionary<K, V>();

    public void WriteObject(IDsonObjectWriter writer, ref IDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
        WriteDictionary(writer, inst, declaredType, style);
    }

    public IDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<IDictionary<K, V>>? factory = null) {
        return ReadDictionary(reader, declaredType, factory ?? _factory);
    }
}
}