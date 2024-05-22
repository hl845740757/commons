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

package cn.wjybxx.base.io;

import cn.wjybxx.base.io.ArrayPoolCore.ArrayNode;
import cn.wjybxx.base.io.ArrayPoolCore.LengthNode;
import cn.wjybxx.base.io.ArrayPoolCore.Node;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.Array;
import java.util.Map;
import java.util.concurrent.ConcurrentNavigableMap;
import java.util.concurrent.ConcurrentSkipListMap;
import java.util.function.Consumer;

/**
 * 简单并发数组池实现
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentArrayPool<T> implements ArrayPool<T> {

    /** 全局共享字节数组池 */
    public static final ConcurrentArrayPool<byte[]> SHARED_BYTE_ARRAY_POOL = ArrayPoolBuilder.newConcurrentBuilder(byte[].class)
            .setDefCapacity(4096)
            .setMaxCapacity(1024 * 1024)
            .setClear(false)
            .build();
    /** 全局共享char数组池 */
    public static final ConcurrentArrayPool<char[]> SHARED_CHAR_ARRAY_POOL = ArrayPoolBuilder.newConcurrentBuilder(char[].class)
            .setDefCapacity(4096)
            .setMaxCapacity(1024 * 1024)
            .setClear(false)
            .build();

    private final Class<T> arrayType;
    private final int defCapacity;
    private final int maxCapacity;
    private final boolean clear;

    private final ConcurrentNavigableMap<Node<T>, Boolean> freeArrays;
    private final Consumer<T> clearHandler;

    public ConcurrentArrayPool(ArrayPoolBuilder.ConcurrentArrayPoolBuilder<T> builder) {
        Class<T> arrayType = builder.getArrayType();
        if (arrayType.getComponentType() == null) {
            throw new IllegalArgumentException("arrayType");
        }
        if (builder.getDefCapacity() <= 0 || builder.getMaxCapacity() <= 0) {
            throw new IllegalArgumentException();
        }

        this.arrayType = arrayType;
        this.defCapacity = builder.getDefCapacity();
        this.maxCapacity = builder.getMaxCapacity();
        this.clear = builder.isClear();

        this.clearHandler = ArrayPoolCore.findClearHandler(arrayType);
        this.freeArrays = new ConcurrentSkipListMap<>(ArrayPoolCore.COMPARATOR);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public T acquire() {
        Map.Entry<Node<T>, Boolean> firstEntry = freeArrays.pollFirstEntry();
        if (firstEntry != null) {
            return firstEntry.getKey().array();
        }
        return (T) Array.newInstance(arrayType.getComponentType(), defCapacity);
    }

    @Override
    public T acquire(int minimumLength) {
        return acquire(minimumLength, false);
    }

    @SuppressWarnings("unchecked")
    @Override
    public T acquire(int minimumLength, boolean clear) {
        Node<T> ceilingNode = freeArrays.ceilingKey(new LengthNode<>(minimumLength));
        if (ceilingNode != null) {
            freeArrays.remove(ceilingNode);

            T array = ceilingNode.array();
            if (!this.clear && clear) { // 默认不清理的情况下用户请求有效
                clearHandler.accept(array);
            }
            return array;
        }
        return (T) Array.newInstance(arrayType.getComponentType(), minimumLength);
    }

    @Override
    public void release(T array) {
        releaseImpl(array, this.clear);
    }

    @Override
    public void release(T array, boolean clear) {
        releaseImpl(array, this.clear || clear); // 默认不清理的情况下用户请求有效
    }

    private void releaseImpl(T array, boolean clear) {
        int length = Array.getLength(array);
        if (length <= maxCapacity) {
            if (clear) {
                clearHandler.accept(array);
            }
        }
        freeArrays.put(new ArrayNode<>(array, length), Boolean.TRUE);
    }

    @Override
    public void clear() {
        freeArrays.clear();
    }

}