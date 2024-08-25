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

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.DsonTextWriter;
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
    private final DsonWriter writer;

    public DefaultDsonObjectWriter(DsonConverter converter, DsonWriter writer) {
        this.converter = converter;
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
            writer.writeBinary(name, Binary.copyFrom(value));
        }
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
    public void writeBinary(String name, @Nonnull DsonChunk chunk) {
        Objects.requireNonNull(chunk);
        writer.writeBinary(name, chunk);
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
    public <T> void writeObject(String name, T value, TypeInfo<?> typeInfo, ObjectStyle style) {
        Objects.requireNonNull(typeInfo, "typeInfo");
        if (value == null) {
            writeNull(name);
            return;
        }
        // 常见基础类型也在CodecRegistry中
        DsonCodecImpl<? super T> codec = findObjectEncoder(value);
        if (codec != null) {
            if (writer.isAtName()) { // 写入name
                writer.writeName(name);
            }
            codec.writeObject(this, value, typeInfo, findObjectStyle(value, typeInfo, style));
            return;
        }
        Class<?> type = value.getClass();
        // 类型补充
        if (type == byte[].class) {
            writeBytes(name, (byte[]) value);
            return;
        }
        if (type == Short.class) {
            writeShort(name, (Short) value);
            return;
        }
        if (type == Byte.class) {
            writeByte(name, (Byte) value);
            return;
        }
        if (type == Character.class) {
            writeChar(name, (Character) value);
            return;
        }
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
    public void writeStartObject(@Nonnull Object value, TypeInfo<?> typeInfo, ObjectStyle style) {
        writer.writeStartObject(style);
        writeClsName(value, typeInfo);
    }

    @Override
    public void writeEndObject() {
        writer.writeEndObject();
    }

    @Override
    public void writeStartArray(@Nonnull Object value, TypeInfo<?> typeInfo, ObjectStyle style) {
        writer.writeStartArray(style);
        writeClsName(value, typeInfo);
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
    public String encodeKey(Object key) {
        Objects.requireNonNull(key);
        if (key instanceof String str) {
            return str;
        }
        if ((key instanceof Integer) || (key instanceof Long)) {
            return key.toString();
        }
        if (!(key instanceof EnumLite enumLite)) {
            throw DsonCodecException.unsupportedType(key.getClass());
        }
        if (converter.options().writeEnumAsString) {
            return key.toString();
        } else {
            return Integer.toString(enumLite.getNumber());
        }
    }

    @Override
    public void println() {
        if (writer instanceof DsonTextWriter textWriter) {
            textWriter.println();
        }
    }

    @Override
    public void flush() {
        writer.flush();
    }

    @Override
    public void close() {
        writer.close();
    }

    // 发现个问题：子类使用超类编码器时，如果当前类不存在TypeMeta，是否应当尝试写入超类的TypeMeta？
    // 写入超类信息似乎也不对劲 -- 最好的方式是每个类型都有对应的TypeMeta

    /** 写入clsName时，应当尽可能写上泛型参数信息 */
    private void writeClsName(Object value, TypeInfo<?> typeInfo) {
        if (typeInfo == TypeInfo.NONE) {
            return;
        }
        final Class<?> encodeClass = DsonConverterUtils.getEncodeClass(value); // 小心枚举
        if (!converter.options().classIdPolicy.test(typeInfo.rawType, encodeClass)) {
            return;
        }
        final TypeMeta typeMeta = getEncoderTypeMeta(typeInfo, encodeClass);
        if (typeMeta != null && !typeMeta.clsNames.isEmpty()) {
            writer.writeSimpleHeader(typeMeta.mainClsName());
        }
    }

    /** style可以允许泛型参数不同时走不同的style - 但不是必须的 */
    private ObjectStyle findObjectStyle(Object value, TypeInfo<?> typeInfo, @Nullable ObjectStyle style) {
        if (style != null) return style;
        final Class<?> encodeClass = DsonConverterUtils.getEncodeClass(value); // 小心枚举...
        final TypeMeta typeMeta = getEncoderTypeMeta(typeInfo, encodeClass);
        return typeMeta != null ? typeMeta.style : ObjectStyle.INDENT;
    }

    private TypeMeta getEncoderTypeMeta(TypeInfo<?> typeInfo, Class<?> encoderClass) {
        if (encoderClass == typeInfo.rawType) {
            return converter.typeMetaRegistry().ofType(typeInfo);
        }
        if (DsonConverterUtils.canInheritTypeArgs(encoderClass, typeInfo)) {
            return converter.typeMetaRegistry().ofType(TypeInfo.of(encoderClass, typeInfo.typeArgs));
        }
        return converter.typeMetaRegistry().ofClass(encoderClass);
    }

    /** 查找codec始终通过原始类型查找 -- 这么有问题 */
    @SuppressWarnings("unchecked")
    private <T> DsonCodecImpl<? super T> findObjectEncoder(T value) {
        final Class<?> encodeClass = DsonConverterUtils.getEncodeClass(value); // 小心枚举...
        DsonCodecRegistry rootRegistry = converter.codecRegistry();
        return (DsonCodecImpl<? super T>) rootRegistry.getEncoder(encodeClass, rootRegistry);
    }
    // endregion

}