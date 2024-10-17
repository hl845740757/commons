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

import cn.wjybxx.base.tuple.Tuple2;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Supplier;

/**
 * 通常使用该对象表示用于
 *
 * @author wjybxx
 * date - 2024/5/19
 */
public class MapEncodeProxyCodec<V> implements DsonCodec<MapEncodeProxy<V>> {

    private final TypeInfo encoderType;

    public MapEncodeProxyCodec() {
        encoderType = TypeInfo.of(MapEncodeProxy.class, TypeInfo.OBJECT);
    }

    public MapEncodeProxyCodec(TypeInfo encoderType) {
        this.encoderType = encoderType;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, MapEncodeProxy<V> inst, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(0);
        Collection<Map.Entry<String, V>> entries = Objects.requireNonNull(inst.getEntries());
        switch (inst.getMode()) {
            default -> {
                writer.writeStartObject(style, encoderType, declaredType); // 字典写为普通文档
                for (Map.Entry<String, V> entry : entries) {
                    // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                    writer.writeName(entry.getKey());
                    writer.writeObject(entry.getKey(), entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case MapEncodeProxy.MODE_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType); // 整个字典写为数组
                for (Map.Entry<String, V> entry : entries) {
                    writer.writeString(null, entry.getKey());
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_ARRAY -> {
                writer.writeStartArray(style, encoderType, declaredType);
                for (Map.Entry<String, V> entry : entries) {
                    writer.writeStartArray(ObjectStyle.FLOW); // pair写为子数组-没有类型
                    {
                        writer.writeString(null, entry.getKey());
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(style, encoderType, declaredType);
                for (Map.Entry<String, V> entry : entries) {
                    writer.writeStartObject(ObjectStyle.FLOW); // pair写为子文档-没有类型
                    {
                        writer.writeName(entry.getKey()); // 确保写入null
                        writer.writeObject(entry.getKey(), entry.getValue(), valueTypeInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
        }
    }

    @Override
    public MapEncodeProxy<V> readObject(DsonObjectReader reader, Supplier<? extends MapEncodeProxy<V>> factory) {
        TypeInfo valueTypeInfo = encoderType.genericArgs.get(0);

        List<Map.Entry<String, V>> entries = new ArrayList<>();
        MapEncodeProxy<V> result = new MapEncodeProxy<>();
        result.setEntries(entries);

        DsonType currentDsonType = reader.getCurrentDsonType();
        if (currentDsonType == DsonType.OBJECT) {
            result.setWriteAsDocument(); // 对方写为普通对象
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String key = reader.readName();
                V value = reader.readObject(key, valueTypeInfo);
                entries.add(Tuple2.of(key, value)); // Map.entry不支持value为null
            }
            reader.readEndObject();
        } else {
            assert currentDsonType == DsonType.ARRAY;
            reader.readStartArray();
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case STRING -> { // 整个字典写为数组
                    result.setWriteAsArray();
                    do {
                        String key = reader.readString(null);
                        V value = reader.readObject(null, valueTypeInfo);
                        entries.add(Tuple2.of(key, value));
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    result.setWritePairAsArray();
                    do {
                        reader.readStartArray();
                        {
                            String key = reader.readString(null);
                            V value = reader.readObject(null, valueTypeInfo);
                            entries.add(Tuple2.of(key, value));
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case OBJECT -> { // Pair为子文档
                    result.setWritePairAsDocument();
                    do {
                        reader.readStartObject();
                        {
                            String key = reader.readName();
                            V value = reader.readObject(key, valueTypeInfo);
                            entries.add(Tuple2.of(key, value));
                        }
                        reader.readEndObject();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case END_OF_OBJECT -> {
                    // 没有元素...不能确定类型，但不造成解码错误
                }
                default -> {
                    throw new DsonCodecException("unexpected dsonType: " + firstDsonType);
                }
            }
            reader.readEndArray();
        }
        return result;
    }
}