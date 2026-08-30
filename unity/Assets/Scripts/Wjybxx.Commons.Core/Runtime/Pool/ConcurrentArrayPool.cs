#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 高性能的并发数组池实现
/// (未鉴定归属，可归还外部数组，适用简单场景)
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ConcurrentArrayPool<T> : IArrayPool<T>
{
    private readonly bool _clear;
    private readonly int _lookAhead;

    private readonly int[] _capacities; // 用于快速二分查找，避免查询buckets
    private readonly MpmcObjectBucket<T[]>[] _buckets;

    public ConcurrentArrayPool(Builder builder) {
        List<ArrayBucketConfig> bucketInfo = builder.BucketInfo;
        int[] arrayCapacities;
        int[] arrayCacheCounts;
        if (bucketInfo.Count > 0) {
            arrayCapacities = new int[bucketInfo.Count];
            arrayCacheCounts = new int[bucketInfo.Count];
            ArrayPoolCore.InitArrayCapacityAndBucketLengths(bucketInfo, arrayCapacities, arrayCacheCounts);
        } else {
            arrayCapacities = ArrayPoolCore.CalArrayCapacities(builder.DefCapacity, builder.MaxCapacity, builder.ArrayGrowFactor);
            arrayCacheCounts = ArrayPoolCore.CalArrayCacheCounts(arrayCapacities.Length, builder.FirstBucketLength, builder.BucketGrowFactor);
        }
        this._clear = builder.Clear;
        this._lookAhead = Math.Max(0, builder.LookAhead);

        // 初始化chunk
        this._capacities = arrayCapacities;
        this._buckets = new MpmcObjectBucket<T[]>[arrayCapacities.Length];
        for (int i = 0; i < _buckets.Length; i++) {
            _buckets[i] = new MpmcObjectBucket<T[]>(arrayCacheCounts[i]);
        }
    }

    public T[] Acquire() {
        return Acquire(_capacities[0]);
    }

    public T[] Acquire(int minimumLength, bool clear = false) {
        int index = ArrayPoolCore.BucketIndexOfArray(_capacities, minimumLength);
        if (index < 0) { // 不能被池化
            return new T[minimumLength];
        }
        // 先尝试从最佳池申请
        if (_buckets[index].Poll(out T[] array)) {
            if (clear && !this._clear) {
                Array.Clear(array, 0, array.Length);
            }
            return array;
        }
        // 尝试从更大的池申请 -- 最多跳3级，避免大规模遍历
        {
            int end = Math.Min(_buckets.Length, index + _lookAhead + 1);
            for (int nextIndex = index + 1; nextIndex < end; nextIndex++) {
                if (_buckets[nextIndex].Poll(out array)) {
                    if (clear && !this._clear) {
                        Array.Clear(array, 0, array.Length);
                    }
                    return array;
                }
            }
        }
        // 分配新的
        array = new T[_capacities[index]];
        return array;
    }

    public void Release(T[] array) {
        ReleaseImpl(array, this._clear);
    }

    public void Release(T[] array, bool clear) {
        ReleaseImpl(array, this._clear | clear);
    }

    private void ReleaseImpl(T[] array, bool clear) {
        int length = array.Length;
        int index = ArrayPoolCore.BucketIndexOfArray(_capacities, length);
        if (index < 0 || length != _capacities[index]) { // 长度不匹配
            return;
        }
        if (clear) {
            Array.Clear(array, 0, array.Length);
        }
        _buckets[index].Offer(array);
    }

    public void Clear() {
        foreach (MpmcObjectBucket<T[]> bucket in _buckets) {
            while (bucket.Poll(out T[] _)) {
            }
        }
    }

    #region builder

    public class Builder
    {
        /** 数组在归还时是否清理数组内容 */
        private bool clear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
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

        /// <summary>
        /// bucket信息，用于手动指定每一个bucket的信息。
        /// count表示bucket数量，key为bucket对应的数组空间大小，value为bucket的大小。
        /// 注意：使用该方式配置以后，其它设置数组和bucket的参数将无效
        /// </summary>
        private readonly List<ArrayBucketConfig> bucketInfo = new List<ArrayBucketConfig>();

        public Builder() {
        }

        public ConcurrentArrayPool<T> Build() {
            return new ConcurrentArrayPool<T>(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayCapacity">bucket中的数组大小</param>
        /// <param name="cacheCount">bucket缓存的数组个数</param>
        /// <returns></returns>
        public Builder AddBucket(int arrayCapacity, int cacheCount) {
            this.bucketInfo.Add(new ArrayBucketConfig(arrayCapacity, cacheCount));
            return this;
        }

        public int DefCapacity {
            get => defCapacity;
            set => defCapacity = value;
        }

        public int MaxCapacity {
            get => maxCapacity;
            set => maxCapacity = value;
        }

        public bool Clear {
            get => clear;
            set => clear = value;
        }

        public int FirstBucketLength {
            get => firstBucketLength;
            set => firstBucketLength = value;
        }

        public double BucketGrowFactor {
            get => bucketGrowFactor;
            set => bucketGrowFactor = value;
        }

        public double ArrayGrowFactor {
            get => arrayGrowFactor;
            set => arrayGrowFactor = value;
        }

        public int LookAhead {
            get => lookAhead;
            set => lookAhead = value;
        }

        public List<ArrayBucketConfig> BucketInfo => bucketInfo;
    }

    #endregion
}
}