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
@SuppressWarnings("rawtypes")
public class MapEncodeProxyCodec implements DsonCodec<MapEncodeProxy> {

    private final TypeInfo typeInfo;

    public MapEncodeProxyCodec() {
        typeInfo = TypeInfo.of(MapEncodeProxy.class, TypeInfo.OBJECT);
    }

    public MapEncodeProxyCodec(TypeInfo typeInfo) {
        this.typeInfo = typeInfo;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, MapEncodeProxy instance, TypeInfo declaredType, ObjectStyle style) {
        TypeInfo valueTypeInfo = declaredType.isConstructedGenericType() ? declaredType.getGenericArgument(0) : TypeInfo.OBJECT;
        @SuppressWarnings("unchecked") Collection<Map.Entry<String, Object>> entries = Objects.requireNonNull(instance.getEntries());
        switch (instance.getMode()) {
            default -> {
                writer.writeStartObject(instance, declaredType, style); // 字典写为普通文档
                for (Map.Entry<String, ?> entry : entries) {
                    // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                    writer.writeName(entry.getKey());
                    writer.writeObject(entry.getKey(), entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndObject();
            }
            case MapEncodeProxy.MODE_ARRAY -> {
                writer.writeStartArray(instance, declaredType, style); // 整个字典写为数组
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeString(null, entry.getKey());
                    writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_ARRAY -> {
                writer.writeStartArray(instance, declaredType, style);
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeStartArray(entry, TypeInfo.ARRAY_OBJECT); // pair写为子数组-没有类型
                    {
                        writer.writeString(null, entry.getKey());
                        writer.writeObject(null, entry.getValue(), valueTypeInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_DOCUMENT -> {
                writer.writeStartArray(instance, declaredType, style);
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeStartObject(entry, TypeInfo.OBJECT); // pair写为子文档-没有类型
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
    public MapEncodeProxy readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends MapEncodeProxy> factory) {
        TypeInfo valueTypeInfo = declaredType.isConstructedGenericType() ? declaredType.getGenericArgument(0) : TypeInfo.OBJECT;

        List<Map.Entry<String, Object>> entries = new ArrayList<>();
        MapEncodeProxy<Object> result = new MapEncodeProxy<>();
        result.setEntries(entries);

        DsonType currentDsonType = reader.getCurrentDsonType();
        if (currentDsonType == DsonType.OBJECT) {
            result.setWriteAsDocument(); // 对方写为普通对象
            reader.readStartObject(declaredType);
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String key = reader.readName();
                Object value = reader.readObject(key, valueTypeInfo);
                entries.add(Tuple2.of(key, value)); // Map.entry不支持value为null
            }
            reader.readEndObject();
        } else {
            assert currentDsonType == DsonType.ARRAY;
            reader.readStartArray(declaredType);
            DsonType firstDsonType = reader.readDsonType();
            switch (firstDsonType) {
                case STRING -> { // 整个字典写为数组
                    result.setWriteAsArray();
                    do {
                        String key = reader.readString(null);
                        Object value = reader.readObject(null, valueTypeInfo);
                        entries.add(Tuple2.of(key, value));
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    result.setWritePairAsArray();
                    do {
                        reader.readStartArray(TypeInfo.ARRAY_OBJECT);
                        {
                            String key = reader.readString(null);
                            Object value = reader.readObject(null, valueTypeInfo);
                            entries.add(Tuple2.of(key, value));
                        }
                        reader.readEndArray();
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case OBJECT -> { // Pair为子文档
                    result.setWritePairAsDocument();
                    do {
                        reader.readStartObject(TypeInfo.OBJECT);
                        {
                            String key = reader.readName();
                            Object value = reader.readObject(key, valueTypeInfo);
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