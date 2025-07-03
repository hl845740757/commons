/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dsoncodec.codecs;

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.Collections;
import java.util.EnumMap;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@DsonCodecScanIgnore
public class MapCodec<K, V> implements DsonCodec<Map<K, V>> {

    protected final TypeInfo encoderType;
    protected final Supplier<? extends Map<K, V>> factory;
    private final FactoryKind factoryKind;
    private final KeyKind keyKind;

    public MapCodec(TypeInfo encoderType) {
        this(encoderType, null);
    }

    @SuppressWarnings("unchecked")
    public MapCodec(TypeInfo encoderType, Supplier<? extends Map<K, V>> factory) {
        if (encoderType.typeArgs.size() != 2) {
            throw new IllegalArgumentException("encoderType.typeArgs.size() != 2");
        }
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Map<K, V>>) encoderType.rawType);
        }
        this.encoderType = encoderType;
        this.factory = factory;
        this.factoryKind = factory == null ? computeFactoryKind(encoderType) : FactoryKind.Unknown;
        this.keyKind = computeKeyKind(encoderType);
    }

    private static KeyKind computeKeyKind(TypeInfo typeInfo) {
        TypeInfo keyType = typeInfo.typeArgs.get(0);
        if (keyType.rawType == Integer.class || keyType.rawType == int.class) return KeyKind.Int32;
        if (keyType.rawType == Long.class || keyType.rawType == long.class) return KeyKind.Int64;
        if (keyType.rawType == String.class) return KeyKind.String;
        if (keyType.isEnum()) return KeyKind.Enum;
        return KeyKind.Generic;
    }

    private static FactoryKind computeFactoryKind(TypeInfo typeInfo) {
        Class<?> clazz = typeInfo.rawType;
        // EnumMap需要考虑泛型擦除问题
        if (clazz == EnumMap.class && typeInfo.typeArgs.get(0).isEnum()) {
            return FactoryKind.EnumMap;
        }
        if (ConcurrentMap.class.isAssignableFrom(clazz)) {
            return FactoryKind.ConcurrentMap;
        }
        return FactoryKind.Unknown;
    }

    private enum FactoryKind {
        Unknown,
        EnumMap,
        ConcurrentMap,
    }

    private enum KeyKind {
        Generic,
        Int32,
        Int64,
        String,
        Enum,
    }

    // 需要动态处理是否写为文档
    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private Map<K, V> newMap(Supplier<? extends Map<K, V>> userFactory, int count) {
        if (userFactory != null) return userFactory.get();
        if (factory != null) return factory.get();
        return switch (factoryKind) {
            case EnumMap -> {
                TypeInfo elementTypeInfo = encoderType.typeArgs.get(0);
                yield new EnumMap((Class) elementTypeInfo.rawType);
            }
            case ConcurrentMap -> new ConcurrentHashMap<>();
            default -> count > 0 ? LinkedHashMap.newLinkedHashMap(count) : new LinkedHashMap<>();
        };
    }

    protected Map<K, V> toImmutable(TypeInfo declaredType, Map<K, V> result) {
        if (!declaredType.rawType.isInterface()) {
            return result;
        }
        if (result instanceof LinkedHashMap<K, V> linkedHashMap) {
            return Collections.unmodifiableMap(linkedHashMap);
        }
        if (result instanceof EnumMap<?, ?>) {
            return Collections.unmodifiableMap(result);
        }
        return CollectionUtils.toImmutableLinkedHashMap(result);
    }

    @SuppressWarnings("unchecked")
    @Override
    public void writeObject(DsonObjectWriter writer, Map<K, V> inst, TypeInfo declaredType, ObjectStyle style) {
        if (keyKind == KeyKind.Int32) {
            writeDictionaryInt(writer, (Map<Integer, V>) inst, declaredType, style);
        } else if (keyKind == KeyKind.Int64) {
            writeDictionaryLong(writer, (Map<Long, V>) inst, declaredType, style);
        } else {
            writeDictionaryObject(writer, inst, declaredType, style);
        }
    }

    @SuppressWarnings("unchecked")
    @Override
    public Map<K, V> readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends Map<K, V>> factory) {
        reader.setEnableNameIntern(false); // 禁用字典的name池化
        Map<K, V> result;
        if (keyKind == KeyKind.Int32) {
            result = (Map<K, V>) readDictionaryInt(reader, factory);
        } else if (keyKind == KeyKind.Int64) {
            result = (Map<K, V>) readDictionaryLong(reader, factory);
        } else {
            result = readDictionaryObject(reader, factory);
        }
        return reader.options().readAsImmutable ? toImmutable(declaredType, result) : result;
    }

    // region int

    private void writeDictionaryInt(DsonObjectWriter writer, Map<Integer, V> inst, TypeInfo declaredType, ObjectStyle style) {
//        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        switch (writer.options().mapEncodePolicy) {
            case DOCUMENT -> {
                writer.writeStartObject(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Integer, V> entry : inst.entrySet()) {
                    String keyString = entry.getKey().toString();
                    writer.writeName(keyString); // 确保Null会被写入
                    writer.writeObject(keyString, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Integer, V> entry : inst.entrySet()) {
                    writer.writeStartObject(ObjectStyle.FLOW); // pair写为子文档-没有类型
                    {
                        String keyString = entry.getKey().toString();
                        writer.writeName(keyString); // 确保写入null
                        writer.writeObject(keyString, entry.getValue(), valueTypeInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
            case PAIR_AS_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Integer, V> entry : inst.entrySet()) {
                    writer.writeStartArray(ObjectStyle.FLOW); // pair写为子数组-没有类型
                    {
                        writer.writeInt(null, entry.getKey()); // key不可以为null
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Integer, V> entry : inst.entrySet()) {
                    writer.writeInt(null, entry.getKey()); // key不可以为null
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
        }
    }

    @SuppressWarnings("unchecked")
    private Map<Integer, V> readDictionaryInt(DsonObjectReader reader, Supplier<? extends Map<K, V>> factory) {
        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        //
        Map<Integer, V> result;
        if (reader.getCurrentDsonType() == DsonType.OBJECT) {
            int count = reader.readStartObject();
            result = (Map<Integer, V>) newMap(factory, count);
            //
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                Integer key = Integer.parseInt(keyString);
                V value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            int count = reader.readStartArray();
            result = (Map<Integer, V>) newMap(factory, count);
            //
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case END_OF_OBJECT -> {} // 没有元素
                case OBJECT -> { // Pair为子文档
                    do {
                        reader.readStartObject();
                        {
                            reader.readDsonType();
                            String keyString = reader.readName();
                            Integer key = Integer.parseInt(keyString);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndObject();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    do {
                        reader.readStartArray();
                        {
                            Integer key = reader.readInt(null);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                default -> {
                    // 整个字典写为数组
                    do {
                        Integer key = reader.readInt(null);
                        V value = reader.readObject(null, valueTypeInfo);
                        result.put(key, value);
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
            }
            reader.readEndArray();
        }
        return result;
    }

    // endregion

    // region int64
    private void writeDictionaryLong(DsonObjectWriter writer, Map<Long, V> inst, TypeInfo declaredType, ObjectStyle style) {
//        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        switch (writer.options().mapEncodePolicy) {
            case DOCUMENT -> {
                writer.writeStartObject(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Long, V> entry : inst.entrySet()) {
                    String keyString = entry.getKey().toString();
                    writer.writeName(keyString); // 确保Null会被写入
                    writer.writeObject(keyString, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Long, V> entry : inst.entrySet()) {
                    writer.writeStartObject(ObjectStyle.FLOW); // pair写为子文档-没有类型
                    {
                        String keyString = entry.getKey().toString();
                        writer.writeName(keyString); // 确保写入null
                        writer.writeObject(keyString, entry.getValue(), valueTypeInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
            case PAIR_AS_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Long, V> entry : inst.entrySet()) {
                    writer.writeStartArray(ObjectStyle.FLOW); // pair写为子数组-没有类型
                    {
                        writer.writeLong(null, entry.getKey()); // key不可以为null
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<Long, V> entry : inst.entrySet()) {
                    writer.writeLong(null, entry.getKey()); // key不可以为null
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
        }
    }

    @SuppressWarnings("unchecked")
    private Map<Long, V> readDictionaryLong(DsonObjectReader reader, Supplier<? extends Map<K, V>> factory) {
        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        //
        Map<Long, V> result;
        if (reader.getCurrentDsonType() == DsonType.OBJECT) {
            int count = reader.readStartObject();
            result = (Map<Long, V>) newMap(factory, count);
            //
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                Long key = Long.parseLong(keyString);
                V value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            int count = reader.readStartArray();
            result = (Map<Long, V>) newMap(factory, count);
            //
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case END_OF_OBJECT -> {} // 没有元素
                case OBJECT -> { // Pair为子文档
                    do {
                        reader.readStartObject();
                        {
                            reader.readDsonType();
                            String keyString = reader.readName();
                            Long key = Long.parseLong(keyString);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndObject();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    do {
                        reader.readStartArray();
                        {
                            Long key = reader.readLong(null);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                default -> {
                    // 整个字典写为数组
                    do {
                        Long key = reader.readLong(null);
                        V value = reader.readObject(null, valueTypeInfo);
                        result.put(key, value);
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
            }
            reader.readEndArray();
        }
        return result;
    }
    // endregion

    // region object
    private void writeDictionaryObject(DsonObjectWriter writer, Map<K, V> inst, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        // policy修正
        MapEncodePolicy policy = writer.options().mapEncodePolicy;
        if (keyKind == KeyKind.Generic) {
            if (policy == MapEncodePolicy.DOCUMENT) {
                policy = MapEncodePolicy.ARRAY;
            } else if (policy == MapEncodePolicy.PAIR_AS_DOCUMENT) {
                policy = MapEncodePolicy.PAIR_AS_ARRAY;
            }
        }
        switch (policy) {
            case DOCUMENT -> {
                writer.writeStartObject(style, encoderType, declaredType, inst.size());
                for (Map.Entry<K, V> entry : inst.entrySet()) {
                    String keyString = writer.encodeKey(entry.getKey(), keyTypeInfo);
                    writer.writeName(keyString); // 确保Null会被写入
                    writer.writeObject(keyString, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<K, V> entry : inst.entrySet()) {
                    writer.writeStartObject(ObjectStyle.FLOW); // pair写为子文档-没有类型
                    {
                        String keyString = writer.encodeKey(entry.getKey(), keyTypeInfo);
                        writer.writeName(keyString); // 确保写入null
                        writer.writeObject(keyString, entry.getValue(), valueTypeInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
            case PAIR_AS_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<K, V> entry : inst.entrySet()) {
                    writer.writeStartArray(ObjectStyle.FLOW); // pair写为子数组-没有类型
                    {
                        writer.writeObject(null, entry.getKey(), keyTypeInfo);
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType, inst.size());
                for (Map.Entry<K, V> entry : inst.entrySet()) {
                    writer.writeObject(null, entry.getKey(), keyTypeInfo, null);
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
        }
    }

    private Map<K, V> readDictionaryObject(DsonObjectReader reader, Supplier<? extends Map<K, V>> factory) {
        TypeInfo keyTypeInfo = encoderType.typeArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(1);
        //
        Map<K, V> result;
        if (reader.getCurrentDsonType() == DsonType.OBJECT) {
            int count = reader.readStartObject();
            result = newMap(factory, count);
            //
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                K key = reader.decodeKey(keyString, keyTypeInfo);
                V value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            int count = reader.readStartArray();
            result = newMap(factory, count);
            //
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case END_OF_OBJECT -> {} // 没有元素
                case OBJECT -> { // Pair为子文档
                    do {
                        reader.readStartObject();
                        {
                            reader.readDsonType();
                            String keyString = reader.readName();
                            K key = reader.decodeKey(keyString, keyTypeInfo);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndObject();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    do {
                        reader.readStartArray();
                        {
                            K key = reader.readObject(null, keyTypeInfo);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                default -> {
                    // 整个字典写为数组
                    do {
                        K key = reader.readObject(null, keyTypeInfo);
                        V value = reader.readObject(null, valueTypeInfo);
                        result.put(key, value);
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
            }
            reader.readEndArray();
        }
        return result;
    }
    // endregion
}