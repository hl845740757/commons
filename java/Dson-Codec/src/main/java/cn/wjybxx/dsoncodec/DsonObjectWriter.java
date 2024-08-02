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
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
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

    /** 该方法默认会拷贝value -- 如果不想拷贝，可提前转换为Binary */
    void writeBytes(String name, byte[] value);

    // Binary
    void writeBinary(String name, Binary binary);

    void writeBinary(String name, @Nonnull DsonChunk chunk);

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
     * @param name     字段的名字，数组元素和顶层对象的name可为null或空字符串
     * @param value    要写入的对象，{@code getClass}获取真实类型 -- 小心枚举。
     * @param typeInfo 对象的类型参数信息
     * @param style    对象的编码风格，如果为null则使用目标类型Codec的默认格式
     */
    <T> void writeObject(String name, T value, TypeInfo<?> typeInfo, @Nullable ObjectStyle style);

    default <T> void writeObject(String name, T value) {
        writeObject(name, value, TypeInfo.OBJECT, null);
    }

    default <T> void writeObject(String name, T value, TypeInfo<?> typeInfo) {
        writeObject(name, value, typeInfo, null);
    }

    // endregion

    // region 流程

    ConverterOptions options();

    String getCurrentName();

    void writeName(String name);

    void writeStartObject(@Nonnull Object value, TypeInfo<?> typeInfo, ObjectStyle style);

    void writeEndObject();

    void writeStartArray(@Nonnull Object value, TypeInfo<?> typeInfo, ObjectStyle style);

    void writeEndArray();

    void writeValueBytes(String name, DsonType dsonType, byte[] data);

    /** 编码字典的key */
    String encodeKey(Object key);

    /** 打印换行，用于控制Dson文本的样式 */
    void println();

    void flush();

    @Override
    void close();

    // defaults
    default void writeStartObject(Object value, TypeInfo<?> typeInfo) {
        writeStartObject(value, typeInfo, ObjectStyle.INDENT);
    }

    default void writeStartObject(String name, Object value, TypeInfo<?> typeInfo) {
        writeName(name);
        writeStartObject(value, typeInfo, ObjectStyle.INDENT);
    }

    default void writeStartObject(String name, Object value, TypeInfo<?> typeInfo, ObjectStyle style) {
        writeName(name);
        writeStartObject(value, typeInfo, style);
    }

    default void writeStartArray(Object value, TypeInfo<?> typeInfo) {
        writeStartArray(value, typeInfo, ObjectStyle.INDENT);
    }

    default void writeStartArray(String name, Object value, TypeInfo<?> typeInfo) {
        writeName(name);
        writeStartArray(value, typeInfo, ObjectStyle.INDENT);
    }

    default void writeStartArray(String name, Object value, TypeInfo<?> typeInfo, ObjectStyle style) {
        writeName(name);
        writeStartArray(value, typeInfo, style);
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
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeShort(String name, short value, WireType wireType, INumberStyle style) {
        writeInt(name, value, wireType, style);
    }

    default void writeByte(String name, byte value) {
        writeInt(name, value, WireType.VARINT, NumberStyle.SIMPLE);
    }

    default void writeByte(String name, byte value, WireType wireType, INumberStyle style) {
        writeInt(name, value, WireType.VARINT, style);
    }

    default void writeChar(String name, char value) {
        writeInt(name, value, WireType.UINT, NumberStyle.SIMPLE);
    }

    /** @apiNote 保持签名以确保生成的代码可正确调用 */
    default void writeChar(String name, char value, WireType ignore, INumberStyle style) {
        writeInt(name, value, WireType.UINT, style);
    }

    // endregion

}