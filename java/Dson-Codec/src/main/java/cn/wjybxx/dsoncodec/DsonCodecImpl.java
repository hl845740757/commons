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

package cn.wjybxx.dsoncodec;

import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.codecs.AbstractEnumCodec;
import cn.wjybxx.dsoncodec.codecs.EnumCodec;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DsonCodecImpl<T> {

    private final DsonCodec<T> codec;
    private final boolean autoStart; // 避免查找default方法
    private final boolean writeAsArray;
    private final AbstractEnumCodec<T> enumCodec;

    public DsonCodecImpl(DsonCodec<T> codec) {
        Objects.requireNonNull(codec.getEncoderClass());
        this.codec = codec;
        this.autoStart = codec.autoStartEnd();
        this.writeAsArray = codec.isWriteAsArray();

        if (codec instanceof AbstractEnumCodec<?>) {
            @SuppressWarnings("unchecked") AbstractEnumCodec<T> enumCodec = (AbstractEnumCodec<T>) codec;
            this.enumCodec = enumCodec;
        } else {
            this.enumCodec = null;
        }
    }

    @Nonnull
    public Class<T> getEncoderClass() {
        return codec.getEncoderClass();
    }

    /** 是否编码为数组 */
    public boolean isWriteAsArray() {
        return writeAsArray;
    }

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        if (autoStart) {
            if (writeAsArray) {
                writer.writeStartArray(instance, typeInfo, style);
                codec.writeObject(writer, instance, typeInfo, style);
                writer.writeEndArray();
            } else {
                writer.writeStartObject(instance, typeInfo, style);
                codec.writeObject(writer, instance, typeInfo, style);
                writer.writeEndObject();
            }
        } else {
            codec.writeObject(writer, instance, typeInfo, style);
        }
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     */
    public T readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends T> factory) {
        if (autoStart) {
            T result;
            if (writeAsArray) {
                reader.readStartArray();
                result = codec.readObject(reader, typeInfo, factory);
                reader.readEndArray();
            } else {
                reader.readStartObject();
                result = codec.readObject(reader, typeInfo, factory);
                reader.readEndObject();
            }
            return result;
        } else {
            return codec.readObject(reader, typeInfo, factory);
        }
    }

    public boolean isEnumCodec() {
        return codec instanceof EnumCodec;
    }

    public T forNumber(int number) {
        if (enumCodec != null) {
            return enumCodec.forNumber(number);
        }
        throw new DsonCodecException("unexpected forNumber method call");
    }

    public T forName(String name) {
        if (enumCodec != null) {
            return enumCodec.forName(name);
        }
        throw new DsonCodecException("unexpected forName method call");
    }
}