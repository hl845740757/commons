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

    public MapEncodeProxyCodec() {
    }

    @Nonnull
    @Override
    public Class<MapEncodeProxy> getEncoderClass() {
        return MapEncodeProxy.class;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, MapEncodeProxy instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        TypeInfo<?> valueArgInfo = typeInfo.isGenericType() ? typeInfo.getGenericArgument(0) : TypeInfo.OBJECT;
        @SuppressWarnings("unchecked") Collection<Map.Entry<String, Object>> entries = Objects.requireNonNull(instance.getEntries());
        switch (instance.getMode()) {
            default -> {
                writer.writeStartObject(instance, typeInfo, style); // 字典写为普通文档
                for (Map.Entry<String, ?> entry : entries) {
                    // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                    writer.writeName(entry.getKey());
                    writer.writeObject(entry.getKey(), entry.getValue(), valueArgInfo, null);
                }
                writer.writeEndObject();
            }
            case MapEncodeProxy.MODE_ARRAY -> {
                writer.writeStartArray(instance, typeInfo, style); // 整个字典写为数组
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeString(null, entry.getKey());
                    writer.writeObject(null, entry.getValue(), valueArgInfo, null);
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_ARRAY -> {
                TypeInfo<Map.Entry> pairTypeInfo = TypeInfo.of(Map.Entry.class, String.class, valueArgInfo.rawType);
                writer.writeStartArray(instance, typeInfo, style);
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeStartArray(entry, pairTypeInfo); // pair写为子数组
                    {
                        writer.writeString(null, entry.getKey());
                        writer.writeObject(null, entry.getValue(), valueArgInfo, null);
                    }
                    writer.writeEndArray();
                }
                writer.writeEndArray();
            }
            case MapEncodeProxy.MODE_PAIR_AS_DOCUMENT -> {
                TypeInfo<Map.Entry> pairTypeInfo = TypeInfo.of(Map.Entry.class, String.class, valueArgInfo.rawType);
                writer.writeStartArray(instance, typeInfo, style);
                for (Map.Entry<String, ?> entry : entries) {
                    writer.writeStartObject(entry, pairTypeInfo); // pair写为子文档
                    {
                        writer.writeObject(entry.getKey(), entry.getValue(), valueArgInfo);
                    }
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }

        }
    }

    @Override
    public MapEncodeProxy readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends MapEncodeProxy> factory) {
        TypeInfo<?> valueArgInfo = typeInfo.isGenericType() ? typeInfo.getGenericArgument(0) : TypeInfo.OBJECT;

        List<Map.Entry<String, Object>> entries = new ArrayList<>();
        MapEncodeProxy<Object> result = new MapEncodeProxy<>();
        result.setEntries(entries);

        DsonType currentDsonType = reader.getCurrentDsonType();
        if (currentDsonType == DsonType.OBJECT) {
            result.setWriteAsDocument(); // 对方写为普通对象
            reader.readStartObject();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                String key = reader.readName();
                Object value = reader.readObject(key, valueArgInfo);
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
                        Object value = reader.readObject(null, valueArgInfo);
                        entries.add(Tuple2.of(key, value));
                    } while (reader.readDsonType() != DsonType.END_OF_OBJECT);
                }
                case ARRAY -> { // Pair为子数组
                    result.setWritePairAsArray();
                    do {
                        reader.readStartArray();
                        {
                            String key = reader.readString(null);
                            Object value = reader.readObject(null, valueArgInfo);
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
                            Object value = reader.readObject(key, valueArgInfo);
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