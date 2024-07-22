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
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 高性能的并发对象池实现
/// (未鉴定归属，可归还外部对象，适用简单场景)
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ConcurrentArrayLikePool<T> : IArrayLikePool<T>
{
    private readonly IPoolableArrayHandler<T> _handler;
    private readonly int _lookAhead;

    private readonly int[] _capacities; // 用于快速二分查找，避免查询buckets
    private readonly MpmcObjectBucket<T>[] _buckets;

    public ConcurrentArrayLikePool(Builder builder) {
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
        this._handler = builder.Handler;
        this._lookAhead = Math.Max(0, builder.LookAhead);

        // 初始化chunk
        this._capacities = arrayCapacities;
        this._buckets = new MpmcObjectBucket<T>[arrayCapacities.Length];
        for (int i = 0; i < _buckets.Length; i++) {
            _buckets[i] = new MpmcObjectBucket<T>(arrayCacheCounts[i]);
        }
    }

    public T Acquire() {
        return Acquire(_capacities[0]);
    }

    public T Acquire(int minimumLength) {
        int index = ArrayPoolCore.BucketIndexOfArray(_capacities, minimumLength);
        if (index < 0) { // 不能被池化
            return _handler.Create(this, minimumLength);
        }
        // 先尝试从最佳池申请
        if (_buckets[index].Poll(out T array)) {
            return array;
        }
        // 尝试从更大的池申请 -- 最多跳3级，避免大规模遍历
        {
            int end = Math.Min(_buckets.Length, index + _lookAhead + 1);
            for (int nextIndex = index + 1; nextIndex < end; nextIndex++) {
                if (_buckets[nextIndex].Poll(out array)) {
                    return array;
                }
            }
        }
        // 分配新的
        array = _handler.Create(this, _capacities[index]);
        return array;
    }

    public void Release(T array) {
        int length = _handler.GetCapacity(array);
        int index = ArrayPoolCore.BucketIndexOfArray(_capacities, length);
        if (index < 0 || length != _capacities[index] || !_handler.Validate(array)) { // 长度不匹配
            _handler.Destroy(array); // 也可能是池创建的对象
            return;
        }
        _handler.Reset(array);
        if (!_buckets[index].Offer(array)) {
            _handler.Destroy(array);
        }
    }

    public void Clear() {
        foreach (MpmcObjectBucket<T> bucket in _buckets) {
            while (bucket.Poll(out T array)) {
                _handler.Destroy(array);
            }
        }
    }

    #region builder

    public class Builder
    {
        /** 处理器 */
        private readonly IPoolableArrayHandler<T> handler;
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

        public Builder(IPoolableArrayHandler<T> handler) {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public ConcurrentArrayLikePool<T> Build() {
            return new ConcurrentArrayLikePool<T>(this);
        }

        public IPoolableArrayHandler<T> Handler => handler;

        public int DefCapacity {
            get => defCapacity;
            set => defCapacity = value;
        }

        public int MaxCapacity {
            get => maxCapacity;
            set => maxCapacity = value;
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