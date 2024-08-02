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

import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.text.*;
import cn.wjybxx.dson.types.*;

import java.util.Objects;

/**
 * 0. Object/Header先写入name再写入value，数组直接写入value。
 * 1.写数组普通元素的时候，{@code name}传null或空字符串；
 * 2.写数组嵌套对象时使用无name参数的start方法（实在不想定义太多的方法）；
 * 3.为减少API数量，我们的所有简单值写入都是带有name参数的，在已经写入name的情况下，接口的name参数将被忽略。
 * 4.double、boolean、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签；
 * 5.内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
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

    void writeInt32(String name, int value, WireType wireType, INumberStyle style);

    void writeInt64(String name, long value, WireType wireType, INumberStyle style);

    void writeFloat(String name, float value, INumberStyle style);

    void writeDouble(String name, double value, INumberStyle style);

    void writeBool(String name, boolean value);

    void writeString(String name, String value, StringStyle style);

    void writeNull(String name);

    void writeBinary(String name, Binary binary);

    void writeBinary(String name, DsonChunk chunk);

    void writePtr(String name, ObjectPtr objectPtr);

    void writeLitePtr(String name, ObjectLitePtr objectLitePtr);

    void writeDateTime(String name, ExtDateTime dateTime);

    void writeTimestamp(String name, Timestamp timestamp);

    // 快捷方法
    default void writeInt32(String name, int value, WireType wireType) {
        writeInt32(name, value, wireType, NumberStyle.TYPED); // 默认需要打印类型，才能精确解析
    }

    default void writeInt64(String name, long value, WireType wireType) {
        writeInt64(name, value, wireType, NumberStyle.TYPED);
    }

    default void writeFloat(String name, float value) {
        writeFloat(name, value, NumberStyle.TYPED);
    }

    default void writeDouble(String name, double value) {
        writeDouble(name, value, NumberStyle.SIMPLE);
    }

    default void writeString(String name, String value) {
        writeString(name, value, StringStyle.AUTO);
    }

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
     * 写入一个简单对象头 -- 仅有一个clsName属性的header。
     * 1.该接口是为{@link DsonTextWriter}定制的，以支持简写。
     * 2.对于其它Writer，则等同于普通写入。
     */
    default void writeSimpleHeader(String clsName) {
        Objects.requireNonNull(clsName, "clsName");
        writeStartHeader(ObjectStyle.FLOW);
        writeString(DsonHeader.NAMES_CLASS_NAME, clsName, StringStyle.AUTO_QUOTE);
        writeEndHeader();
    }

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

    // endregion

}