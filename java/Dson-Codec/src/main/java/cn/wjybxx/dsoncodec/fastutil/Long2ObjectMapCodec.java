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

package cn.wjybxx.dsoncodec.fastutil;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;
import it.unimi.dsi.fastutil.longs.Long2ObjectLinkedOpenHashMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectMaps;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class Long2ObjectMapCodec<V> implements DsonCodec<Long2ObjectMap<V>> {

    protected final TypeInfo encoderType;
    protected final Supplier<? extends Long2ObjectMap<V>> factory;

    public Long2ObjectMapCodec(TypeInfo encoderType) {
        this(encoderType, null);
    }

    @SuppressWarnings("unchecked")
    public Long2ObjectMapCodec(TypeInfo encoderType, Supplier<? extends Long2ObjectMap<V>> factory) {
        if (factory == null) {
            Class<? extends Long2ObjectMap<V>> rawType = (Class<? extends Long2ObjectMap<V>>) encoderType.rawType;
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier(rawType);
        }
        this.encoderType = encoderType;
        this.factory = factory;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    private Long2ObjectMap<V> newMap() {
        if (factory != null) return factory.get();
        return new Long2ObjectLinkedOpenHashMap<>();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Long2ObjectMap<V> inst, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(0);

        if (writer.options().writeMapAsDocument) {
            writer.writeStartObject(style, encoderType, declaredType);
            for (var itr = Long2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                Long2ObjectMap.Entry<V> entry = itr.next();
                String keyString = Long.toString(entry.getLongKey());
                V value = entry.getValue();
                if (value == null) {
                    writer.writeName(keyString);
                    writer.writeNull(keyString);
                } else {
                    writer.writeObject(keyString, value, valueTypeInfo, null);
                }
            }
            writer.writeEndObject();
        } else {
            writer.writeStartArray(style, encoderType, declaredType);
            for (var itr = Long2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                Long2ObjectMap.Entry<V> entry = itr.next();
                writer.writeLong(null, entry.getLongKey(), null);
                writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
            }
            writer.writeEndArray();
        }
    }

    @Override
    public Long2ObjectMap<V> readObject(DsonObjectReader reader, Supplier<? extends Long2ObjectMap<V>> factory) {
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(0);

        Long2ObjectMap<V> result = factory != null ? factory.get() : newMap();
        if (reader.options().writeMapAsDocument) {
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                long key = Long.parseLong(keyString);
                V value = reader.readObject(keyString, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            reader.readStartArray();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                int key = reader.readInt(null);
                V value = reader.readObject(null, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndArray();
        }
        return reader.options().readAsImmutable ? Long2ObjectMaps.unmodifiable(result) : result;
    }
}
