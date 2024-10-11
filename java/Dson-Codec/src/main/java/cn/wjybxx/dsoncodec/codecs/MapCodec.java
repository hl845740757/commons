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

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends Map<K, V>> factory;
    private final FactoryKind factoryKind;

    public MapCodec(TypeInfo typeInfo) {
        this(typeInfo, null);
    }

    @SuppressWarnings("unchecked")
    public MapCodec(TypeInfo typeInfo, Supplier<? extends Map<K, V>> factory) {
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Map<K, V>>) typeInfo.rawType);
        }
        this.typeInfo = typeInfo;
        this.factory = factory;
        this.factoryKind = factory == null ? computeFactoryKind(typeInfo) : FactoryKind.Unknown;
    }

    private static FactoryKind computeFactoryKind(TypeInfo typeInfo) {
        Class<?> clazz = typeInfo.rawType;
        if (ConcurrentMap.class.isAssignableFrom(clazz)) {
            return FactoryKind.ConcurrentMap;
        }
        return FactoryKind.Unknown;
    }

    private enum FactoryKind {
        Unknown,
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
        return typeInfo;
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private Map<K, V> newMap(TypeInfo declaredType) {
        if (factory != null) return factory.get();
        // EnumMap只有通过DeclaredType构建才稳妥 -- 因为这里的Codec可能是泛型原型
        if (declaredType.rawType == EnumMap.class) {
            TypeInfo elementTypeInfo = declaredType.genericArgs.get(0);
            return new EnumMap((Class) elementTypeInfo.rawType);
        }
        return switch (factoryKind) {
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
        // 理论上declaredType只影响当前inst是否写入类型，而不影响KV是否写入类型，因此应当优先从inst的真实类型中查询K,V的类型...
        // 另外，typeInfo就是根据【运行时类型】和【declaredType】生成的
        TypeInfo keyTypeInfo = typeInfo.genericArgs.get(0);
        TypeInfo valueTypeInfo = typeInfo.genericArgs.get(1);

        var entrySet = inst.entrySet();
        if (writer.options().writeMapAsDocument) {
            writer.writeStartObject(inst, declaredType, style);
            for (Map.Entry<K, V> entry : entrySet) {
                String keyString = writer.encodeKey(entry.getKey());
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
            writer.writeStartArray(inst, declaredType, style);
            for (Map.Entry<K, V> entry : entrySet) {
                writer.writeObject(null, entry.getKey(), keyTypeInfo, null);
                writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
            }
            writer.writeEndArray();
        }
    }

    @Override
    public Map<K, V> readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends Map<K, V>> factory) {
        TypeInfo keyTypeInfo = typeInfo.genericArgs.get(0);
        TypeInfo valueTypeInfo = typeInfo.genericArgs.get(1);
        //
        Map<K, V> result = factory != null ? factory.get() : newMap(declaredType);
        if (reader.options().writeMapAsDocument) {
            reader.readStartObject(declaredType);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                K key = reader.decodeKey(keyString, keyTypeInfo);
                V value = reader.readObject(keyString, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            reader.readStartArray(declaredType);
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