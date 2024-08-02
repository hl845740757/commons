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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.annotation.StableName;
import cn.wjybxx.dson.DsonContextType;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.time.LocalDateTime;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Supplier;

/**
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
     * @param name     数组内元素传null或空字符串
     * @param typeInfo 对象声明类型信息；可以与写入的类型不一致，
     */
    @Nullable
    <T> T readObject(String name, TypeInfo<T> typeInfo, Supplier<? extends T> factory);

    default <T> T readObject(String name, TypeInfo<T> typeInfo) {
        return readObject(name, typeInfo, null);
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
     * 如果已调用{@link #readDsonType()}，则该方法必须与下一个name匹配。
     * 如果reader不支持随机读，当名字不匹配下一个值时将抛出异常。
     * 返回false的情况下，可继续调用该方法或{@link #readDsonType()}读取下一个字段。
     *
     * @return 如果是Object上下午，如果字段存在则返回true，否则返回false；
     * 如果是Array上下文，如果尚未到达数组尾部，则返回true，否则返回false
     */
    boolean readName(String name);

    DsonType getCurrentDsonType();

    String getCurrentName();

    void readStartObject();

    void readEndObject();

    void readStartArray();

    void readEndArray();

    void skipName();

    void skipValue();

    void skipToEndOfObject();

    byte[] readValueAsBytes(String name);

    /** 解码字典的key */
    <T> T decodeKey(String keyString, Class<T> keyDeclared);

    /** 设置数组/object的value的类型，用于精确解析Dson文本 */
    void setComponentType(DsonType dsonType);

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

    /** 读取为不可变List */
    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> List<E> readImmutableList(String name, Class<E> elementType) {
        final Collection<E> result = readObject(name, TypeInfo.of(Collection.class, elementType));
        return CollectionUtils.toImmutableList(result);
    }

    /** 读取为不可变Set */
    @SuppressWarnings("unchecked")
    @Nonnull
    default <E> Set<E> readImmutableSet(String name, Class<E> elementType) {
        final Set<E> result = readObject(name, TypeInfo.of(Set.class, elementType));
        return CollectionUtils.toImmutableLinkedHashSet(result);
    }

    /** 读取为不可变字典 */
    @SuppressWarnings("unchecked")
    @Nonnull
    default <K, V> Map<K, V> readImmutableMap(String name, Class<K> keyType, Class<V> valueType) {
        final Map<K, V> result = readObject(name, TypeInfo.of(Map.class, keyType, valueType));
        return CollectionUtils.toImmutableLinkedHashMap(result);
    }

    // endregion
}