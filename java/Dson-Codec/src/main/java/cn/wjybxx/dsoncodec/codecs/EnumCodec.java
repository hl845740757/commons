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
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.function.IntFunction;
import java.util.function.Supplier;

/**
 * 让枚举的Codec继承该Codec的话，生成的代码就更为稳定，我们调整编解码方式也更方便。
 *
 * @author wjybxx
 * date - 2023/4/24
 */
@DsonCodecScanIgnore
public class EnumCodec<T extends EnumLite> extends AbstractEnumCodec<T> implements DsonCodec<T> {

    private final Class<T> encoderClass;
    private final IntFunction<T> mapper;
    private final Map<String, T> name2EnumMap;

    /**
     * @param mapper forNumber静态方法的lambda表达式
     */
    public EnumCodec(Class<T> encoderClass, IntFunction<T> mapper) {
        if (!encoderClass.isEnum()) {
            throw new IllegalArgumentException("EnumLite must be enum class, type: " + encoderClass);
        }
        // 虽然可以支持覆盖toString，但我们选择不支持，保持统一的规范更好
        final boolean overrideToString = Arrays.stream(encoderClass.getDeclaredMethods())
                .anyMatch(e -> "toString".equals(e.getName()));
        if (overrideToString) {
            throw new IllegalArgumentException("enum class cannot override toString methods, type: " + encoderClass);
        }

        this.encoderClass = Objects.requireNonNull(encoderClass);
        this.mapper = Objects.requireNonNull(mapper);

        T[] enumConstants = encoderClass.getEnumConstants();
        name2EnumMap = HashMap.newHashMap(enumConstants.length);
        for (T enumConstant : enumConstants) {
            Enum<?> anEnum = (Enum<?>) enumConstant;
            name2EnumMap.put(anEnum.name(), enumConstant);
        }
    }

    @Override
    @Nullable
    public T forName(String name) {
        return name2EnumMap.get(name);
    }

    @Override
    @Nullable
    public T forNumber(int number) {
        return mapper.apply(number);
    }

    @Nonnull
    @Override
    public final Class<T> getEncoderClass() {
        return encoderClass;
    }

    /** false 可以将枚举简单写为整数 */
    @Override
    public final boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        if (writer.options().writeEnumAsString) {
            writer.writeString(null, instance.toString(), StringStyle.UNQUOTE);
        } else {
            writer.writeInt(null, instance.getNumber());
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends T> factory) {
        if (reader.options().writeEnumAsString) {
            return name2EnumMap.get(reader.readString(reader.getCurrentName()));
        } else {
            return mapper.apply(reader.readInt(reader.getCurrentName()));
        }
    }
}