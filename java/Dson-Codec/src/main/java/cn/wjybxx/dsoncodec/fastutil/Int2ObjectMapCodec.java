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

import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonConverterUtils;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import it.unimi.dsi.fastutil.ints.Int2ObjectLinkedOpenHashMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectMaps;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class Int2ObjectMapCodec<V> implements DsonCodec<Int2ObjectMap<V>> {

    protected final TypeInfo encoderType;
    protected final Supplier<? extends Int2ObjectMap<V>> factory;

    public Int2ObjectMapCodec(TypeInfo encoderType) {
        this(encoderType, null);
    }

    @SuppressWarnings("unchecked")
    public Int2ObjectMapCodec(TypeInfo encoderType, Supplier<? extends Int2ObjectMap<V>> factory) {
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Int2ObjectMap<V>>) encoderType.rawType);
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

    protected Int2ObjectMap<V> newMap() {
        if (factory != null) return factory.get();
        return new Int2ObjectLinkedOpenHashMap<>();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Int2ObjectMap<V> inst, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(0);

        switch (writer.options().mapEncodePolicy) {
            case DOCUMENT -> {
                writer.writeStartObject(style, encoderType, declaredType);
                for (var itr = Int2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                    Int2ObjectMap.Entry<V> entry = itr.next();
                    String keyString = Integer.toString(entry.getIntKey());
                    writer.writeName(keyString); // 确保Null会被写入
                    writer.writeObject(keyString, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(style, encoderType, declaredType);
                for (var itr = Int2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                    Int2ObjectMap.Entry<V> entry = itr.next();
                    writer.writeStartObject(ObjectStyle.FLOW); // pair写为子文档-没有类型
                    {
                        String keyString = Integer.toString(entry.getIntKey());
                        writer.writeName(keyString); // 确保写入null
                        writer.writeObject(keyString, entry.getValue(), valueTypeInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
            case PAIR_AS_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType);
                for (var itr = Int2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                    Int2ObjectMap.Entry<V> entry = itr.next();
                    writer.writeStartArray(ObjectStyle.FLOW); // pair写为子数组-没有类型
                    {
                        writer.writeInt(null, entry.getIntKey());
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType);
                for (var itr = Int2ObjectMaps.fastIterator(inst); itr.hasNext(); ) {
                    Int2ObjectMap.Entry<V> entry = itr.next();
                    writer.writeInt(null, entry.getIntKey());
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
        }
    }

    @Override
    public Int2ObjectMap<V> readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends Int2ObjectMap<V>> factory) {
        reader.setEnableNameIntern(false); // 禁用字典的name池化
        TypeInfo valueTypeInfo = encoderType.typeArgs.get(0);

        Int2ObjectMap<V> result = factory != null ? factory.get() : newMap();
        if (reader.getCurrentDsonType() == DsonType.OBJECT) {
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String keyString = reader.readName();
                int key = Integer.parseInt(keyString);
                V value = reader.readObject(keyString, valueTypeInfo);
                result.put(key, value);
            }
            reader.readEndObject();
        } else {
            reader.readStartArray();
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case END_OF_OBJECT -> {} // 没有元素
                case OBJECT -> { // Pair为子文档
                    do {
                        reader.readStartObject();
                        {
                            String keyString = reader.readName();
                            int key = Integer.parseInt(keyString);
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
                            int key = reader.readInt(null);
                            V value = reader.readObject(null, valueTypeInfo);
                            result.put(key, value);
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                default -> {
                    // 整个字典写为数组
                    do {
                        int key = reader.readInt(null);
                        V value = reader.readObject(null, valueTypeInfo);
                        result.put(key, value);
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
            }
            reader.readEndArray();
        }
        return reader.options().readAsImmutable ? Int2ObjectMaps.unmodifiable(result) : result;
    }
}
