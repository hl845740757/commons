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

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dsoncodec.DsonCodecException;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.lang.reflect.Field;
import java.util.*;
import java.util.function.Supplier;

/**
 * 枚举编解码器
 * 如果枚举实现了{@link EnumLite}接口，则序列化时使用自定义数字，否则使用{@link Enum#ordinal()}
 *
 * @author wjybxx
 * date - 2023/4/24
 */
@DsonCodecScanIgnore
public final class EnumCodec<T extends Enum<T>> implements IEnumCodec<T> {

    private final Class<T> enumClass;
    private final EnumMap<T, EnumValueInfo<T>> value2EnumMap;
    private final Int2ObjectMap<EnumValueInfo<T>> number2EnumMap;
    private final Map<String, EnumValueInfo<T>> name2EnumMap; // 这允许为枚举指定别名

    /**
     * @param enumClass  枚举类
     * @param valueInfos 枚举值信息，允许自定义枚举序列化数据
     */
    public EnumCodec(Class<T> enumClass, List<EnumValueInfo<T>> valueInfos) {
        this.enumClass = Objects.requireNonNull(enumClass);

        this.value2EnumMap = new EnumMap<>(enumClass);
        this.number2EnumMap = new Int2ObjectOpenHashMap<>(valueInfos.size());
        this.name2EnumMap = HashMap.newHashMap(valueInfos.size());

        for (EnumValueInfo<T> valueInfo : valueInfos) {
            value2EnumMap.put(valueInfo.value, valueInfo);
            number2EnumMap.put(valueInfo.number, valueInfo);
            name2EnumMap.put(valueInfo.name, valueInfo);
        }
    }

    public EnumCodec(Class<T> enumClass) {
        this.enumClass = Objects.requireNonNull(enumClass);

        T[] enumConstants = enumClass.getEnumConstants();
        this.value2EnumMap = new EnumMap<>(enumClass);
        this.number2EnumMap = new Int2ObjectOpenHashMap<>(enumConstants.length);
        this.name2EnumMap = HashMap.newHashMap(enumConstants.length);

        Field[] enumFields = enumClass.getDeclaredFields();
        for (int idx = 0; idx < enumConstants.length; idx++) {
            T value = enumConstants[idx];
            int number = value instanceof EnumLite enumLite ? enumLite.getNumber() : value.ordinal();

            // 可通过注解指定DsonName -- java的枚举常量在所有其它字段之前
            DsonProperty annotation = enumFields[idx].getAnnotation(DsonProperty.class);
            String name;
            if (annotation != null && !ObjectUtils.isBlank(annotation.name())) {
                name = annotation.name();
            } else {
                name = value.name();
            }

            EnumValueInfo<T> valueInfo = new EnumValueInfo<>(value, number, name);
            value2EnumMap.put(valueInfo.value, valueInfo);
            number2EnumMap.put(valueInfo.number, valueInfo);
            name2EnumMap.put(valueInfo.name, valueInfo);
        }
    }

    @Override
    @Nullable
    public T forName(String name) {
        EnumValueInfo<T> valueInfo = name2EnumMap.get(name);
        return valueInfo == null ? null : valueInfo.value;
    }

    @Override
    @Nullable
    public T forNumber(int number) {
        EnumValueInfo<T> valueInfo = number2EnumMap.get(number);
        return valueInfo == null ? null : valueInfo.value;
    }

    @Override
    public String getName(T val) {
        EnumValueInfo<T> valueInfo = value2EnumMap.get(val);
        return valueInfo.name;
    }

    @Override
    public int getNumber(T val) {
        EnumValueInfo<T> valueInfo = value2EnumMap.get(val);
        return valueInfo.number;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return TypeInfo.of(enumClass);
    }

    /** false 可以将枚举简单写为整数 */
    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T inst, TypeInfo declaredType, ObjectStyle style) {
        EnumValueInfo<T> valueInfo = value2EnumMap.get(inst);
        if (valueInfo == null) {
            throw new DsonCodecException("invalid enum value: %s, type: %s".formatted(inst, enumClass));
        }
        if (writer.options().writeEnumAsString) {
            writer.writeString(null, valueInfo.name, StringStyle.UNQUOTE);
        } else {
            writer.writeInt(null, valueInfo.number);
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, Supplier<? extends T> factory) {
        if (reader.options().writeEnumAsString) {
            String name = reader.readString(null);
            EnumValueInfo<T> valueInfo = name2EnumMap.get(name);
            if (valueInfo != null) {
                return valueInfo.value;
            }
            throw new DsonCodecException("invalid enum value: %s, type: %s".formatted(name, enumClass));
        } else {
            int number = reader.readInt(null);
            EnumValueInfo<T> valueInfo = number2EnumMap.get(number);
            if (valueInfo != null) {
                return valueInfo.value;
            }
            throw new DsonCodecException("invalid enum value: %d, type: %s".formatted(number, enumClass));
        }
    }

}