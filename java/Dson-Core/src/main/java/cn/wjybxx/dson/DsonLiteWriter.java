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
import cn.wjybxx.dson.types.*;

/**
 * 由于java的泛型是擦除实现，我们为避免拆装箱开销，提供了几乎重复的实现。
 * <p>
 * 0.Object/Header先写入name再写入value，数组直接写入value。
 * 1.写数组简单元素的时候，name为字符串类型时，传null或空字符串，name为数字类型时传0；
 * 2.写数组对象元素时使用无name参数的start方法（实在不想定义太多的方法）；
 * 3.为减少API数量，我们的所有简单值写入都是带有name参数的，在已经写入name的情况下，接口的name参数将被忽略。
 * 4.double、boolean、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签，
 * 5.内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonLiteWriter extends AutoCloseable {

    void flush();

    @Override
    void close();

    /** 获取当前上下文的类型 */
    DsonContextType getContextType();

    /** 获取当前写入的name -- 如果先调用WriteName */
    int getCurrentName();

    /** 当前是否处于等待写入name的状态 */
    boolean isAtName();

    /**
     * 编码的时候，用户总是习惯 name和value 同时写入，
     * 但在写Array或Object容器的时候，不能同时完成，需要先写入number再开始写值
     */
    void writeName(int name);

    // region 简单值

    void writeInt32(int name, int value, WireType wireType);

    void writeInt64(int name, long value, WireType wireType);

    void writeFloat(int name, float value);

    void writeDouble(int name, double value);

    void writeBool(int name, boolean value);

    void writeString(int name, String value);

    void writeNull(int name);

    void writeBinary(int name, Binary binary);

    void writeBinary(int name, DsonChunk chunk);

    void writePtr(int name, ObjectPtr objectPtr);

    void writeLitePtr(int name, ObjectLitePtr objectLitePtr);

    void writeDateTime(int name, ExtDateTime dateTime);

    void writeTimestamp(int name, Timestamp timestamp);
    // endregion

    // region 容器

    void writeStartArray();

    void writeEndArray();

    void writeStartObject();

    void writeEndObject();

    void writeStartHeader();

    void writeEndHeader();

    /**
     * 开始写一个数组
     * 1.可以写入header
     * 2.数组内元素没有名字，因此name传 0 即可
     *
     * <pre>{@code
     *      writer.writeStartArray(name);
     *      for (String coderName: coderNames) {
     *          writer.writeString(0, coderName);
     *      }
     *      writer.writeEndArray();
     * }</pre>
     */
    default void writeStartArray(int name) {
        writeName(name);
        writeStartArray();
    }

    /**
     * 开始写一个普通对象
     * 1.可以写入header
     *
     * <pre>{@code
     *      writer.writeStartObject(name);
     *      writer.writeString("name", "wjybxx")
     *      writer.writeInt32("age", 28)
     *      writer.writeEndObject();
     * }</pre>
     */
    default void writeStartObject(int name) {
        writeName(name);
        writeStartObject();
    }
    // endregion

    // region 特殊支持

    /**
     * 直接写入一个已编码的字节数组
     * 1.请确保合法性
     * 2.支持的类型与读方法相同
     *
     * @param data {@link DsonLiteReader#readValueAsBytes(int)}读取的数据
     */
    void writeValueBytes(int name, DsonType type, byte[] data);

    /**
     * 附近一个数据到当前上下文
     *
     * @return 旧值
     */
    Object attach(Object userData);

    Object attachment();
    // endregion

}