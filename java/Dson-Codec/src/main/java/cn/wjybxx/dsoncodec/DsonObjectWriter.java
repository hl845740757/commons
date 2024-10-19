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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nullable;
import java.time.LocalDateTime;

/**
 * 如果用户期望强制写入null，需要先调用{@link DsonObjectWriter#writeName(String)}，
 * 再调用{@link DsonObjectWriter#writeNull(String)}
 * <p>
 * 1.对于对象类型，如果value为null，将自动调用{@link #writeNull(String)}
 * 2.数组内元素name传null
 * <p>
 * 注意：不要轻易重命名方法，注解处理器对这里的方法名是有依赖的。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@SuppressWarnings("unused")
public interface DsonObjectWriter extends AutoCloseable {

    // region 简单值

    void writeInt(String name, int value, WireType wireType, INumberStyle style);

    void writeLong(String name, long value, WireType wireType, INumberStyle style);

    void writeFloat(String name, float value, INumberStyle style);

    void writeDouble(String name, double value, INumberStyle style);

    void writeBoolean(String name, boolean value);

    void writeString(String name, String value, StringStyle style);

    void writeNull(String name);

    /** bytes默认为不可共享对象 -- 如果不期望拷贝，可先包装为Binary */
    void writeBytes(String name, byte[] value);

    void writeBytes(String name, byte[] value, int offset, int len);

    /** Binary默认为可共享对象 */
    void writeBinary(String name, Binary binary);

    // 内建结构体
    void writePtr(String name, ObjectPtr objectPtr);

    void writeLitePtr(String name, ObjectLitePtr objectLitePtr);

    void writeDateTime(String name, LocalDateTime dateTime);

    // ExtDateTime并不常见
    void writeExtDateTime(String name, ExtDateTime dateTime);

    void writeTimestamp(String name, Timestamp timestamp);

    // endregion

    // region object

    /**
     * 写嵌套对象
     *
     * @param name         字段的名字，数组元素和顶层对象的name可为null或空字符串
     * @param value        要写入的对象
     * @param declaredType 对象的声明类型
     * @param style        对象的编码风格，如果为null则使用目标类型Codec的默认格式
     */
    <T> void writeObject(String name, T value, TypeInfo declaredType, @Nullable ObjectStyle style);

    default <T> void writeObject(String name, T value) {
        writeObject(name, value, TypeInfo.OBJECT, null);
    }

    default <T> void writeObject(String name, T value, TypeInfo declaredType) {
        writeObject(name, value, declaredType, null);
    }

    // endregion

    // region 流程

    ConverterOptions options();

    String getCurrentName();

    void writeName(String name);

    /**
     * 写入类型信息
     * 1.该方法应当在writeStartObject/Array后立即调用，写在所有字段之前。
     * 2.默认会调用{@link #setEncoderType(TypeInfo)}保存类型信息
     *
     * @param encoderType  编码器绑定的类型，真实写入的类型信息
     * @param declaredType 对象的声明类型，用于测试是否写入类型信息
     */
    void writeTypeInfo(TypeInfo encoderType, TypeInfo declaredType);

    /** 开始写入Object */
    void writeStartObject(ObjectStyle style);

    void writeEndObject();

    /** 开始写入Array */
    void writeStartArray(ObjectStyle style);

    void writeEndArray();

    /** 写入已编码的二进制数据 */
    void writeValueBytes(String name, DsonType dsonType, byte[] data);

    /** 编码字典的key */
    <T> String encodeKey(T key, TypeInfo keyType);

    /**
     * 设置当前对象的encoderType
     * 1.java特殊支持，用于读写Object/Array期间查询当前对象的类型信息
     * 2.应当在writeStartObject/Array以后调用
     */
    void setEncoderType(TypeInfo encoderType);

    /** 获取当前对象的类型信息 */
    TypeInfo getEncoderType();

    void flush();

    @Override
    void close();

    // defaults
    default void writeStartObject(ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writeStartObject(style);
        writeTypeInfo(encoderType, declaredType);
    }

    default void writeStartObject(String name, ObjectStyle style) {
        writeName(name);
        writeStartObject(style);
    }

    default void writeStartObject(String name, ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writeName(name);
        writeStartObject(style);
        writeTypeInfo(encoderType, declaredType);
    }
    //

    default void writeStartArray(ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writeStartArray(style);
        writeTypeInfo(encoderType, declaredType);
    }

    default void writeStartArray(String name, ObjectStyle style) {
        writeName(name);
        writeStartArray(style);
    }

    default void writeStartArray(String name, ObjectStyle style, TypeInfo encoderType, TypeInfo declaredType) {
        writeName(name);
        writeStartArray(style);
        writeTypeInfo(encoderType, declaredType);
    }

    // endregion

    // region 便捷方法

    default void writeInt(String name, int value) {
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE); // 这里使用simple -- 外部通常包含明确类型
    }

    default void writeInt(String name, int value, WireType wireType) {
        writeInt(name, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeLong(String name, long value) {
        writeLong(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeLong(String name, long value, WireType wireType) {
        writeLong(name, value, wireType, NumberStyle.SIMPLE);
    }

    default void writeFloat(String name, float value) {
        writeFloat(name, value, NumberStyle.SIMPLE);
    }

    default void writeDouble(String name, double value) {
        writeDouble(name, value, NumberStyle.SIMPLE);
    }

    default void writeString(String name, String value) {
        writeString(name, value, StringStyle.AUTO);
    }

    /** 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。 */
    default void writeShort(String name, short value) {
        writeInt(name, value, WireType.SINT, NumberStyle.SIMPLE);
    }

    default void writeShort(String name, short value, INumberStyle style) {
        writeInt(name, value, WireType.SINT, style);
    }

    default void writeByte(String name, byte value) {
        writeInt(name, value, WireType.SINT, NumberStyle.SIMPLE); // java的byte是有符号数，容易负数
    }

    default void writeByte(String name, byte value, INumberStyle style) {
        writeInt(name, value, WireType.SINT, style);
    }

    default void writeChar(String name, char value) {
        writeInt(name, value, WireType.UINT, NumberStyle.SIMPLE);
    }

    default void writeChar(String name, char value, INumberStyle style) {
        writeInt(name, value, WireType.UINT, style);
    }

    // endregion

}