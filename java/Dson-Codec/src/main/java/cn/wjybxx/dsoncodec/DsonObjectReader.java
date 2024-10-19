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

import cn.wjybxx.base.annotation.StableName;
import cn.wjybxx.dson.DsonContextType;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nullable;
import java.time.LocalDateTime;
import java.util.function.Supplier;

/**
 * 1.读取数组内普通成员时，name传null，读取嵌套对象时使用无name参数的start方法
 * 2.为减少API数量，我们的所有简单值读取都是带有name参数的，在已读取name的情况下，接口的name参数将被忽略。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@SuppressWarnings("unused")
public interface DsonObjectReader extends AutoCloseable {

    // region 简单值

    int readInt(String name);

    long readLong(String name);

    float readFloat(String name);

    double readDouble(String name);

    boolean readBoolean(String name);

    String readString(String name);

    void readNull(String name);

    default byte[] readBytes(String name) {
        Binary binary = readBinary(name);
        return binary == null ? null : binary.unsafeBuffer();
    }

    Binary readBinary(String name);

    ObjectPtr readPtr(String name);

    ObjectLitePtr readLitePtr(String name);

    LocalDateTime readDateTime(String name);

    // ExtDateTime并不常见
    ExtDateTime readExtDateTime(String name);

    Timestamp readTimestamp(String name);
    // endregion

    // region object封装

    /**
     * 读取嵌套对象
     * 注意：
     * 1. 该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
     * 2. 目标类型可以与写入类型不一致，甚至无继承关系，只要数据格式兼容即可 —— 投影。
     *
     * @param name         数组内元素传null或空字符串
     * @param declaredType 对象声明类型信息
     */
    @Nullable
    <T> T readObject(String name, TypeInfo declaredType, Supplier<? extends T> factory);

    default <T> T readObject(String name, TypeInfo declaredType) {
        return readObject(name, declaredType, null);
    }

    // endregion

    // region 流程

    ConverterOptions options();

    DsonContextType getContextType();

    /** 读取下一个数据的类型 */
    DsonType readDsonType();

    /** 读取下一个数据的name */
    String readName();

    /**
     * 读取指定名字的值 -- 可实现随机读
     * 如果尚未调用{@link #readDsonType()}，该方法将尝试跳转到该name所在的字段。
     * 如果已调用{@link #readDsonType()}，则name必须与下一个name匹配。
     * 如果已调用{@link #readName()}，则name可以为null，否则必须当前name匹配。
     * 如果reader不支持随机读，当名字不匹配下一个值时将抛出异常。
     * 返回false的情况下，可继续调用该方法或{@link #readDsonType()}读取下一个字段。
     *
     * @return 如果是Object上下文，如果字段存在则返回true，否则返回false；
     * 如果是Array上下文，如果尚未到达数组尾部，则返回true，否则返回false
     */
    boolean readName(String name);

    DsonType getCurrentDsonType();

    String getCurrentName();

    /** typeInfo用于传递给嵌套对象，以及暂存到Context */
    void readStartObject();

    void readEndObject();

    void readStartArray();

    void readEndArray();

    void skipName();

    void skipValue();

    void skipToEndOfObject();

    byte[] readValueAsBytes(String name);

    /** 解码字典的key */
    <T> T decodeKey(String keyString, TypeInfo keyTypeInfo);

    /** 设置数组/object的value的类型，用于精确解析Dson文本 */
    void setComponentType(DsonType dsonType);

    /**
     * 设置当前对象的encoderType
     * 1.java特殊支持，用于读写Object/Array期间查询当前对象的类型信息
     * 2.应当在readStartObject/Array以后调用
     */
    void setEncoderType(TypeInfo encoderType);

    /** 获取当前对象的类型信息 */
    TypeInfo getEncoderType();

    @Override
    void close();

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartObject(String name) {
        if (readName(name)) {
            readStartObject();
            return true;
        }
        return false;
    }

    /** @return 如果存在对应的字段则返回true */
    default boolean readStartArray(String name) {
        if (readName(name)) {
            readStartArray();
            return true;
        }
        return false;
    }

    // endregion

    // region 快捷方法

    /** 应当减少 short/byte/char 的使用，尤其应当避免使用其包装类型，使用的越多越难以扩展，越难以支持跨语言等。 */
    @StableName
    default short readShort(String name) {
        return (short) readInt(name);
    }

    @StableName
    default byte readByte(String name) {
        return (byte) readInt(name);
    }

    @StableName
    default char readChar(String name) {
        return (char) readInt(name);
    }

    // endregion
}