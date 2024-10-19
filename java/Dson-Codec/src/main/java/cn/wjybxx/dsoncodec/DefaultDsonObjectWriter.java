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

import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.time.LocalDateTime;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
final class DefaultDsonObjectWriter implements DsonObjectWriter {

    private final DsonConverter converter;
    private final TypeWriteHelper typeWriteHelper;
    private final DsonWriter writer;

    public DefaultDsonObjectWriter(DsonConverter converter, TypeWriteHelper typeWriteHelper, DsonWriter writer) {
        this.converter = converter;
        this.typeWriteHelper = typeWriteHelper;
        this.writer = writer;
    }

    // region 简单值

    @Override
    public void writeInt(String name, int value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options().appendDef)) {
            writer.writeInt32(name, value, wireType, style);
        }
    }

    @Override
    public void writeLong(String name, long value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options().appendDef)) {
            writer.writeInt64(name, value, wireType, style);
        }
    }

    @Override
    public void writeFloat(String name, float value, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options().appendDef)) {
            writer.writeFloat(name, value, style);
        }
    }

    @Override
    public void writeDouble(String name, double value, INumberStyle style) {
        if (value != 0 || (!writer.isAtName() || converter.options().appendDef)) {
            writer.writeDouble(name, value, style);
        }
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        if (value || (!writer.isAtName() || converter.options().appendDef)) {
            writer.writeBool(name, value);
        }
    }

    @Override
    public void writeString(String name, @Nullable String value, StringStyle style) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeString(name, value, style);
        }
    }

    @Override
    public void writeNull(String name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.isAtName() || converter.options().appendNull) {
            writer.writeNull(name);
        }
    }

    @Override
    public void writeBytes(String name, byte[] value) {
        if (value == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, value, 0, value.length);
        }
    }

    @Override
    public void writeBytes(String name, byte[] value, int offset, int len) {
        if (value == null) throw new NullPointerException("value");
        writer.writeBinary(name, value, offset, len);
    }

    @Override
    public void writeBinary(String name, Binary binary) {
        if (binary == null) {
            writeNull(name);
        } else {
            writer.writeBinary(name, binary);
        }
    }

    @Override
    public void writePtr(String name, ObjectPtr objectPtr) {
        if (objectPtr == null) {
            writeNull(name);
        } else {
            writer.writePtr(name, objectPtr);
        }
    }

    @Override
    public void writeLitePtr(String name, ObjectLitePtr objectLitePtr) {
        if (objectLitePtr == null) {
            writeNull(name);
        } else {
            writer.writeLitePtr(name, objectLitePtr);
        }
    }

    @Override
    public void writeDateTime(String name, LocalDateTime dateTime) {
        if (dateTime == null) {
            writeNull(name);
        } else {
            writer.writeDateTime(name, ExtDateTime.ofDateTime(dateTime));
        }
    }

    @Override
    public void writeExtDateTime(String name, ExtDateTime dateTime) {
        if (dateTime == null) {
            writeNull(name);
        } else {
            writer.writeDateTime(name, dateTime);
        }
    }

    @Override
    public void writeTimestamp(String name, Timestamp timestamp) {
        if (timestamp == null) {
            writeNull(name);
        } else {
            writer.writeTimestamp(name, timestamp);
        }
    }
    // endregion

    // region object处理

    @Override
    public <T> void writeObject(String name, T value, TypeInfo declaredType, ObjectStyle style) {
        Objects.requireNonNull(declaredType, "typeInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        TypeInfo runtimeTypeInfo = getRuntimeTypeInfo(value, declaredType);
        @SuppressWarnings("unchecked") var codec = (DsonCodecImpl<? super T>) converter.codecRegistry().getEncoder(runtimeTypeInfo);
        if (codec != null) {
            if (writer.isAtName()) { // 写入name
                writer.writeName(name);
            }
            if (style == null) style = findObjectStyle(codec.getEncoderType());
            codec.writeObject(this, value, declaredType, style);
            return;
        }
        Class<?> type = value.getClass();
        // DsonValue
        if (value instanceof DsonValue dsonValue) {
            Dsons.writeDsonValue(writer, dsonValue, name);
            return;
        }
        throw DsonCodecException.unsupportedType(type);
    }
    // endregion

    // region 流程

    @Override
    public ConverterOptions options() {
        return converter.options();
    }

    @Override
    public String getCurrentName() {
        return writer.getCurrentName();
    }

    @Override
    public void writeName(String name) {
        writer.writeName(name);
    }

    @Override
    public void writeTypeInfo(TypeInfo encoderType, TypeInfo declaredType) {
        writer.attach(encoderType);

        TypeWritePolicy policy = converter.options().typeWritePolicy;
        if ((policy == TypeWritePolicy.OPTIMIZED && !typeWriteHelper.isOptimizable(encoderType, declaredType))
                || policy == TypeWritePolicy.ALWAYS) {
            TypeMeta typeMeta = converter.typeMetaRegistry().ofType(encoderType);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta of encoderType: %s is absent".formatted(encoderType));
            }
            writer.writeSimpleHeader(typeMeta.mainClsName());
        }
    }

    @Override
    public void writeStartObject(ObjectStyle style) {
        writer.writeStartObject(style);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(ObjectStyle style) {
        writer.writeStartArray(style);
    }

    @Override
    public void writeEndArray() {
        writer.writeEndArray();
    }

    @Override
    public void writeValueBytes(String name, DsonType dsonType, byte[] data) {
        Objects.requireNonNull(data);
        writer.writeValueBytes(name, dsonType, data);
    }

    @Override
    public <T> String encodeKey(T key, TypeInfo keyType) {
        Objects.requireNonNull(key);
        if (key instanceof String str) {
            return str;
        }
        if ((key instanceof Integer) || (key instanceof Long)) {
            return key.toString();
        }
        @SuppressWarnings("unchecked") var codecImpl = (DsonCodecImpl<T>) converter.codecRegistry().getEncoder(keyType);
        if (codecImpl == null || !codecImpl.isEnumCodec()) {
            throw DsonCodecException.unsupportedType(key.getClass());
        }
        if (converter.options().writeEnumAsString) {
            return codecImpl.getName(key);
        } else {
            return Integer.toString(codecImpl.getNumber(key));
        }
    }

    @Override
    public void setEncoderType(TypeInfo encoderType) {
        writer.attach(encoderType);
    }

    @Override
    public TypeInfo getEncoderType() {
        return (TypeInfo) writer.attachment();
    }

    @Override
    public void flush() {
        writer.flush();
    }

    @Override
    public void close() {
        writer.close();
    }

    /** 允许泛型参数不同时走不同的style */
    private ObjectStyle findObjectStyle(TypeInfo encoderType) {
        final TypeMeta typeMeta = converter.typeMetaRegistry().ofType(encoderType);
        return typeMeta != null ? typeMeta.style : ObjectStyle.INDENT;
    }

    /** 计算对象的运行时类型信息 */
    private TypeInfo getRuntimeTypeInfo(Object value, TypeInfo declaredType) {
        final Class<?> encoderClass = DsonConverterUtils.getEncodeClass(value); // 小心枚举...
        if (encoderClass == declaredType.rawType) {
            return declaredType;
        }
        // 尝试继承泛型参数
        if (declaredType.hasGenericArgs()) {
            TypeInfo typeInfo = converter.genericCodecHelper().inheritTypeArgs(encoderClass, declaredType);
            return typeInfo == null ? TypeInfo.of(encoderClass) : typeInfo;
        }
        // 如果真实类型是泛型，而声明类型是object等，会导致泛型信息丢失
        // 在查找泛型类对应的codec时会修正为对应的泛型原型，从而保证泛型参数个数的正确性
        return TypeInfo.of(encoderClass);
    }

    // endregion

    // region 重复实现，提高效率

    @Override
    public void writeStartObject(ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writer.writeStartObject(style);
        writeTypeInfo(encoderType, declaredType);
    }

    @Override
    public void writeStartObject(String name, ObjectStyle style) {
        writer.writeName(name);
        writer.writeStartObject(style);
    }

    @Override
    public void writeStartObject(String name, ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writer.writeName(name);
        writer.writeStartObject(style);
        writeTypeInfo(encoderType, declaredType);
    }
    //

    @Override
    public void writeStartArray(ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writer.writeStartArray(style);
        writeTypeInfo(encoderType, declaredType);
    }

    @Override
    public void writeStartArray(String name, ObjectStyle style) {
        writer.writeName(name);
        writer.writeStartArray(style);
    }

    @Override
    public void writeStartArray(String name, ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writer.writeName(name);
        writer.writeStartArray(style);
        writeTypeInfo(encoderType, declaredType);
    }
    // endregion
}