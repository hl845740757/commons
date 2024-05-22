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
import javax.annotation.concurrent.NotThreadSafe;
import java.lang.reflect.Array;
import java.util.TreeSet;
import java.util.function.Consumer;

/**
 * 简单数组池实现
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@NotThreadSafe
public final class SimpleArrayPool<T> implements ArrayPool<T> {

    private final Class<T> arrayType;
    private final int defCapacity;
    private final int maxCapacity;
    private final boolean clear;
    private final int poolSize;

    private final TreeSet<Node<T>> freeArrays;
    private final Consumer<T> clearHandler;
    private long sequence = 1;

    public SimpleArrayPool(ArrayPoolBuilder.SimpleArrayPoolBuilder<T> builder) {
        this(builder.getArrayType(), builder.getPoolSize(),
                builder.getDefCapacity(), builder.getMaxCapacity(),
                builder.isClear());
    }

    /**
     * @param arrayType   数组类型
     * @param poolSize    池大小
     * @param defCapacity 默认数组大小
     * @param maxCapacity 数组最大大小 -- 超过大小的数组不会放入池中
     */
    public SimpleArrayPool(Class<T> arrayType, int poolSize, int defCapacity, int maxCapacity) {
        this(arrayType, poolSize, defCapacity, maxCapacity, false);
    }

    /**
     * @param arrayType   数组类型
     * @param poolSize    池大小
     * @param defCapacity 默认数组大小
     * @param maxCapacity 数组最大大小 -- 超过大小的数组不会放入池中
     * @param clear       数组归还到池时是否清理
     */
    public SimpleArrayPool(Class<T> arrayType, int poolSize, int defCapacity, int maxCapacity, boolean clear) {
        if (arrayType.getComponentType() == null) {
            throw new IllegalArgumentException("arrayType");
        }
        if (defCapacity <= 0 || maxCapacity < 0 || poolSize < 0) {
            throw new IllegalArgumentException();
        }
        this.arrayType = arrayType;
        this.poolSize = poolSize;
        this.defCapacity = defCapacity;
        this.maxCapacity = maxCapacity;
        this.clear = clear;

        this.clearHandler = ArrayPoolCore.findClearHandler(arrayType);
        this.freeArrays = new TreeSet<>(ArrayPoolCore.COMPARATOR);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public T acquire() {
        Node<T> minNode = freeArrays.pollFirst();
        if (minNode != null) {
            return minNode.array();
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
        Node<T> ceilingNode = freeArrays.ceiling(new LengthNode<>(minimumLength));
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
        if (freeArrays.size() < poolSize && length <= maxCapacity) {
            if (clear) {
                clearHandler.accept(array);
            }
            freeArrays.add(new ArrayNode<>(array, length, sequence++));
        }
    }

    @Override
    public void clear() {
        freeArrays.clear();
    }

}