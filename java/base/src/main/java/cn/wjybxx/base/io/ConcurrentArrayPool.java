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

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.Array;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;
import java.util.function.Consumer;

/**
 * 高性能的并发数组池实现
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentArrayPool<T> implements ArrayPool<T> {

    /** 全局共享字节数组池 */
    public static final ConcurrentArrayPool<byte[]> SHARED_BYTE_ARRAY_POOL = newBuilder(byte[].class)
            .setDefCapacity(4096)
            .setMaxCapacity(512 * 1024)
            .setClear(false)
            .setArraysPerBucket(50)
            .build();

    /** 全局共享char数组池 -- charArray的使用频率稍低 */
    public static final ConcurrentArrayPool<char[]> SHARED_CHAR_ARRAY_POOL = newBuilder(char[].class)
            .setDefCapacity(1024)
            .setMaxCapacity(64 * 1024)
            .setClear(false)
            .setArraysPerBucket(50)
            .build();

    private final Class<T> arrayType;
    private final int defCapacity;
    private final int maxCapacity;
    private final boolean clear;
    private final Consumer<T> clearHandler;
    private final int lookAhead;

    private final int[] capacityArray; // 用于快速二分查找，避免查询buckets
    private final MpmcArrayQueue<T>[] buckets;

    @SuppressWarnings("unchecked")
    public ConcurrentArrayPool(Builder<T> builder) {
        Class<T> arrayType = builder.getArrayType();
        if (!arrayType.isArray()) {
            throw new IllegalArgumentException("arrayType");
        }
        if (builder.getArraysPerBucket() < 0 || builder.getDefCapacity() <= 0
                || builder.getMaxCapacity() < builder.getDefCapacity()) {
            throw new IllegalArgumentException();
        }

        this.arrayType = arrayType;
        this.defCapacity = builder.getDefCapacity();
        this.maxCapacity = builder.getMaxCapacity();
        this.clear = builder.isClear();
        this.clearHandler = ArrayPoolCore.findClearHandler(arrayType);
        this.lookAhead = Math.max(0, builder.getLookAhead());

        // 初始化chunk
        int arraysPerBucket = builder.getArraysPerBucket();
        this.capacityArray = calBucketInfo(builder.getGrowFactor());
        this.buckets = new MpmcArrayQueue[capacityArray.length];
        for (int i = 0; i < buckets.length; i++) {
            buckets[i] = new MpmcArrayQueue<>(arraysPerBucket);
        }
    }

    private int[] calBucketInfo(double growFactor) {
        growFactor = Math.max(0.25f, growFactor); // 避免成长过低

        List<Integer> capacityList = new ArrayList<>();
        capacityList.add(defCapacity);

        long capacity = defCapacity;
        while (capacity < maxCapacity) {
            capacity = (long) Math.min(maxCapacity, capacity * (1 + growFactor));
            capacityList.add((int) capacity);
        }
        return capacityList.stream().mapToInt(e -> e).toArray();
    }

    private int indexOfArray(int arrayLength) {
        if (arrayLength < defCapacity || arrayLength > maxCapacity) {
            return -1;
        }
        int index = Arrays.binarySearch(capacityArray, arrayLength);
        if (index < 0) {
            index = (index + 1) * -1;
        }
        return index;
    }

    @Nonnull
    @Override
    public T acquire() {
        return acquire(defCapacity, false);
    }

    @Override
    public T acquire(int minimumLength) {
        return acquire(minimumLength, false);
    }

    @SuppressWarnings("unchecked")
    @Override
    public T acquire(int minimumLength, boolean clear) {
        final int index = indexOfArray(minimumLength);
        if (index < 0) { // 不能被池化
            return (T) Array.newInstance(arrayType.getComponentType(), minimumLength);
        }
        // 先尝试从最佳池申请
        T array = buckets[index].poll();
        if (array != null) {
            return array;
        }
        // 尝试从更大的池申请 -- 最多跳3级，避免大规模遍历
        {
            int end = Math.min(buckets.length, index + lookAhead + 1);
            for (int nextIndex = index + 1; nextIndex < end; nextIndex++) {
                array = buckets[nextIndex].poll();
                if (array != null) {
                    return array;
                }
            }
        }
        // 分配新的
        array = (T) Array.newInstance(arrayType.getComponentType(), capacityArray[index]);
        return array;
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
        if (clear) {
            clearHandler.accept(array);
        }
        int length = Array.getLength(array);
        int index = indexOfArray(length);
        if (index < 0 || length != capacityArray[index]) { // 长度不匹配
            return;
        }
        buckets[index].offer(array);
    }

    @Override
    public void clear() {
        for (MpmcArrayQueue<T> bucket : buckets) {
            //noinspection StatementWithEmptyBody
            while (bucket.poll() != null) {

            }
        }
    }

    // region builder

    public static <T> Builder<T> newBuilder(Class<T> arrayType) {
        return new Builder<>(arrayType);
    }

    public static class Builder<T> {

        /** 数组类型 */
        private final Class<T> arrayType;
        /** 默认空间 */
        private int defCapacity = 4096;
        /** 最大空间 */
        private int maxCapacity = 64 * 1024;
        /** 数组在归还时是否清理数组内容 */
        private boolean clear;

        private int arraysPerBucket = 50;
        /** 数组成长空间大小，默认二倍 */
        private double growFactor = 1;
        /** 当前chunk没有合适空闲数组时，后向查找个数 -- 该值过大可能导致性能损耗，也可能导致返回过大的数组 */
        private int lookAhead = 1;

        private Builder(Class<T> arrayType) {
            this.arrayType = Objects.requireNonNull(arrayType, "arrayType");
        }

        public ConcurrentArrayPool<T> build() {
            return new ConcurrentArrayPool<>(this);
        }

        public Class<T> getArrayType() {
            return arrayType;
        }

        /** 默认分配的数组空间大小 */
        public int getDefCapacity() {
            return defCapacity;
        }

        public Builder<T> setDefCapacity(int defCapacity) {
            this.defCapacity = defCapacity;
            return this;
        }

        /** 可缓存的数组的最大空间 -- 超过大小的数组销毁 */
        public int getMaxCapacity() {
            return maxCapacity;
        }

        public Builder<T> setMaxCapacity(int maxCapacity) {
            this.maxCapacity = maxCapacity;
            return this;
        }

        /** 数组在归还时是否清理数组内容 */
        public boolean isClear() {
            return clear;
        }

        public Builder<T> setClear(boolean clear) {
            this.clear = clear;
            return this;
        }

        /** 每个bucket存储多少个数组 */
        public int getArraysPerBucket() {
            return arraysPerBucket;
        }

        public Builder<T> setArraysPerBucket(int arraysPerBucket) {
            this.arraysPerBucket = arraysPerBucket;
            return this;
        }

        /** 数组成长空间大小 */
        public double getGrowFactor() {
            return growFactor;
        }

        public Builder<T> setGrowFactor(double growFactor) {
            this.growFactor = growFactor;
            return this;
        }

        public int getLookAhead() {
            return lookAhead;
        }

        public Builder<T> setLookAhead(int lookAhead) {
            this.lookAhead = lookAhead;
            return this;
        }
    }
    // endregion
}