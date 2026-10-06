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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 字典通用编解码器
/// </summary>
public class DictionaryCodec<K, V> : IDsonCodec<IDictionary<K, V>> where K : notnull
{
    private readonly Type encoderType; // KV应当和encoderType的泛型参数相同，因为Codec就是根据encoderType的泛型参数构建的
    private readonly Func<IDictionary<K, V>>? factory;
    private readonly FactoryKind factoryKind; // 处理默认情况

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
    }

    private static FactoryKind ComputeFactoryKind(Type typeInfo) {
        if (typeInfo == typeof(LinkedDictionary<K, V>)
            || typeInfo == typeof(IGenericDictionary<K, V>)) {
            return FactoryKind.LinkedDictionary;
        }
        if (typeInfo == typeof(ArrayDictionary<K, V>)) {
            return FactoryKind.ArrayDictionary;
        }
        if (typeInfo == typeof(SortedList<K, V>)) {
            return FactoryKind.SortedList;
        }
        // IDictionary接口类型根据配置决定
        return FactoryKind.Unknown;
    }

    private enum FactoryKind
    {
        Unknown,
        LinkedDictionary,
        ArrayDictionary,
        SortedList
    }

    public Type GetEncoderType() => encoderType;

    private IDictionary<K, V> NewDictionary(int count) {
        if (this.factory != null) return this.factory();
        return factoryKind switch
        {
            FactoryKind.LinkedDictionary => new LinkedDictionary<K, V>(count),
            FactoryKind.ArrayDictionary => new ArrayDictionary<K, V>(count),
            FactoryKind.SortedList => new SortedList<K, V>(count),
            _ => new Dictionary<K, V>(count)
        };
    }

    public void WriteObject(IDsonObjectWriter writer, IDictionary<K, V> inst, Type declaredType, SerializeFeatures features) {
        DsonCodecImpl<K> keyCodec = writer.GetInlinableCodec<K>();
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        if (keyCodec == null || !keyCodec.IsKeyCodec) {
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
            foreach (KeyValuePair<K, V> pair in inst) {
                writer.WriteObject(pair.Key);
                writer.WriteObject(pair.Value, elementFeatures);
            }
            writer.WriteEndArray();
        } else {
            // 接口类型foreach会导致迭代器装箱，但序列化性能不是重点关注项
            // 理论上Value也可以内联加速，但Key+Value组合情况较多，暂不优化
            SerializeFeatures keyFeatures = GetKeyFeatures(features);
            if (IsWriteAsArray(features, writer)) {
                writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    keyCodec.WriteObject(writer, pair.Key, typeof(K), keyFeatures);
                    writer.WriteObject(pair.Value, elementFeatures);
                }
                writer.WriteEndArray();
            } else {
                writer.WriteStartObject(encoderType, declaredType, selfFeatures, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    string keyString = keyCodec.EncodeKey(pair.Key, keyFeatures);
                    writer.WriteName(keyString); // 确保null值写入，也可通过Feature实现
                    writer.WriteObject(keyString, pair.Value, elementFeatures);
                }
                writer.WriteEndObject();
            }
        }
    }

    public IDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        reader.SetEnableNameIntern(false); // 禁用字典的name池化
        if ((features & DeserializeFeatures.SerializeReference) != 0) {
            return ReadDictionaryRef(reader, features);
        }

        DsonCodecImpl<K> keyCodec = reader.GetInlinableCodec<K>();
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        IDictionary<K, V> result;
        if (keyCodec == null || !keyCodec.IsKeyCodec) {
            int count = reader.ReadStartArray(encoderType, selfFeatures).count;
            result = NewDictionary(count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = reader.ReadObject<K>();
                V value = reader.ReadObject<V>(elementFeatures);
                result[key] = value;
            }
            reader.ReadEndArray();
        } else {
            const DeserializeFeatures keyFeatures = 0;
            if (reader.CurrentDsonType == DsonType.Array) {
                // 输入流为Array
                int count = reader.ReadStartArray(encoderType, selfFeatures).count;
                result = NewDictionary(count);
                //
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    K key = keyCodec.ReadObject(reader, typeof(K), keyFeatures);
                    V value = reader.ReadObject<V>(elementFeatures);
                    result[key] = value;
                }
                reader.ReadEndArray();
            } else {
                // 输入流为Document
                int count = reader.ReadStartObject(encoderType, selfFeatures).count;
                result = NewDictionary(count);
                //
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    K key = keyCodec.DecodeKey(reader.ReadName());
                    V value = reader.ReadObject<V>(elementFeatures);
                    result[key] = value;
                }
                reader.ReadEndObject();
            }
        }
        return result;
    }

    private IDictionary<K, V> ReadDictionaryRef(IDsonObjectReader reader, DeserializeFeatures features) {
        DsonCodecImpl<K> keyCodec = reader.GetInlinableCodec<K>();
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        Dictionary<K, V> result;
        List<K> keyArray;
        if (keyCodec == null || !keyCodec.IsKeyCodec) {
            int count = reader.ReadStartArray(encoderType, selfFeatures).count;
            result = new Dictionary<K, V>(count);
            keyArray = new List<K>(count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = reader.ReadObject<K>();
                ReadValueRef(reader, elementFeatures, result, keyArray, key);
            }
            reader.ReadEndArray();
        } else {
            const DeserializeFeatures keyFeatures = 0;
            if (reader.CurrentDsonType == DsonType.Array) {
                // 输入流为Array
                int count = reader.ReadStartArray(encoderType, selfFeatures).count;
                result = new Dictionary<K, V>(count);
                keyArray = new List<K>(count);
                //
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    K key = keyCodec.ReadObject(reader, typeof(K), keyFeatures);
                    ReadValueRef(reader, elementFeatures, result, keyArray, key);
                }
                reader.ReadEndArray();
            } else {
                // 输入流为Document
                int count = reader.ReadStartObject(encoderType, selfFeatures).count;
                result = new Dictionary<K, V>(count);
                keyArray = new List<K>(count);
                //
                while (reader.ReadDsonType() != DsonType.EndOfObject) {
                    K key = keyCodec.DecodeKey(reader.ReadName());
                    ReadValueRef(reader, elementFeatures, result, keyArray, key);
                }
                reader.ReadEndObject();
            }
        }
        return result;
    }

    // 走到该方法时Value通常为引用类型
    private void ReadValueRef(IDsonObjectReader reader, DeserializeFeatures elementFeatures,
                              Dictionary<K, V> result, List<K> keyArray, K key) {
        if (reader.TryReadRefId(out int refId)) {
            result[key] = default; // 预填充 - 由于可能立即执行SetField，所以必须先填充
            keyArray.Add(key);
            reader.DeferReference(refId, this, result, keyArray, keyArray.Count - 1);
        } else {
            result[key] = reader.ReadObject<V>(elementFeatures);
        }
    }

    public bool SetField(object inst, object keyArray, int index, object value) {
        if (inst is Dictionary<K, V> result && keyArray is List<K> keys) {
            K key = keys[index];
            result[key] = (V)value;
            return true;
        }
        return false;
    }

    private static bool IsWriteAsArray(SerializeFeatures features, IDsonObjectWriter writer) {
        if ((features & SerializeFeatures.MapAsArray) != 0) return true;
        if ((features & SerializeFeatures.MapAsDocument) != 0) return false;
        //
        TypeMeta typeMeta = writer.ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.MapAsArray) != 0) return true;
            if ((features & SerializeFeatures.MapAsDocument) != 0) return false;
        }
        features = writer.Options.encodeFeatures;
        if ((features & SerializeFeatures.MapAsArray) != 0) return true;
        if ((features & SerializeFeatures.MapAsDocument) != 0) return false;
        //
        return !writer.IsTextWriter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TypeMeta GetPairTypeMeta(ITypeMetaRegistry typeMetaRegistry) {
        return typeMetaRegistry.OfType(typeof(KeyValuePair<K, V>))!;
    }

    private static SerializeFeatures GetKeyFeatures(SerializeFeatures features) {
        if (typeof(K).IsEnum) {
            if ((features & SerializeFeatures.EnumKeyAsString) != 0) return SerializeFeatures.EnumAsString;
            if ((features & SerializeFeatures.EnumKeyAsNumber) != 0) return SerializeFeatures.EnumAsNumber;
        }
        return default;
    }
}
}