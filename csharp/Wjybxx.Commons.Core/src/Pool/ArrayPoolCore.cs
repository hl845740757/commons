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

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 数组对象池工具类
/// </summary>
internal static class ArrayPoolCore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="defCapacity">默认空间</param>
    /// <param name="maxCapacity">最大空间</param>
    /// <param name="growFactor">数组空间成长系数</param>
    /// <returns>空间信息</returns>
    public static int[] CalArrayCapacities(int defCapacity, int maxCapacity, double growFactor) {
        if (defCapacity < 0 || maxCapacity < defCapacity) {
            throw new ArgumentException($"defCapacity: {defCapacity}, maxCapacity: {maxCapacity}");
        }
        growFactor = Math.Max(1.25d, growFactor); // 数组长度不可以变小

        List<int> capacityList = new List<int>();
        capacityList.Add(defCapacity);

        long capacity = defCapacity;
        while (capacity < maxCapacity) {
            capacity = (long)Math.Min(maxCapacity, capacity * growFactor);
            capacityList.Add((int)capacity);
        }
        return capacityList.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bucketCount">块数量</param>
    /// <param name="firstBucketLength">第一个块大小</param>
    /// <param name="growFactor">成长系数</param>
    /// <returns>每个bucket的长度</returns>
    /// <exception cref="ArgumentException"></exception>
    public static int[] CalArrayCacheCounts(int bucketCount, int firstBucketLength, double growFactor) {
        if (firstBucketLength < 0) throw new ArgumentException("firstBucketLength");
        if (growFactor < 0) throw new ArgumentException("growFactor");
        // bucket的长度可以变小
        int[] result = new int[bucketCount];
        result[0] = firstBucketLength;
        for (int idx = 1; idx < bucketCount; idx++) {
            long expectedLength = (long)(result[idx - 1] * growFactor);
            result[idx] = MathCommon.Clamp(expectedLength, 0, MathCommon.MaxPowerOfTwo);
        }
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bucketInfo">配置信息</param>
    /// <param name="arrayCapacities">数组空间信息</param>
    /// <param name="cacheCounts">数组缓存数量信息</param>
    /// <exception cref="ArgumentException"></exception>
    public static void InitArrayCapacityAndBucketLengths(List<ArrayBucketConfig> bucketInfo,
                                                         int[] arrayCapacities, int[] cacheCounts) {
        if (bucketInfo.Count != arrayCapacities.Length || bucketInfo.Count != cacheCounts.Length) {
            throw new ArgumentException();
        }
        for (var i = 0; i < bucketInfo.Count; i++) {
            ArrayBucketConfig bucketConfig = bucketInfo[i];
            int arrayCapacity = bucketConfig.arrayCapacity;
            int cacheCount = bucketConfig.cacheCount;
            arrayCapacities[i] = arrayCapacity;
            cacheCounts[i] = cacheCount;
            // 数组空间不可以缩小
            if (i > 0 && arrayCapacity <= arrayCapacities[i - 1]) {
                throw new ArgumentException("bucketInfo: " + bucketInfo);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacityArray">空间信息数组</param>
    /// <param name="arrayLength">期望的数组大小</param>
    /// <returns></returns>
    public static int BucketIndexOfArray(int[] capacityArray, int arrayLength) {
        int index = ArrayUtil.BinarySearch(capacityArray, arrayLength);
        if (index < 0) {
            // 不匹配的情况下再校验合法性
            if (arrayLength > capacityArray[capacityArray.Length - 1]) {
                return -1;
            }
            index = (index + 1) * -1;
        }
        return index;
    }
}
}