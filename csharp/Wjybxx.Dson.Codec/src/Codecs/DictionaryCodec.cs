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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 字典通用编解码器
/// </summary>
public class DictionaryCodec<K, V> : IDsonCodec<IDictionary<K, V>>
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
        if (typeInfo == typeof(ConcurrentDictionary<K, V>)) {
            return FactoryKind.ConcurrentDictionary;
        }
        if (typeInfo == typeof(ArrayDictionary<K, V>)) {
            return FactoryKind.ArrayDictionary;
        }
        // IDictionary接口类型根据配置决定
        return FactoryKind.Unknown;
    }

    private enum FactoryKind
    {
        Unknown,
        LinkedDictionary,
        ConcurrentDictionary,
        ArrayDictionary
    }

    public Type GetEncoderType() => encoderType;

    private IDictionary<K, V> NewDictionary(Func<object>? userFactory, int count) {
        if (userFactory != null) return (IDictionary<K, V>)userFactory();
        if (this.factory != null) return this.factory();
        return factoryKind switch
        {
            FactoryKind.LinkedDictionary => new LinkedDictionary<K, V>(count),
            FactoryKind.ConcurrentDictionary => new ConcurrentDictionary<K, V>(),
            FactoryKind.ArrayDictionary => new ArrayDictionary<K, V>(count),
            _ => new Dictionary<K, V>(count)
        };
    }

    private IDictionary<K, V> ToImmutable(Type declaredType, IDictionary<K, V> dictionary) {
        if (declaredType.IsInterface) {
            return ImmutableDictionary<K, V>.CreateRange(dictionary);
        }
        if (declaredType.IsGenericType
            && declaredType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>)) {
            return ImmutableDictionary<K, V>.CreateRange(dictionary);
        }
        return dictionary;
    }

    public void WriteObject(IDsonObjectWriter writer, IDictionary<K, V> inst, Type declaredType, SerializeFeatures features) {
        DsonCodecImpl<K> keyEncoder = writer.CodecRegistry.GetEncoder(typeof(K)) as DsonCodecImpl<K>;
        if (keyEncoder == null || !keyEncoder.IsKeyCodec) {
            SerializeFeatures selfFeatures = features.ErasureElementFeatures();
            SerializeFeatures elementFeatures = features.GetElementFeatures();
            //
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
            foreach (KeyValuePair<K, V> pair in inst) {
                writer.WriteObject(pair.Key);
                writer.WriteObject(pair.Value, elementFeatures);
            }
            writer.WriteEndArray();
        } else {
            WriteDictionary(writer, inst, declaredType, features, keyEncoder);
        }
    }

    public IDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        reader.SetEnableNameIntern(false); // 禁用字典的name池化
        DsonCodecImpl<K> keyEncoder = reader.CodecRegistry.GetDecoder(typeof(K)) as DsonCodecImpl<K>;
        IDictionary<K, V> result;
        if (keyEncoder == null || !keyEncoder.IsKeyCodec) {
            DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
            DeserializeFeatures elementFeatures = features.GetElementFeatures();
            //
            int count = reader.ReadStartArray(encoderType).count;
            result = NewDictionary(factory, count);
            reader.PublishReference(result);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = reader.ReadObject<K>(0);
                V value = reader.ReadObject<V>(elementFeatures);
                result[key] = value;
            }
            reader.ReadEndArray();
        } else {
            result = ReadDictionary(reader, features, factory, keyEncoder);
        }
        // 处理默认的不可变集合
        if (declaredType.IsGenericType) {
            if (declaredType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>)) {
                return result.ToImmutableDictionary2();
            }
        }
        return DsonCodecHelper.IsReadAsImmutable(features, reader)
            ? ToImmutable(declaredType, result)
            : result;
    }

    private void WriteDictionary(IDsonObjectWriter writer, IDictionary<K, V> inst,
                                 Type declaredType, SerializeFeatures features,
                                 DsonCodecImpl<K> keyEncoder) {
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        SerializeFeatures keyFeatures = GetKeyFeatures(features, writer);
        MapStyle style = GetMapStyle(features, writer, MapStyle.Document);
        switch (style) {
            case MapStyle.Document: {
                writer.WriteStartObject(encoderType, declaredType, selfFeatures, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<K, V> pair in inst) {
                    string keyString = keyEncoder.EncodeKey(pair.Key, keyFeatures);
                    writer.WriteName(keyString); // 确保null值写入
                    writer.WriteObject(keyString, pair.Value, elementFeatures);
                }
                writer.WriteEndObject();
                break;
            }
            case MapStyle.PairAsDocument: {
                TypeMeta pairTypeMeta = GetPairTypeMeta(writer.TypeMetaRegistry);
                writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteStartObject(pairTypeMeta, SerializeFeatures.ObjectFlow); // pair写为子文档-没有类型
                    {
                        string keyString = keyEncoder.EncodeKey(pair.Key, keyFeatures);
                        writer.WriteName(keyString); // 确保null值写入
                        writer.WriteObject(keyString, pair.Value, elementFeatures);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }
            case MapStyle.PairAsArray: {
                TypeMeta pairTypeMeta = GetPairTypeMeta(writer.TypeMetaRegistry);
                writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteStartArray(pairTypeMeta, SerializeFeatures.ObjectFlow); // pair写为子数组-没有类型
                    {
                        keyEncoder.WriteObject(writer, pair.Key, typeof(K), default);
                        writer.WriteObject(pair.Value, elementFeatures);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapStyle.Array:
            default: {
                writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<K, V> pair in inst) {
                    keyEncoder.WriteObject(writer, pair.Key, typeof(K), default);
                    writer.WriteObject(pair.Value, elementFeatures);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<K, V> ReadDictionary(IDsonObjectReader reader, DeserializeFeatures features,
                                             Func<object>? factory,
                                             DsonCodecImpl<K> keyDecoder) {
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        IDictionary<K, V> result;
        if (reader.CurrentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject(encoderType).count;
            result = NewDictionary(factory, count);
            reader.PublishReference(result);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = keyDecoder.DecodeKey(reader.ReadName());
                V value = reader.ReadObject<V>(elementFeatures);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray(encoderType).count;
            result = NewDictionary(factory, count);
            reader.PublishReference(result);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    TypeMeta pairTypeMeta = GetPairTypeMeta(reader.TypeMetaRegistry);
                    do {
                        reader.ReadStartObject(pairTypeMeta);
                        {
                            reader.ReadDsonType();
                            K key = keyDecoder.DecodeKey(reader.ReadName());
                            V value = reader.ReadObject<V>(elementFeatures);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    TypeMeta pairTypeMeta = GetPairTypeMeta(reader.TypeMetaRegistry);
                    do {
                        reader.ReadStartArray(pairTypeMeta);
                        {
                            K key = reader.ReadObject<K>(0);
                            V value = reader.ReadObject<V>(elementFeatures);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        K key = reader.ReadObject<K>(0);
                        V value = reader.ReadObject<V>(elementFeatures);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TypeMeta GetPairTypeMeta(ITypeMetaRegistry typeMetaRegistry) {
        return typeMetaRegistry.OfType(typeof(KeyValuePair<K, V>))!;
    }

    private MapStyle GetMapStyle(SerializeFeatures features, IDsonObjectWriter writer, MapStyle def) {
        if (features.ToMapStyle(out MapStyle style)) {
            return style;
        }
        TypeMeta typeMeta = writer.ContainerTypeMeta;
        if (typeMeta != null && typeMeta.encodeFeatures.ToMapStyle(out style)) {
            return style;
        }
        if (writer.Options.encodeFeatures.ToMapStyle(out style)) {
            return style;
        }
        return def;
    }

    private SerializeFeatures GetKeyFeatures(SerializeFeatures features, IDsonObjectWriter writer) {
        if (typeof(K).IsEnum) {
            return IsWriteEnumKeyAsString(features, writer)
                ? SerializeFeatures.EnumKeyAsString
                : SerializeFeatures.EnumKeyAsNumber;
        }
        return default; // 需要为数字支持额外格式吗？
    }

    private bool IsWriteEnumKeyAsString(SerializeFeatures features, IDsonObjectWriter writer) {
        if ((features & SerializeFeatures.EnumKeyAsString) != 0) return true;
        if ((features & SerializeFeatures.EnumKeyAsNumber) != 0) return false;
        TypeMeta typeMeta = writer.ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.EnumKeyAsString) != 0) return true;
            if ((features & SerializeFeatures.EnumKeyAsNumber) != 0) return false;
        }
        features = writer.Options.encodeFeatures;
        return (features & SerializeFeatures.EnumKeyAsString) != 0;
    }
}
}