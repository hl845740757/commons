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
import cn.wjybxx.dsoncodec.codecs.IEnumCodec;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public final class DsonCodecImpl<T> {

    private final DsonCodec<T> codec;
    private final TypeInfo encoderType; // 统一缓存TypeInfo
    private final boolean autoStart; // 避免查找default方法
    private final boolean writeAsArray;
    private final IEnumCodec<T> enumCodec;

    DsonCodecImpl(DsonCodec<T> codec) {
        this.encoderType = Objects.requireNonNull(codec.getEncoderType());
        this.codec = codec;
        this.autoStart = codec.autoStartEnd();
        this.writeAsArray = autoStart && codec.isWriteAsArray();

        if (codec instanceof IEnumCodec<?>) {
            @SuppressWarnings("unchecked") IEnumCodec<T> enumCodec = (IEnumCodec<T>) codec;
            this.enumCodec = enumCodec;
        } else {
            this.enumCodec = null;
        }
    }

    public DsonCodec<T> getCodec() {
        return codec;
    }

    @Nonnull
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     */
    public void writeObject(DsonObjectWriter writer, T inst, TypeInfo declaredType, ObjectStyle style) {
        if (autoStart) {
            if (writeAsArray) {
                writer.writeStartArray(style);
                writer.writeTypeInfo(encoderType, declaredType);
                codec.writeObject(writer, inst, declaredType, style);
                writer.writeEndArray();
            } else {
                writer.writeStartObject(style);
                writer.writeTypeInfo(encoderType, declaredType);
                codec.writeObject(writer, inst, declaredType, style);
                writer.writeEndObject();
            }
        } else {
            codec.writeObject(writer, inst, declaredType, style);
        }
    }

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     *
     * @param declaredType 对象的声明类型，java是伪泛型，可能需要从声明类型中获取一些信息
     */
    public T readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends T> factory) {
        if (autoStart) {
            T result;
            if (writeAsArray) {
                reader.readStartArray();
                reader.setEncoderType(encoderType);
                result = codec.readObject(reader, factory);
                reader.readEndArray();
            } else {
                reader.readStartObject();
                reader.setEncoderType(encoderType);
                result = codec.readObject(reader, factory);
                reader.readEndObject();
            }
            return result;
        } else {
            return codec.readObject(reader, factory);
        }
    }

    public boolean isEnumCodec() {
        return enumCodec != null;
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

    public int getNumber(T val) {
        if (enumCodec != null) {
            return enumCodec.getNumber(val);
        }
        throw new DsonCodecException("unexpected getNumber method call");
    }

    public String getName(T val) {
        if (enumCodec != null) {
            return enumCodec.getName(val);
        }
        throw new DsonCodecException("unexpected getName method call");
    }
}