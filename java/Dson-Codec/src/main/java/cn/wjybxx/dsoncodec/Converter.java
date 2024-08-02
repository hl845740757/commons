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

import cn.wjybxx.dson.io.DsonChunk;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.function.Supplier;

/**
 * Q：如何解决集合和Map的编解码问题？
 * A：为使用到的集合分配{@link TypeMeta}，并利用Utils类提供的方法创建对应{@code CollectionCodec}和{@code MapCodec}实例。
 * <p>
 * Q：如何解决Protobuf的消息编解码问题？
 * A：为使用到的消息分配{@link TypeMeta}，并利用Utils类提供的方法创建对应的{@code MessageCodec}。
 * <p>
 * Q: factory的作用？
 * A: factory用于支持多态，或将数据读取到既有对象。不过，如果使用了特殊的缓存实现，需考虑线程安全问题。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface Converter {

    // region read/write

    /**
     * 将一个对象写入源
     * 注意：如果对象的运行时类型和声明类型一致，则可省去编码结果中的类型信息。
     *
     * @param typeInfo 对象声明类型信息
     */

    byte[] write(Object value, TypeInfo<?> typeInfo);

    /**
     * 从数据源中读取一个对象
     * 注意：如果对象的声明类型和写入的类型不兼容，则表示投影；factory用于支持将数据读取到既有实例或子类实例上。
     *
     * @param source   数据源
     * @param typeInfo 对象声明类型信息
     * @param factory  实例工厂
     */
    <T> T read(byte[] source, TypeInfo<T> typeInfo, @Nullable Supplier<? extends T> factory);

    /**
     * @param value    要写入的对象
     * @param typeInfo 对象声明类型信息
     * @param chunk    二进制块，写入的字节数设置到{@link DsonChunk}
     */
    void write(Object value, TypeInfo<?> typeInfo, DsonChunk chunk);

    /**
     * @param chunk    二进制块，读取的字节数设置到{@link DsonChunk}
     * @param typeInfo 对象声明类型信息
     * @param factory  实例工厂
     * @return 解码结果，顶层对象不应该是null
     */
    <T> T read(DsonChunk chunk, TypeInfo<T> typeInfo, @Nullable Supplier<? extends T> factory);

    default <T> T read(byte[] source, TypeInfo<T> typeInfo) {
        return read(new DsonChunk(source), typeInfo, null);
    }

    default <T> T read(DsonChunk chunk, TypeInfo<T> typeInfo) {
        return read(chunk, typeInfo, null);
    }

    // endregion

    // region 快捷方法

    default byte[] write(Object value) {
        return write(value, TypeInfo.OBJECT); // 默认写入对象类型
    }

    default Object read(byte[] source) {
        return read(source, TypeInfo.OBJECT);
    }

    /**
     * @param typeInfo 对象声明类型信息
     * @param buffer   编码输出
     * @return 写入的字节数
     */
    default int write(Object value, TypeInfo<?> typeInfo, byte[] buffer) {
        DsonChunk chunk = new DsonChunk(buffer);
        write(value, typeInfo, chunk);
        return chunk.getUsed();
    }

    /**
     * 克隆一个对象。
     * 注意：
     * 1.返回值的类型不一定和原始对象相同，这通常发生在集合对象上。
     * 2.如果Codec存在lazyDecode，也会导致不同
     *
     * @param typeInfo 用于确定返回结果类型
     * @param factory  实例工厂
     */
    default <T> T cloneObject(Object value, TypeInfo<T> typeInfo, @Nullable Supplier<? extends T> factory) {
        if (value == null) {
            return null;
        }
        // 克隆属于立即使用数据，可以不写入顶层的类型信息
        final byte[] data = write(value, TypeInfo.of(value.getClass()));
        return read(data, typeInfo, factory);
    }

    default <T> T cloneObject(Object value, TypeInfo<T> typeInfo) {
        return cloneObject(value, typeInfo, null);
    }

    // endregion
}