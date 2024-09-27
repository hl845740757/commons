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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DsonCodecScanIgnore
public class MapCodec<T extends Map> implements DsonCodec<T> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends T> factory;
    private final TypeInfo keyTypeInfo;
    private final TypeInfo valueTypeInfo;

    @SuppressWarnings("unchecked")
    public MapCodec(TypeInfo typeInfo) {
        this.typeInfo = Objects.requireNonNull(typeInfo);

        Class<T> rawType = (Class<T>) typeInfo.rawType;
        this.factory = DsonConverterUtils.tryNoArgConstructorToSupplier(rawType);
        this.keyTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Map.class, "K");
        this.valueTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Map.class, "V");
    }

    public MapCodec(TypeInfo typeInfo, Supplier<? extends T> factory) {
        this.typeInfo = Objects.requireNonNull(typeInfo);
        this.factory = factory;

        @SuppressWarnings("unchecked") Class<T> rawType = (Class<T>) typeInfo.rawType;
        this.keyTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Map.class, "K");
        this.valueTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Map.class, "V");
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    @SuppressWarnings("unchecked")
    protected Map<Object, Object> newMap(TypeInfo declaredType, Supplier<? extends T> factory) {
        if (factory != null) {
            return (Map<Object, Object>) factory.get();
        }
        if (this.factory != null) {
            return (Map<Object, Object>) this.factory.get();
        }
        return new LinkedHashMap<>();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo declaredType, ObjectStyle style) {
        // 理论上declaredType只影响当前inst是否写入类型，因此应当优先从inst的真实类型中查询K,V的类型，但Java是伪泛型...
        TypeInfo keyTypeInfo = this.keyTypeInfo;
        TypeInfo valueTypeInfo = this.valueTypeInfo;

        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();
        if (writer.options().writeMapAsDocument) {
            writer.writeStartObject(instance, declaredType, style);
            for (Map.Entry<?, ?> entry : entrySet) {
                String keyString = writer.encodeKey(entry.getKey());
                Object value = entry.getValue();
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
            writer.writeStartArray(instance, declaredType, style);
            for (Map.Entry<?, ?> entry : entrySet) {
                writer.writeObject(null, entry.getKey(), keyTypeInfo, null);
                writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
            }
            writer.writeEndArray();
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends T> factory) {
        TypeInfo keyTypeInfo = this.keyTypeInfo;
        TypeInfo valueTypeInfo = this.valueTypeInfo;

        Map<Object, Object> result = newMap(declaredType, factory);
        if (reader.options().writeMapAsDocument) {
            reader.readStartObject(declaredType);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                Object key = reader.decodeKey(keyString, keyTypeInfo);
                Object value = reader.readObject(keyString, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            reader.readStartArray(declaredType);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                Object key = reader.readObject(null, keyTypeInfo);
                Object value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndArray();
        }
        CollectionConverter collectionConverter = reader.options().collectionConverter;
        if (collectionConverter != null) {
            result = collectionConverter.convertMap(declaredType, result);
        }
        @SuppressWarnings("unchecked") Class<T> rawType = (Class<T>) typeInfo.rawType;
        return rawType.cast(result);
    }

}