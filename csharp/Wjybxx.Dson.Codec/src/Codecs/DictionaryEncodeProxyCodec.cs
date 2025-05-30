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

    public void WriteObject(IDsonObjectWriter writer, in DictionaryEncodeProxy<V> inst, Type declaredType, ObjectStyle style) {
        IEnumerable<KeyValuePair<string, V>> entries = inst.Entries ?? throw new NullReferenceException("inst.Entries");
        Type encoderType = typeof(DictionaryEncodeProxy<V>);

        switch (inst.Policy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType); // 字典写为普通文档
                foreach (KeyValuePair<string, V> pair in entries) {
                    string keyString = pair.Key;
                    V value = pair.Value;
                    if (value == null) {
                        // 字典写为普通对象时，必须写入null，否则containsKey会异常；要强制写入null，必须先写入name
                        writer.WriteName(keyString);
                        writer.WriteNull(keyString);
                    } else {
                        writer.WriteObject(keyString, value);
                    }
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType);
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        writer.WriteName(pair.Key); // 确保写入null
                        writer.WriteObject(pair.Key, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }

            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType);
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteString(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType); // 整个字典写为数组
                foreach (KeyValuePair<string, V> pair in entries) {
                    writer.WriteString(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    public DictionaryEncodeProxy<V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        reader.SetEnableNameIntern(false); // 禁用字典的name池化

        List<KeyValuePair<string, V>> entries = new List<KeyValuePair<string, V>>();
        DictionaryEncodeProxy<V> result = new DictionaryEncodeProxy<V>();
        result.Entries = entries;

        DsonType currentDsonType = reader.CurrentDsonType;
        if (currentDsonType == DsonType.Object) {
            result.Policy = MapEncodePolicy.Document;
            reader.ReadStartObject();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string key = reader.ReadName();
                V value = reader.ReadObject<V>(null);
                entries.Add(new KeyValuePair<string, V>(key, value));
            }
            reader.ReadEndObject();
        } else {
            Debug.Assert(currentDsonType == DsonType.Array);
            reader.ReadStartArray();
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    result.Policy = MapEncodePolicy.PairAsDocument;
                    do {
                        reader.ReadStartObject();
                        {
                            string key = reader.ReadName();
                            V value = reader.ReadObject<V>(null);
                            entries.Add(new KeyValuePair<string, V>(key, value));
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    result.Policy = MapEncodePolicy.PairAsArray;
                    do {
                        reader.ReadStartArray();
                        {
                            string key = reader.ReadString(null);
                            V value = reader.ReadObject<V>(null);

                            entries.Add(new KeyValuePair<string, V>(key, value));
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    result.Policy = MapEncodePolicy.Array;
                    do {
                        string key = reader.ReadString(null);
                        V value = reader.ReadObject<V>(null);
                        entries.Add(new KeyValuePair<string, V>(key, value));
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