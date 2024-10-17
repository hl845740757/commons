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

    public MapCodec(TypeInfo encoderType) {
        this(encoderType, null);
    }

    @SuppressWarnings("unchecked")
    public MapCodec(TypeInfo encoderType, Supplier<? extends Map<K, V>> factory) {
        if (encoderType.genericArgs.size() != 2) {
            throw new IllegalArgumentException("encoderType.genericArgs.size() != 2");
        }
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Map<K, V>>) encoderType.rawType);
        }
        this.encoderType = encoderType;
        this.factory = factory;
        this.factoryKind = factory == null ? computeFactoryKind(encoderType) : FactoryKind.Unknown;
    }

    private static FactoryKind computeFactoryKind(TypeInfo typeInfo) {
        Class<?> clazz = typeInfo.rawType;
        // EnumMap需要考虑泛型擦除问题
        if (clazz == EnumMap.class && typeInfo.genericArgs.get(0).isEnum()) {
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

    /** {@link #encoderType}一定是用户declaredType的子类型，因此创建实例时不依赖declaredType */
    @SuppressWarnings({"unchecked", "rawtypes"})
    private Map<K, V> newMap() {
        if (factory != null) return factory.get();
        return switch (factoryKind) {
            case EnumMap -> {
                TypeInfo elementTypeInfo = encoderType.genericArgs.get(0);
                yield new EnumMap((Class) elementTypeInfo.rawType);
            }
            case ConcurrentMap -> new ConcurrentHashMap<>();
            default -> new LinkedHashMap<>();
        };
    }

    protected Map<K, V> toImmutable(Map<K, V> result) {
        if (result instanceof LinkedHashMap<K, V> linkedHashMap) {
            return Collections.unmodifiableMap(linkedHashMap);
        }
        if (result instanceof EnumMap<?, ?>) {
            return Collections.unmodifiableMap(result);
        }
        return CollectionUtils.toImmutableLinkedHashMap(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Map<K, V> inst, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo keyTypeInfo = encoderType.genericArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(1);

        var entrySet = inst.entrySet();
        if (writer.options().writeMapAsDocument) {
            writer.writeStartObject(style, encoderType, declaredType);
            for (Map.Entry<K, V> entry : entrySet) {
                String keyString = writer.encodeKey(entry.getKey(), keyTypeInfo);
                V value = entry.getValue();
                if (value == null) {
                    // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                    writer.writeName(keyString);
                    writer.writeNull(keyString);
                } else {
                    writer.writeObject(keyString, value, valueTypeInfo, null);
                }
            }
            writer.writeEndObject();
        } else {
            writer.writeStartArray(style, encoderType, declaredType);
            for (Map.Entry<K, V> entry : entrySet) {
                writer.writeObject(null, entry.getKey(), keyTypeInfo, null);
                writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
            }
            writer.writeEndArray();
        }
    }

    @Override
    public Map<K, V> readObject(DsonObjectReader reader, Supplier<? extends Map<K, V>> factory) {
        TypeInfo keyTypeInfo = encoderType.genericArgs.get(0);
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(1);
        //
        Map<K, V> result = factory != null ? factory.get() : newMap();
        if (reader.options().writeMapAsDocument) {
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                K key = reader.decodeKey(keyString, keyTypeInfo);
                V value = reader.readObject(keyString, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            reader.readStartArray();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                K key = reader.readObject(null, keyTypeInfo);
                V value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndArray();
        }
        return reader.options().readAsImmutable ? toImmutable(result) : result;
    }

}