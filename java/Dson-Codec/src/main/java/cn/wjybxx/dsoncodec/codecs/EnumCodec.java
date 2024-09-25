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
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.EnumMap;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * 枚举编解码器
 * 如果枚举实现了{@link EnumLite}接口，则序列化时使用自定义数字，否则使用{@link Enum#ordinal()}
 *
 * @author wjybxx
 * date - 2023/4/24
 */
@DsonCodecScanIgnore
public final class EnumCodec<T extends Enum<T>> extends AbstractEnumCodec<T> implements DsonCodec<T> {

    private final Class<T> encoderClass;
    private final EnumMap<T, EnumValueInfo<T>> value2EnumMap;
    private final Int2ObjectMap<EnumValueInfo<T>> number2EnumMap;
    private final Map<String, EnumValueInfo<T>> name2EnumMap; // 这允许为枚举指定别名

    public EnumCodec(Class<T> encoderClass) {
        if (!encoderClass.isEnum()) {
            throw new IllegalArgumentException("EnumLite must be enum class, type: " + encoderClass);
        }
        this.encoderClass = Objects.requireNonNull(encoderClass);

        T[] enumConstants = encoderClass.getEnumConstants();
        this.value2EnumMap = new EnumMap<>(encoderClass);
        this.number2EnumMap = new Int2ObjectOpenHashMap<>(enumConstants.length);
        this.name2EnumMap = HashMap.newHashMap(enumConstants.length);
        for (T enumConstant : enumConstants) {
            int number = enumConstant instanceof EnumLite enumLite ? enumLite.getNumber() : enumConstant.ordinal();

            EnumValueInfo<T> enumValueInfo = new EnumValueInfo<>(enumConstant, number, enumConstant.name());
            value2EnumMap.put(enumConstant, enumValueInfo);
            number2EnumMap.put(number, enumValueInfo);
            name2EnumMap.put(enumConstant.name(), enumValueInfo);
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

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return encoderClass;
    }

    /** false 可以将枚举简单写为整数 */
    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo typeInfo, ObjectStyle style) {
        EnumValueInfo<T> valueInfo = value2EnumMap.get(instance);
        if (valueInfo == null) {
            throw new DsonCodecException("invalid enum value: %s, type: %s".formatted(instance, encoderClass));
        }
        if (writer.options().writeEnumAsString) {
            writer.writeString(null, valueInfo.name, StringStyle.UNQUOTE);
        } else {
            writer.writeInt(null, valueInfo.number);
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo typeInfo, Supplier<? extends T> factory) {
        if (reader.options().writeEnumAsString) {
            String name = reader.readString(reader.getCurrentName());
            EnumValueInfo<T> valueInfo = name2EnumMap.get(name);
            if (valueInfo != null) {
                return valueInfo.value;
            }
            throw new DsonCodecException("invalid enum value: %s, type: %s".formatted(name, encoderClass));
        } else {
            int number = reader.readInt(reader.getCurrentName());
            EnumValueInfo<T> valueInfo = number2EnumMap.get(number);
            if (valueInfo != null) {
                return valueInfo.value;
            }
            throw new DsonCodecException("invalid enum value: %d, type: %s".formatted(number, encoderClass));
        }
    }

    private static class EnumValueInfo<T> {
        final T value;
        final int number;
        final String name;

        public EnumValueInfo(T value, int number, String name) {
            this.value = value;
            this.number = number;
            this.name = name;
        }
    }
}