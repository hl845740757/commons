﻿#region LICENSE

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
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
namespace Wjybxx.Commons;

/// <summary>
/// 数学基础库
/// </summary>
public static class MathCommon
{
    #region INT48

    /** 48位无符号整数的最大值 */
    public const long UInt48MaxValue = (1L << 48) - 1;
    /** 有符号48位整数的最大值(140737488355327L) */
    public const long Int48MaxValue = (1L << 47) - 1;
    /** 有符号48位整数的最小值(-140737488355328L) */
    public const long Int48MinValue = -Int48MaxValue - 1;

    /** 判断一个数是否是有效的uint48 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUInt48(long value) {
        return value >= 0 && value <= UInt48MaxValue;
    }

    /** 判断一个数是否是有效的int48 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInt48(long value) {
        return value >= Int48MinValue && value <= Int48MaxValue;
    }

    #endregion

    #region power2

    public const int MaxPowerOfTwo = 1 << 30;
    public const long LongMaxPowerOfTwo = 1L << 62;

    /** 判断一个数是否是2的整次幂 */
    public static bool IsPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    public static int NextPowerOfTwo(int num) {
        if (num < 1) {
            return 1;
        }
        // https://acius2.blogspot.com/2007/11/calculating-next-power-of-2.html
        // C#未提供获取前导0数量的接口，因此我们选用该算法
        // 先减1，兼容自身已经是2的整次幂的情况；然后通过移位使得后续bit全部为1，再加1即获得结果
        num--;
        num = (num >> 1) | num;
        num = (num >> 2) | num;
        num = (num >> 4) | num;
        num = (num >> 8) | num;
        num = (num >> 16) | num;
        return ++num;
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    public static long NextPowerOfTwo(long num) {
        if (num < 1) {
            return 1;
        }
        num--;
        num = (num >> 1) | num;
        num = (num >> 2) | num;
        num = (num >> 4) | num;
        num = (num >> 8) | num;
        num = (num >> 16) | num;
        num = (num >> 32) | num;
        return ++num;
    }

    #endregion

    #region shift

    // c#没有一开始就支持逻辑右移...C#11提供了逻辑右移，但目前.NET6是主流
    /// <summary>
    /// 逻辑右移
    /// </summary>
    /// <param name="val"></param>
    /// <param name="offset">偏移量</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">offset非法</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LogicalShiftRight(int val, int offset) {
        if (offset < 0) throw new ArgumentException("invalid offset " + offset);
        if (offset == 0) return val;
        uint uval = (uint)val;
        return (int)(uval >> offset);
    }

    /// <summary>
    /// 逻辑右移
    /// </summary>
    /// <param name="val"></param>
    /// <param name="offset">偏移量</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">offset非法</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LogicalShiftRight(long val, int offset) {
        if (offset < 0) throw new ArgumentException("invalid offset " + offset);
        if (offset == 0) return val;
        ulong uval = (ulong)val;
        return (long)(uval >> offset);
    }

    #endregion

    #region min/max

    public static int Min(int a, int b, int c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    public static int Max(int a, int b, int c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    public static long Min(long a, long b, long c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    public static long Max(long a, long b, long c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    public static float Min(float a, float b, float c) {
        float r = Math.Min(a, b);
        return Math.Min(r, c);
    }

    public static float Max(float a, float b, float c) {
        float r = Math.Max(a, b);
        return Math.Max(r, c);
    }

    public static double Min(double a, double b, double c) {
        double r = Math.Min(a, b);
        return Math.Min(r, c);
    }

    public static double Max(double a, double b, double c) {
        double r = Math.Max(a, b);
        return Math.Max(r, c);
    }

    #endregion

    #region clamp

    /** 将long值约束到int区间[min, max] */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(long value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return (int)value;
    }

    /** 将double值约束到float区间[min, max] */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(double value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return (float)value;
    }

    /** 将value约束到[0, 1]范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(float value) {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 1f;
        return value;
    }

    /** 将value约束到[0, 1]范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp01(double value) {
        if (value <= 0d) return 0d;
        if (value >= 1d) return 1d;
        return value;
    }

    #endregion
}