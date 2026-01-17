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

package cn.wjybxx.dson;

import cn.wjybxx.dson.text.Double4Style;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

/**
 * 1.Object/Header先写入name再写入value，数组直接写入value。
 * 2.已写入name的情况下，调用包含name的写入value方法时，name将被忽略。
 * 3.double、boolean、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签；
 * 4.内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonWriter extends AutoCloseable {

    void flush();

    @Override
    void close();

    /** 获取当前上下文的类型 */
    DsonContextType getContextType();

    /** 当前上下文深度 */
    int getContextDepth();

    /** 获取当前写入的name -- 如果先调用WriteName */
    String getCurrentName();

    /** 当前是否处于等待写入name的状态 */
    boolean isAtName();

    /**
     * 编码的时候，用户总是习惯 name和value 同时写入，
     * 但在写Array或Object容器的时候，不能同时完成，需要先写入name再开始写值
     */
    void writeName(String name);

    // region 简单值

    void writeInt32(String name, int value, NumberStyle style);

    void writeInt64(String name, long value, NumberStyle style);

    void writeFloat(String name, float value, NumberStyle style);

    void writeDouble(String name, double value, NumberStyle style);

    void writeBool(String name, boolean value);

    void writeString(String name, String value, StringStyle style);

    void writeNull(String name);

    void writeBinary(String name, Binary binary);

    void writeBinary(String name, byte[] bytes, int offset, int len);

    void writePtr(String name, ObjectPtr objectPtr);

    void writeDateTime(String name, ExtDateTime dateTime);

    void writeTimestamp(String name, Timestamp timestamp);

    void writeDouble4(String name, Double4 double4, Double4Style style);

    // endregion

    // region 简单值(无name版)

    void writeInt32(int value, NumberStyle style);

    void writeInt64(long value, NumberStyle style);

    void writeFloat(float value, NumberStyle style);

    void writeDouble(double value, NumberStyle style);

    void writeBool(boolean value);

    void writeString(String value, StringStyle style);

    void writeNull();

    void writeBinary(Binary binary);

    void writeBinary(byte[] bytes, int offset, int len);

    void writePtr(ObjectPtr objectPtr);

    void writeDateTime(ExtDateTime dateTime);

    void writeTimestamp(Timestamp timestamp);

    void writeDouble4(Double4 double4, Double4Style style);

    // endregion

    // region 容器

    void writeStartArray(ObjectStyle style);

    void writeEndArray();

    void writeStartObject(ObjectStyle style);

    void writeEndObject();

    /** Header应该保持简单，因此通常应该使用Flow模式 */
    void writeStartHeader(ObjectStyle style);

    void writeEndHeader();

    /**
     * 开始写一个数组
     * 1.数组内元素没有名字，因此name传 null或空字符串 即可
     *
     * <pre>{@code
     *      writer.writeStartArray(name, ObjectStyle.INDENT);
     *      for (String coderName: coderNames) {
     *          writer.writeString(null, coderName);
     *      }
     *      writer.writeEndArray();
     * }</pre>
     */
    default void writeStartArray(String name, ObjectStyle style) {
        writeName(name);
        writeStartArray(style);
    }

    /**
     * 开始写一个普通对象
     * <pre>{@code
     *      writer.writeStartObject(name, ObjectStyle.INDENT);
     *      writer.writeString("name", "wjybxx")
     *      writer.writeInt32("age", 28)
     *      writer.writeEndObject();
     * }</pre>
     */
    default void writeStartObject(String name, ObjectStyle style) {
        writeName(name);
        writeStartObject(style);
    }
    // endregion

    // region 特殊支持

    /**
     * 直接写入一个已编码的字节数组
     * 1.请确保合法性
     * 2.支持的类型与读方法相同
     *
     * @param data {@link DsonReader#readValueAsBytes(String)}读取的数据
     */
    void writeValueBytes(String name, DsonType type, byte[] data);

    /**
     * 附近一个数据到当前上下文
     *
     * @return 旧值
     */
    Object attach(Object userData);

    Object attachment();

    /** 配置 */
    DsonWriterSettings getSettings();

    // endregion

    // region 快捷方法

    /** 注：默认为Typed模式，因为需要能够精确恢复。 */
    default void writeInt32(String name, int value) {
        writeInt32(name, value, NumberStyle.TYPED);
    }

    default void writeInt64(String name, long value) {
        writeInt64(name, value, NumberStyle.TYPED);
    }

    default void writeFloat(String name, float value) {
        writeFloat(name, value, NumberStyle.TYPED);
    }

    default void writeDouble(String name, double value) {
        writeDouble(name, value, NumberStyle.SIMPLE);
    }

    default void writeString(String name, String value) {
        writeString(name, value, StringStyle.AUTO_QUOTE);
    }

    default void writeBinary(String name, byte[] bytes) {
        writeBinary(name, bytes, 0, bytes.length);
    }

    default void writeDouble4(String name, Double4 double4) {
        writeDouble4(name, double4, Double4Style.ARRAY4);
    }

    default void writeInt32(int value) {
        writeInt32(value, NumberStyle.TYPED);
    }

    default void writeInt64(long value) {
        writeInt64(value, NumberStyle.TYPED);
    }

    default void writeFloat(float value) {
        writeFloat(value, NumberStyle.TYPED);
    }

    default void writeDouble(double value) {
        writeDouble(value, NumberStyle.SIMPLE);
    }

    default void writeString(String value) {
        writeString(value, StringStyle.AUTO_QUOTE);
    }

    default void writeBinary(byte[] bytes) {
        writeBinary(bytes, 0, bytes.length);
    }

    default void writeDouble4(Double4 double4) {
        writeDouble4(double4, Double4Style.ARRAY4);
    }
    // endregion
}