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

package cn.wjybxx.base.pool;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * 高性能的并发数组池实现
 * (未鉴定归属，可归还外部数组，适用简单场景)
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentArrayLikePool<T> implements ArrayLikePool<T> {

    private final PoolableArrayHandler<T> handler;
    private final int lookAhead;

    private final int[] capacities; // 用于快速二分查找，避免查询buckets
    private final MpmcObjectBucket<T>[] buckets;

    @SuppressWarnings("unchecked")
    public ConcurrentArrayLikePool(Builder<T> builder) {
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
        this.handler = builder.getHandler();
        this.lookAhead = Math.max(0, builder.getLookAhead());

        // 初始化chunk
        this.capacities = arrayCapacities;
        this.buckets = new MpmcObjectBucket[arrayCapacities.length];
        for (int i = 0; i < buckets.length; i++) {
            buckets[i] = new MpmcObjectBucket<>(arrayCacheCounts[i]);
        }
    }

    @Nonnull
    @Override
    public T acquire() {
        return acquire(capacities[0]);
    }

    @Override
    public T acquire(int minimumLength) {
        final int index = ArrayPoolCore.bucketIndexOfArray(capacities, minimumLength);
        if (index < 0) { // 不能被池化
            return handler.create(this, minimumLength);
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
        array = handler.create(this, capacities[index]);
        return array;
    }

    @Override
    public void release(T array) {
        int length = handler.getCapacity(array);
        final int index = ArrayPoolCore.bucketIndexOfArray(capacities, length);
        if (index < 0 || length != capacities[index] || !handler.validate(array)) { // 长度不匹配
            handler.destroy(array); // 也可能是池创建的对象
            return;
        }
        handler.reset(array);
        if (!buckets[index].offer(array)) {
            handler.destroy(array);
        }
    }

    @Override
    public void clear() {
        for (MpmcObjectBucket<T> bucket : buckets) {
            T array;
            while ((array = bucket.poll()) != null) {
                handler.destroy(array);
            }
        }
    }

    // region builder

    public static <T> Builder<T> newBuilder(PoolableArrayHandler<T> handler) {
        return new Builder<>(handler);
    }

    public static class Builder<T> {

        /** 处理器 */
        private final PoolableArrayHandler<T> handler;
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

        public Builder(PoolableArrayHandler<T> handler) {
            this.handler = Objects.requireNonNull(handler, "handler");
        }

        public ConcurrentArrayLikePool<T> build() {
            return new ConcurrentArrayLikePool<>(this);
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

        public PoolableArrayHandler<T> getHandler() {
            return handler;
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