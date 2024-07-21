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

import cn.wjybxx.base.SystemPropsUtils;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.Array;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.function.Consumer;

/**
 * 高性能的并发数组池实现
 * (未鉴定归属，可归还外部数组，适用简单场景)
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentArrayPool<T> implements ArrayPool<T> {

    /** 全局共享字节数组池 */
    public static final ConcurrentArrayPool<byte[]> SHARED_BYTE_ARRAY_POOL = newBuilder(byte[].class)
            .setClear(false)
            .setDefCapacity(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedByteArrayPool.DefCapacity", 4096))
            .setMaxCapacity(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedByteArrayPool.MaxCapacity", 512 * 1024))
            .setArrayGrowFactor(SystemPropsUtils.getDouble("Wjybxx.Commons.IO.SharedByteArrayPool.ArrayGrowFactor", 1.5))
            .setFirstBucketLength(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedByteArrayPool.FirstBucketLength", 50))
            .setBucketGrowFactor(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedByteArrayPool.BucketGrowFactor", 1))
            .build();

    /** 全局共享char数组池 -- charArray的使用频率稍低 */
    public static final ConcurrentArrayPool<char[]> SHARED_CHAR_ARRAY_POOL = newBuilder(char[].class)
            .setClear(false)
            .setDefCapacity(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedCharArrayPool.DefCapacity", 1024))
            .setMaxCapacity(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedCharArrayPool.MaxCapacity", 64 * 1024))
            .setArrayGrowFactor(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedCharArrayPool.ArrayGrowFactor", 64 * 1024))
            .setFirstBucketLength(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedCharArrayPool.FirstBucketLength", 50))
            .setBucketGrowFactor(SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedCharArrayPool.BucketGrowFactor", 1))
            .build();

    private final Class<T> arrayType;
    private final Consumer<T> clearHandler;
    private final boolean clear;
    private final int lookAhead;

    private final int[] capacities; // 用于快速二分查找，避免查询buckets
    private final MpmcArrayQueue<T>[] buckets;

    @SuppressWarnings("unchecked")
    public ConcurrentArrayPool(Builder<T> builder) {
        List<ArrayBucketConfig> bucketInfo = builder.getBucketInfo();
        int[] arrayCapacities;
        int[] arrayCacheCounts;
        if (bucketInfo.size() > 0) {
            arrayCapacities = new int[bucketInfo.size()];
            arrayCacheCounts = new int[bucketInfo.size()];
            ArrayPoolCore.initArrayCapacityAndCacheCounts(bucketInfo, arrayCapacities, arrayCacheCounts);
        } else {
            arrayCapacities = ArrayPoolCore.calArrayCapacities(builder.getDefCapacity(), builder.getMaxCapacity(), builder.getArrayGrowFactor());
            arrayCacheCounts = ArrayPoolCore.calArrayCacheCounts(arrayCapacities.length, builder.getFirstBucketLength(), builder.getBucketGrowFactor());
        }
        this.arrayType = builder.getArrayType();
        this.clearHandler = ArrayPoolCore.findClearHandler(builder.getArrayType());
        this.clear = builder.isClear();
        this.lookAhead = Math.max(0, builder.getLookAhead());

        // 初始化chunk
        this.capacities = arrayCapacities;
        this.buckets = new MpmcArrayQueue[arrayCapacities.length];
        for (int i = 0; i < buckets.length; i++) {
            buckets[i] = new MpmcArrayQueue<>(arrayCacheCounts[i]);
        }
    }

    @Nonnull
    @Override
    public T acquire() {
        return acquire(capacities[0], false);
    }

    @Override
    public T acquire(int minimumLength) {
        return acquire(minimumLength, false);
    }

    @SuppressWarnings("unchecked")
    @Override
    public T acquire(int minimumLength, boolean clear) {
        final int index = ArrayPoolCore.bucketIndexOfArray(capacities, minimumLength);
        if (index < 0) { // 不能被池化
            return (T) Array.newInstance(arrayType.getComponentType(), minimumLength);
        }
        // 先尝试从最佳池申请
        T array = buckets[index].poll();
        if (array != null) {
            if (clear && !this.clear) {
                clearHandler.accept(array);
            }
            return array;
        }
        // 尝试从更大的池申请 -- 最多跳3级，避免大规模遍历
        {
            int end = Math.min(buckets.length, index + lookAhead + 1);
            for (int nextIndex = index + 1; nextIndex < end; nextIndex++) {
                array = buckets[nextIndex].poll();
                if (array != null) {
                    if (clear && !this.clear) {
                        clearHandler.accept(array);
                    }
                    return array;
                }
            }
        }
        // 分配新的
        array = (T) Array.newInstance(arrayType.getComponentType(), capacities[index]);
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
        int length = Array.getLength(array);
        int index = ArrayPoolCore.bucketIndexOfArray(capacities, length);
        if (index < 0 || length != capacities[index]) { // 长度不匹配
            return;
        }
        if (clear) {
            clearHandler.accept(array);
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
        /** 数组在归还时是否清理数组内容 */
        private boolean clear;
        /** 当前chunk没有合适空闲数组时，后向查找个数 -- 该值过大可能导致性能损耗，也可能导致返回过大的数组 */
        private int lookAhead = 1;

        /** 默认空间 */
        private int defCapacity = 4096;
        /** 最大空间 */
        private int maxCapacity = 64 * 1024;
        /** 数组空间成长系数，默认二倍 */
        private double arrayGrowFactor = 2;

        /** 第一个桶的大小 */
        private int firstBucketLength = 50;
        /** chunk空间成长系数，可以小于1，表示缓存数量逐渐减少 */
        private double bucketGrowFactor = 1;
        /**
         * bucket信息，用于手动指定每一个bucket的信息。
         * count表示bucket数量，key为bucket对应的数组空间大小，value为bucket的大小。
         * 注意：使用该方式配置以后，其它设置数组和bucket的参数将无效
         */
        private final List<ArrayBucketConfig> bucketInfo = new ArrayList<>();

        private Builder(Class<T> arrayType) {
            this.arrayType = Objects.requireNonNull(arrayType, "arrayType");
            this.clear = ArrayPoolCore.isRefArray(arrayType);
        }

        public ConcurrentArrayPool<T> build() {
            return new ConcurrentArrayPool<>(this);
        }

        /**
         * @param arrayCapacity bucket中的数组大小
         * @param cacheCount    bucket缓存的数组个数
         * @return this
         */
        public Builder<T> addBucket(int arrayCapacity, int cacheCount) {
            this.bucketInfo.add(new ArrayBucketConfig(arrayCapacity, cacheCount));
            return this;
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
        public int getFirstBucketLength() {
            return firstBucketLength;
        }

        public Builder<T> setFirstBucketLength(int firstBucketLength) {
            this.firstBucketLength = firstBucketLength;
            return this;
        }

        /** 数组成长系数 */
        public double getArrayGrowFactor() {
            return arrayGrowFactor;
        }

        public Builder<T> setArrayGrowFactor(double arrayGrowFactor) {
            this.arrayGrowFactor = arrayGrowFactor;
            return this;
        }

        /** 桶大小成长系数 */
        public double getBucketGrowFactor() {
            return bucketGrowFactor;
        }

        public Builder<T> setBucketGrowFactor(double bucketGrowFactor) {
            this.bucketGrowFactor = bucketGrowFactor;
            return this;
        }

        public int getLookAhead() {
            return lookAhead;
        }

        public Builder<T> setLookAhead(int lookAhead) {
            this.lookAhead = lookAhead;
            return this;
        }

        public List<ArrayBucketConfig> getBucketInfo() {
            return bucketInfo;
        }

    }
    // endregion
}