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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons
{
/// <summary>
/// 数学基础库
/// </summary>
public static class MathCommon
{
    /// <summary>
    /// 解决dotnet5缺少全局共享变量的问题
    /// </summary>
    public static readonly Random SharedRandom = new Random();

    /** 测试给定的参数是否是【偶数】 */
    public static bool IsEven(int x) {
        return (x & 1) == 0;
    }

    /** 测试给定的参数是否是【奇数】 */
    public static bool IsOdd(int x) {
        return (x & 1) == 1;
    }

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(int num) {
        if (num > MaxPowerOfTwo) throw new ArgumentException();
        if (num < 1) return 1;
        return 1 << (32 - NumberOfLeadingZeros(num - 1));
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NextPowerOfTwo(long num) {
        if (num > LongMaxPowerOfTwo) throw new ArgumentException();
        if (num < 1) return 1;
        return 1L << (64 - NumberOfLeadingZeros(num - 1));
    }

    #endregion

    #region bitcount

    private const int INT_M1 = 0x5555_5555;
    private const int INT_M2 = 0x3333_3333;
    private const int INT_M4 = 0x0f0f_0f0f;
    private const int INT_M8 = 0x00ff_00ff;
    private const int INT_M16 = 0x0000_ffff;

    /** 计算int32值中1的数量 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount(int n0) {
        // c#不支持逻辑右移，我们先去掉高位1
        int n = n0 & int.MaxValue;
        n = (n & INT_M1) + ((n >> 1) & INT_M1);
        n = (n & INT_M2) + ((n >> 2) & INT_M2);
        n = (n & INT_M4) + ((n >> 4) & INT_M4);
        n = (n & INT_M8) + ((n >> 8) & INT_M8);
        n = (n & INT_M16) + ((n >> 16) & INT_M16);
        return n0 < 0 ? n + 1 : n;
    }

    /** 计算uint32值中1的数量 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount(uint n) {
        return BitCount((int)n);
    }

    private const long LONG_M1 = 0x5555_5555_5555_5555;
    private const long LONG_M2 = 0x3333_3333_3333_3333;
    private const long LONG_M4 = 0x0f0f_0f0f_0f0f_0f0f;
    private const long LONG_M8 = 0x00ff_00ff_00ff_00ff;
    private const long LONG_M16 = 0x0000_ffff_0000_ffff;
    private const long LONG_M32 = 0x0000_0000_ffff_ffff;

    /** 计算int64值中1的数量 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount(long n0) {
        // c#不支持逻辑右移，我们先去掉高位1
        long n = n0 & long.MaxValue;
        n = (n & LONG_M1) + ((n >> 1) & LONG_M1);
        n = (n & LONG_M2) + ((n >> 2) & LONG_M2);
        n = (n & LONG_M4) + ((n >> 4) & LONG_M4);
        n = (n & LONG_M8) + ((n >> 8) & LONG_M8);
        n = (n & LONG_M16) + ((n >> 16) & LONG_M16);
        n = (n & LONG_M32) + ((n >> 32) & LONG_M32);
        return n0 < 0 ? (int)n + 1 : (int)n;
    }

    /** 计算uint64值中1的数量 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount(ulong n) {
        return BitCount((long)n);
    }

    /** 计算int值中1的数量 -- 适用于多数位为0的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCountFast(int n0) {
        int n = n0 & int.MaxValue; // 清除最高位1，使得下方的n-1安全
        int c = 0;
        while (n != 0) {
            n &= (n - 1); // 清除最低位的1
            c++;
        }
        return n0 < 0 ? c + 1 : c;
    }

    /** 计算int值中1的数量 -- 适用于多数位为0的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCountFast(uint n) {
        return BitCountFast((int)n);
    }

    /** 计算int值中1的数量 -- 适用于多数位为0的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCountFast(long n0) {
        long n = n0 & long.MaxValue; // 清除最高位1，使得下方的n-1安全
        int c = 0;
        while (n != 0) {
            n &= (n - 1); // 清除最低位的1
            c++;
        }
        return n0 < 0 ? c + 1 : c;
    }

    /** 计算int值中1的数量 -- 适用于多数位为0的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCountFast(ulong n) {
        return BitCountFast((long)n);
    }

    #endregion

    #region bitZeros

    /// <summary>
    /// 计算int值的前导0数量
    /// (高位0)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfLeadingZeros(int x) {
        if (x <= 0) return x == 0 ? 32 : 0;
        // 下面x为正数，算术右移等同逻辑右移
        int c = 31;
        if (x >= (1 << 16)) {
            c -= 16;
            x >>= 16;
        }
        if (x >= (1 << 8)) {
            c -= 8;
            x >>= 8;
        }
        if (x >= (1 << 4)) {
            c -= 4;
            x >>= 4;
        }
        if (x >= (1 << 2)) {
            c -= 2;
            x >>= 2;
        }
        return c - (x >> 1);
    }

    /// <summary>
    /// 计算int值的后导0数量
    /// (低位0)
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfTrailingZeros(int x) {
        x = ~x & (x - 1);
        if (x <= 0) return x & 32;
        // 下面x为正数，算术右移等同逻辑右移
        int c = 1;
        if (x > (1 << 16)) {
            c += 16;
            x >>= 16;
        }
        if (x > (1 << 8)) {
            c += 8;
            x >>= 8;
        }
        if (x > (1 << 4)) {
            c += 4;
            x >>= 4;
        }
        if (x > (1 << 2)) {
            c += 2;
            x >>= 2;
        }
        return c + (x >> 1);
    }

    /// <summary>
    /// 计算long值的前导0数量
    /// (高位0)
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfLeadingZeros(long i) {
        int x = (int)LogicalShiftRight(i, 32); // 测试高32位是否为0
        return x == 0
            ? 32 + NumberOfLeadingZeros((int)i)
            : NumberOfLeadingZeros(x);
    }

    /// <summary>
    /// 计算long值的后导0数量
    /// (低位0)
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfTrailingZeros(long i) {
        int x = (int)i; // 测试低32位是否为0
        return x == 0
            ? 32 + NumberOfTrailingZeros((int)LogicalShiftRight(i, 32))
            : NumberOfTrailingZeros(x);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LogicalShiftRight(int val, int offset) {
        if (offset < 0) {
            offset = 32 - Math.Abs(offset) & 31;
        } else {
            offset &= 31;
        }
        if (offset == 0) return val;
        int mask = int.MaxValue >> (offset - 1); // 高n位为0
        return (val >> offset) & mask;
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
        if (offset < 0) {
            offset = 64 - Math.Abs(offset) & 63;
        } else {
            offset &= 63;
        }
        if (offset == 0) return val;
        long mask = long.MaxValue >> (offset - 1); // 高n位为0
        return (val >> offset) & mask;
    }

    #endregion

    #region min/max

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float x, float y) {
        return (double)x < y ? x : y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(float x, float y) {
        return (double)x > y ? x : y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Min(int a, int b, int c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Max(int a, int b, int c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Min(long a, long b, long c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Max(long a, long b, long c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(float a, float b, float c) {
        if ((double)a > b) a = b;
        if ((double)a > c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(float a, float b, float c) {
        if ((double)a < b) a = b;
        if ((double)a < c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Min(double a, double b, double c) {
        if (a > b) a = b;
        if (a > c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Max(double a, double b, double c) {
        if (a < b) a = b;
        if (a < c) a = c;
        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MinMax(int a, int b, out int min, out int max) {
        if (a < b) {
            min = a;
            max = b;
        } else {
            min = b;
            max = a;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MinMax(long a, long b, out long min, out long max) {
        if (a < b) {
            min = a;
            max = b;
        } else {
            min = b;
            max = a;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MinMax(float a, float b, out float min, out float max) {
        if ((double)a < b) {
            min = a;
            max = b;
        } else {
            min = b;
            max = a;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MinMax(double a, double b, out double min, out double max) {
        if (a < b) {
            min = a;
            max = b;
        } else {
            min = b;
            max = a;
        }
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

    /** 求两个int的和，溢出时clamp到int范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SumAndClamp(int value, int delta) {
        long r = (long)value + delta;
        return Clamp(r, int.MinValue, int.MaxValue);
    }

    /** 求两个int的和，同时clamp到给定的范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SumAndClamp(int value, int delta, int min, int max) {
        long r = (long)value + delta;
        return Clamp(r, min, max);
    }

    /** 求两个int的乘积，溢出时clamp到int范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MultiplyAndClamp(int value, int delta) {
        long r = (long)value * delta;
        return Clamp(r, int.MinValue, int.MaxValue);
    }

    /** 求两个int的乘积，同时clamp到给定的范围 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MultiplyAndClamp(int value, int delta, int min, int max) {
        long r = (long)value * delta;
        return Clamp(r, min, max);
    }

    #endregion
}
}