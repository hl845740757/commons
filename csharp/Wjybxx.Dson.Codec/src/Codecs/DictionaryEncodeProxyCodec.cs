#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using System.Diagnostics;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
public class DictionaryEncodeProxyCodec<V> : IDsonCodec<DictionaryEncodeProxy<V>>
{
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref DictionaryEncodeProxy<V> inst, Type declaredType, ObjectStyle style) {
        IEnumerable<KeyValuePair<string, V>> entries = inst.Entries ?? throw new NullReferenceException("inst.Entries");
        Type[]? genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type valDeclaredType = genericTypeArguments.Length == 1 ? genericTypeArguments[0] : typeof(object);

        switch (inst.Mode) {
            default: {
                writer.WriteStartObject(in inst, declaredType, style); // 字典写为普通文档
                foreach (KeyValuePair<string, V> pair in entries) {
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
                break;
            }
            case DictionaryEncodeProxy.MODE_ARRAY: {
                writer.WriteStartArray(inst, declaredType, style); // 整个字典写为数组
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteString(null, pair.Key);
                    writer.WriteObject(null, pair.Value, valDeclaredType);
                }
                writer.WriteEndArray();
                break;
            }
            case DictionaryEncodeProxy.MODE_PAIR_AS_ARRAY: {
                Type pairTypeInfo = typeof(KeyValuePair<string, V>);
                writer.WriteStartArray(inst, declaredType, style);
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteStartArray(in pair, pairTypeInfo); // pair写为子数组
                    {
                        writer.WriteString(null, pair.Key);
                        writer.WriteObject(null, pair.Value, valDeclaredType);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case DictionaryEncodeProxy.MODE_PAIR_AS_DOCUMENT: {
                Type pairTypeInfo = typeof(KeyValuePair<string, V>);

                writer.WriteStartArray(inst, declaredType, style);
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteStartObject(in pair, pairTypeInfo); // pair写为子文档
                    {
                        writer.WriteObject(pair.Key, pair.Value, valDeclaredType);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    public DictionaryEncodeProxy<V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<DictionaryEncodeProxy<V>>? factory = null) {
        Type[]? genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type valDeclaredType = genericTypeArguments.Length == 1 ? genericTypeArguments[0] : typeof(object);

        List<KeyValuePair<string, V>> entries = new List<KeyValuePair<string, V>>();
        DictionaryEncodeProxy<V> result = new DictionaryEncodeProxy<V>();
        result.Entries = entries;

        DsonType currentDsonType = reader.CurrentDsonType;
        if (currentDsonType == DsonType.Object) {
            result.SetWriteAsDocument();
            reader.ReadStartObject();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string key = reader.ReadName();
                V value = reader.ReadObject<V>(key, valDeclaredType);
                entries.Add(new KeyValuePair<string, V>(key, value));
            }
            reader.ReadEndObject();
        } else {
            Debug.Assert(currentDsonType == DsonType.Array);
            reader.ReadStartArray();
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.String: { // 整个字典写为数组
                    result.SetWriteAsArray();
                    do {
                        string key = reader.ReadString(null);
                        V value = reader.ReadObject<V>(null, valDeclaredType);
                        entries.Add(new KeyValuePair<string, V>(key, value));
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    result.SetWritePairAsArray();
                    do {
                        reader.ReadStartArray();
                        {
                            string key = reader.ReadString(null);
                            V value = reader.ReadObject<V>(null, valDeclaredType);

                            entries.Add(new KeyValuePair<string, V>(key, value));
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Object: {
                    result.SetWritePairAsArray();
                    do {
                        reader.ReadStartObject();
                        {
                            string key = reader.ReadName();
                            V value = reader.ReadObject<V>(null, valDeclaredType);
                            entries.Add(new KeyValuePair<string, V>(key, value));
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }
}
}