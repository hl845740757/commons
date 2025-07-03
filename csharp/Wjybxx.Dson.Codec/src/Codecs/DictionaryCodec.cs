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
using System.Diagnostics;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

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
        this.keyKind = ComputeKeyKind(encoderType);
    }

    private static KeyKind ComputeKeyKind(Type encoderType) {
        Type typeOfKey = typeof(K);
        if (typeOfKey == typeof(int)) return KeyKind.Int32;
        if (typeOfKey == typeof(long)) return KeyKind.Int64;
        if (typeOfKey == typeof(uint)) return KeyKind.Uint32;
        if (typeOfKey == typeof(ulong)) return KeyKind.Uint64;
        if (typeOfKey == typeof(string)) return KeyKind.String;
        if (typeOfKey.IsEnum) return KeyKind.Enum;
        return KeyKind.Generic;
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
        Uint64,
        String,
        Enum
    }

    public Type GetEncoderType() => encoderType;

    public bool AutoStartEnd => false;

    private IDictionary<K, V> NewDictionary(Func<object>? userFactory, int count) {
        if (userFactory != null) return (IDictionary<K, V>)userFactory();
        if (this.factory != null) return this.factory();
        return factoryKind switch
        {
            FactoryKind.LinkedDictionary => new LinkedDictionary<K, V>(count),
            FactoryKind.ConcurrentDictionary => new ConcurrentDictionary<K, V>(),
            _ => new Dictionary<K, V>(count)
        };
    }

    protected virtual IDictionary<K, V> ToImmutable(Type declaredType, IDictionary<K, V> dictionary) {
        if (declaredType.IsInterface) {
            return ImmutableDictionary<K, V>.CreateRange(dictionary);
        }
        if (declaredType.IsGenericType
            && declaredType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>)) {
            return ImmutableDictionary<K, V>.CreateRange(dictionary);
        }
        return dictionary;
    }

    public void WriteObject(IDsonObjectWriter writer, in IDictionary<K, V> inst, Type declaredType, ObjectStyle style) {
        switch (keyKind) {
            case KeyKind.Int32: {
                WriteDictionaryInt(writer, (IDictionary<int, V>)inst, declaredType, style);
                break;
            }
            case KeyKind.Int64: {
                WriteDictionaryLong(writer, (IDictionary<long, V>)inst, declaredType, style);
                break;
            }
            case KeyKind.Uint32: {
                WriteDictionaryUInt(writer, (IDictionary<uint, V>)inst, declaredType, style);
                break;
            }
            case KeyKind.Uint64: {
                WriteDictionaryULong(writer, (IDictionary<ulong, V>)inst, declaredType, style);
                break;
            }
            default: {
                WriteDictionaryObject(writer, inst, declaredType, style);
                break;
            }
        }
    }

    public IDictionary<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        reader.SetEnableNameIntern(false); // 禁用字典的name池化
        IDictionary<K, V> result;
        switch (keyKind) {
            case KeyKind.Int32: {
                result = (IDictionary<K, V>)ReadDictionaryInt(reader, factory);
                break;
            }
            case KeyKind.Int64: {
                result = (IDictionary<K, V>)ReadDictionaryLong(reader, factory);
                break;
            }
            case KeyKind.Uint32: {
                result = (IDictionary<K, V>)ReadDictionaryUInt(reader, factory);
                break;
            }
            case KeyKind.Uint64: {
                result = (IDictionary<K, V>)ReadDictionaryULong(reader, factory);
                break;
            }
            default: {
                result = ReadDictionaryObject(reader, factory);
                break;
            }
        }
        // 处理默认的不可变集合
        if (declaredType.IsGenericType) {
            if (declaredType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>)) {
                return result.ToImmutableDictionary2();
            }
        }
        return reader.Options.readAsImmutable ? ToImmutable(declaredType, result) : result;
    }

    #region int32

    private void WriteDictionaryInt(IDsonObjectWriter writer, IDictionary<int, V> inst,
                                    Type declaredType, ObjectStyle style) {
        switch (writer.Options.mapEncodePolicy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<int, V> pair in inst) {
                    string keyString = pair.Key.ToString();
                    writer.WriteName(keyString); // 确保null会被写入
                    writer.WriteObject(keyString, pair.Value);
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<int, V> pair in inst) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        string keyString = pair.Key.ToString();
                        writer.WriteName(keyString); // 确保null会被写入
                        writer.WriteObject(keyString, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }

            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<int, V> pair in inst) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteInt(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<int, V> pair in inst) {
                    writer.WriteInt(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<int, V> ReadDictionaryInt(IDsonObjectReader reader, Func<object>? factory) {
        DsonType currentDsonType = reader.CurrentDsonType;
        IDictionary<int, V> result;
        if (currentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject();
            result = (IDictionary<int, V>)NewDictionary(factory, count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                int key = int.Parse(reader.ReadName());
                V value = reader.ReadObject<V>(null);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray();
            result = (IDictionary<int, V>)NewDictionary(factory, count);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    do {
                        reader.ReadStartObject();
                        {
                            reader.ReadDsonType();
                            int key = int.Parse(reader.ReadName());
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    do {
                        reader.ReadStartArray();
                        {
                            int key = reader.ReadInt(null);
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        int key = reader.ReadInt(null);
                        V value = reader.ReadObject<V>(null);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    #endregion

    #region int64

    private void WriteDictionaryLong(IDsonObjectWriter writer, IDictionary<long, V> inst,
                                     Type declaredType, ObjectStyle style) {
        switch (writer.Options.mapEncodePolicy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<long, V> pair in inst) {
                    string keyString = pair.Key.ToString();
                    writer.WriteName(keyString); // 确保null会被写入
                    writer.WriteObject(keyString, pair.Value);
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<long, V> pair in inst) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        string keyString = pair.Key.ToString();
                        writer.WriteName(keyString); // 确保null会被写入
                        writer.WriteObject(keyString, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }

            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<long, V> pair in inst) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteLong(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<long, V> pair in inst) {
                    writer.WriteLong(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<long, V> ReadDictionaryLong(IDsonObjectReader reader, Func<object>? factory) {
        DsonType currentDsonType = reader.CurrentDsonType;
        IDictionary<long, V> result;
        if (currentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject();
            result = (IDictionary<long, V>)NewDictionary(factory, count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                long key = long.Parse(reader.ReadName());
                V value = reader.ReadObject<V>(null);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray();
            result = (IDictionary<long, V>)NewDictionary(factory, count);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    do {
                        reader.ReadStartObject();
                        {
                            reader.ReadDsonType();
                            long key = long.Parse(reader.ReadName());
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    do {
                        reader.ReadStartArray();
                        {
                            long key = reader.ReadLong(null);
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        long key = reader.ReadLong(null);
                        V value = reader.ReadObject<V>(null);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    #endregion

    #region uint32

    private void WriteDictionaryUInt(IDsonObjectWriter writer, IDictionary<uint, V> inst,
                                     Type declaredType, ObjectStyle style) {
        switch (writer.Options.mapEncodePolicy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<uint, V> pair in inst) {
                    string keyString = pair.Key.ToString();
                    writer.WriteName(keyString); // 确保null会被写入
                    writer.WriteObject(keyString, pair.Value);
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<uint, V> pair in inst) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        string keyString = pair.Key.ToString();
                        writer.WriteName(keyString); // 确保null会被写入
                        writer.WriteObject(keyString, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }

            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<uint, V> pair in inst) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteUInt(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<uint, V> pair in inst) {
                    writer.WriteUInt(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<uint, V> ReadDictionaryUInt(IDsonObjectReader reader, Func<object>? factory) {
        DsonType currentDsonType = reader.CurrentDsonType;
        IDictionary<uint, V> result;
        if (currentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject();
            result = (IDictionary<uint, V>)NewDictionary(factory, count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                uint key = uint.Parse(reader.ReadName());
                V value = reader.ReadObject<V>(null);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray();
            result = (IDictionary<uint, V>)NewDictionary(factory, count);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    do {
                        reader.ReadStartObject();
                        {
                            reader.ReadDsonType();
                            uint key = uint.Parse(reader.ReadName());
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    do {
                        reader.ReadStartArray();
                        {
                            uint key = reader.ReadUInt(null);
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        uint key = reader.ReadUInt(null);
                        V value = reader.ReadObject<V>(null);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    #endregion

    #region uint64

    private void WriteDictionaryULong(IDsonObjectWriter writer, IDictionary<ulong, V> inst,
                                      Type declaredType, ObjectStyle style) {
        switch (writer.Options.mapEncodePolicy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<ulong, V> pair in inst) {
                    string keyString = pair.Key.ToString();
                    writer.WriteName(keyString); // 确保null会被写入
                    writer.WriteObject(keyString, pair.Value);
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<ulong, V> pair in inst) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        string keyString = pair.Key.ToString();
                        writer.WriteName(keyString); // 确保null会被写入
                        writer.WriteObject(keyString, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }

            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<ulong, V> pair in inst) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteULong(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<ulong, V> pair in inst) {
                    writer.WriteULong(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<ulong, V> ReadDictionaryULong(IDsonObjectReader reader, Func<object>? factory) {
        DsonType currentDsonType = reader.CurrentDsonType;
        IDictionary<ulong, V> result;
        if (currentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject();
            result = (IDictionary<ulong, V>)NewDictionary(factory, count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                ulong key = ulong.Parse(reader.ReadName());
                V value = reader.ReadObject<V>(null);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray();
            result = (IDictionary<ulong, V>)NewDictionary(factory, count);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Array: { // Pair为子数组
                    do {
                        reader.ReadStartArray();
                        {
                            ulong key = reader.ReadULong(null);
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Object: { // Pair为子文档
                    do {
                        reader.ReadStartObject();
                        {
                            reader.ReadDsonType();
                            ulong key = ulong.Parse(reader.ReadName());
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        ulong key = reader.ReadULong(null);
                        V value = reader.ReadObject<V>(null);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    #endregion

    #region generic

    /// <summary>
    /// 为减少代码量，String，Enum和Object合并
    /// </summary>
    private void WriteDictionaryObject(IDsonObjectWriter writer, IDictionary<K, V> inst,
                                       Type declaredType, ObjectStyle style) {
        // Policy修正
        MapEncodePolicy policy = writer.Options.mapEncodePolicy;
        if (keyKind == KeyKind.Generic) {
            if (policy == MapEncodePolicy.Document) {
                policy = MapEncodePolicy.Array;
            } else if (policy == MapEncodePolicy.PairAsDocument) {
                policy = MapEncodePolicy.PairAsArray;
            }
        }
        switch (policy) {
            case MapEncodePolicy.Document: {
                writer.WriteStartObject(style, encoderType, declaredType, inst.Count); // 字典写为普通文档
                foreach (KeyValuePair<K, V> pair in inst) {
                    string keyString = writer.EncodeKey(pair.Key);
                    writer.WriteName(keyString); // 确保null会被写入
                    writer.WriteObject(keyString, pair.Value);
                }
                writer.WriteEndObject();
                break;
            }
            case MapEncodePolicy.PairAsDocument: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteStartObject(ObjectStyle.Flow); // pair写为子文档-没有类型
                    {
                        string keyString = writer.EncodeKey(pair.Key);
                        writer.WriteName(keyString); // 确保null会被写入
                        writer.WriteObject(keyString, pair.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.PairAsArray: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count);
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteStartArray(ObjectStyle.Flow); // pair写为子数组-没有类型
                    {
                        writer.WriteObject(null, pair.Key);
                        writer.WriteObject(null, pair.Value);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            }
            case MapEncodePolicy.Array:
            default: {
                writer.WriteStartArray(style, encoderType, declaredType, inst.Count); // 整个字典写为数组
                foreach (KeyValuePair<K, V> pair in inst) {
                    writer.WriteObject(null, pair.Key);
                    writer.WriteObject(null, pair.Value);
                }
                writer.WriteEndArray();
                break;
            }
        }
    }

    private IDictionary<K, V> ReadDictionaryObject(IDsonObjectReader reader, Func<object>? factory) {
        DsonType currentDsonType = reader.CurrentDsonType;
        IDictionary<K, V> result;
        if (currentDsonType == DsonType.Object) {
            int count = reader.ReadStartObject();
            result = NewDictionary(factory, count);
            //
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                K key = reader.DecodeKey<K>(reader.ReadName());
                V value = reader.ReadObject<V>(null);
                result[key] = value;
            }
            reader.ReadEndObject();
        } else {
            int count = reader.ReadStartArray();
            result = NewDictionary(factory, count);
            //
            DsonType firstDsonType = reader.ReadDsonType();
            switch (firstDsonType) {
                case DsonType.EndOfObject: break; // 没有元素
                case DsonType.Object: { // Pair为子文档
                    do {
                        reader.ReadStartObject();
                        {
                            reader.ReadDsonType();
                            K key = reader.DecodeKey<K>(reader.ReadName());
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndObject();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                case DsonType.Array: { // Pair为子数组
                    do {
                        reader.ReadStartArray();
                        {
                            K key = reader.ReadObject<K>(null);
                            V value = reader.ReadObject<V>(null);
                            result[key] = value;
                        }
                        reader.ReadEndArray();
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
                default: { // 整个字典写为数组
                    do {
                        K key = reader.ReadObject<K>(null);
                        V value = reader.ReadObject<V>(null);
                        result[key] = value;
                    } while (reader.ReadDsonType() != DsonType.EndOfObject);
                    break;
                }
            }
            reader.ReadEndArray();
        }
        return result;
    }

    #endregion
}
}