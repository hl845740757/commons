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

namespace Wjybxx.Commons
{
/// <summary>
/// 数组工具类
/// </summary>
public static class ArrayUtil
{
    // int[]和byte[]可提供额外支持

    #region equals/hashcode

    /// <summary>
    /// 比较两个数组的相等性 -- 比较所有元素
    /// </summary>
    /// <param name="objA"></param>
    /// <param name="objB"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool Equals<T>(T[]? objA, T[]? objB) {
        if (objA == objB) {
            return true;
        }
        if (objA == null || objB == null || objA.Length != objB.Length) {
            return false;
        }
#if NET6_0_OR_GREATER
        ReadOnlySpan<T> first = objA;
        ReadOnlySpan<T> second = objB;
        return first.SequenceEqual(second);
#else
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0, len = objA.Length; i < len; i++) {
            if (!comparer.Equals(objA[i], objB[i])) {
                return false;
            }
        }
        return true;
#endif
    }

    public static bool Equals(byte[]? objA, byte[]? objB) {
        if (objA == objB) {
            return true;
        }
        if (objA == null || objB == null || objA.Length != objB.Length) {
            return false;
        }
#if NET6_0_OR_GREATER
        ReadOnlySpan<byte> first = objA;
        ReadOnlySpan<byte> second = objB;
        return first.SequenceEqual(second);
#else
        for (int i = 0, len = objA.Length; i < len; i++) {
            if (objA[i] != objB[i]) {
                return false;
            }
        }
        return true;
#endif
    }

    public static bool Equals(int[]? objA, int[]? objB) {
        if (objA == objB) {
            return true;
        }
        if (objA == null || objB == null || objA.Length != objB.Length) {
            return false;
        }
#if NET6_0_OR_GREATER
        ReadOnlySpan<int> first = objA;
        ReadOnlySpan<int> second = objB;
        return first.SequenceEqual(second);
#else
        for (int i = 0, len = objA.Length; i < len; i++) {
            if (objA[i] != objB[i]) {
                return false;
            }
        }
        return true;
#endif
    }
    // HashCode

    public static int HashCode<T>(T?[]? array) {
        if (array == null) {
            return 0;
        }
        int r = 1;
        if (typeof(T).IsValueType) { // Nullable<T>也是安全的
            for (int i = 0; i < array.Length; i++) {
                T e = array[i];
                r = r * 31 + e!.GetHashCode();
            }
        } else {
            for (int i = 0; i < array.Length; i++) {
                T e = array[i];
                r = r * 31 + (e == null ? 0 : e.GetHashCode());
            }
        }
        return r;
    }

    public static int HashCode(byte[]? array) {
        if (array == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < array.Length; i++) {
            r = r * 31 + array[i];
        }
        return r;
    }

    public static int HashCode(int[]? array) {
        if (array == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < array.Length; i++) {
            r = r * 31 + array[i];
        }
        return r;
    }

    #endregion

#nullable disable

    #region indexRef

    /** 查询List中是否包含指定对象引用 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsRef<T>(T[] list, T element) where T : class {
        return IndexOfRef(list, element, 0, list.Length) >= 0;
    }

    /** 查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfRef<T>(T[] list, object element) where T : class {
        return IndexOfRef(list, element, 0, list.Length);
    }

    /** 反向查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfRef<T>(T[] list, object element) where T : class {
        return LastIndexOfRef(list, element, list.Length - 1, list.Length);
    }

    /// <summary>
    /// 查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="element">要查找的元素</param>
    /// <param name="start">开始下标</param>
    /// <param name="len">查询长度</param>
    /// <typeparam name="T"></typeparam>
    public static int IndexOfRef<T>(T[] list, object element, int start, int len) where T : class {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (len < 0) throw new ArgumentNullException(nameof(len));
        for (int i = start, end = start + len; i < end; i++) {
            if (element == list[i]) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="element">要查找的元素</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="len">查询长度</param>
    /// <typeparam name="T"></typeparam>
    public static int LastIndexOfRef<T>(T[] list, object element, int start, int len) where T : class {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (len < 0) throw new ArgumentNullException(nameof(len));
        for (int i = start, end = start - len; i > end; i--) {
            if (element == list[i]) {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region binary-search

    /// <summary>
    /// 如果元素存在，则返回元素对应的下标；
    /// 如果元素不存在，则返回(-(insertion point) - 1)
    /// 即： (index + 1) * -1 可得应当插入的下标。 
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch(int[] array, int value) {
        return ArraySortHelper.BinarySearch(array, 0, array.Length, value);
    }

    /// <summary>
    /// 二分搜索
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="value">要查找的元素</param>
    /// <param name="fromIndex">包含</param>
    /// <param name="toIndex">不包含</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch(int[] array, int value, int fromIndex, int toIndex) {
        RangeCheck(array.Length, fromIndex, toIndex);
        return ArraySortHelper.BinarySearch(array, fromIndex, toIndex, value);
    }

    /// <summary>
    /// 如果元素存在，则返回元素对应的下标；
    /// 如果元素不存在，则返回(-(insertion point) - 1)
    /// 即： (index + 1) * -1 可得应当插入的下标。 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(T[] array, T value, IComparer<T> comparer) {
        return ArraySortHelper.BinarySearch(array, 0, array.Length, value, comparer);
    }

    /// <summary>
    /// 二分搜索
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="value">要查找的元素</param>
    /// <param name="comparer">比较器</param>
    /// <param name="fromIndex">包含</param>
    /// <param name="toIndex">不包含</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(T[] array, T value, IComparer<T> comparer, int fromIndex, int toIndex) {
        RangeCheck(array.Length, fromIndex, toIndex);
        return ArraySortHelper.BinarySearch(array, fromIndex, toIndex, value, comparer);
    }

    /// <summary>
    /// 自定义二分查找(适用无法构建T时)
    /// </summary>
    /// <param name="array"></param>
    /// <param name="comparer">比较器，参数为mid</param>
    /// <typeparam name="T">mid</typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(T[] array, Func<T, int> comparer) {
        return ArraySortHelper.BinarySearch(array, 0, array.Length, comparer);
    }

    /// <summary>
    /// 自定义二分查找(适用无法构建T时)
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="comparer">比较器，参数为mid</param>
    /// <param name="fromIndex">包含</param>
    /// <param name="toIndex">不包含</param>
    /// <typeparam name="T">mid</typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(T[] array, Func<T, int> comparer, int fromIndex, int toIndex) {
        RangeCheck(array.Length, fromIndex, toIndex);
        return ArraySortHelper.BinarySearch(array, fromIndex, toIndex, comparer);
    }

    #endregion

#nullable disable

    /// <summary>
    /// 将指定位置的元素移动到目标为止
    /// </summary>
    /// <param name="array"></param>
    /// <param name="index"></param>
    /// <param name="newIndex"></param>
    /// <typeparam name="T"></typeparam>
    public static void MoveTo<T>(T[] array, int index, int newIndex) {
        if (newIndex == index) return;
        T element = array[index];
        if (index < newIndex) {
            Array.Copy(array, index + 1, array, index, newIndex - index);
        } else {
            Array.Copy(array, newIndex, array, newIndex + 1, index - newIndex);
        }
        array[newIndex] = element;
    }

    public static void Insert<T>(ref T[] array, int index, T item) {
        T[] temp = new T[array.Length + 1];
        Array.Copy(array, temp, index);
        temp[index] = item;
        if (index < array.Length) {
            Array.Copy(array, index, temp, index + 1, array.Length - index);
        }
        array = temp;
    }

    public static void InsertRange<T>(ref T[] array, int index, T[] items) {
        T[] temp = new T[array.Length + items.Length];
        Array.Copy(array, temp, index);
        Array.Copy(items, 0, temp, index, items.Length);
        // 允许直接插入到末尾
        if (index < array.Length) {
            Array.Copy(array, index, temp, index + items.Length, array.Length - index);
        }
        array = temp;
    }

    public static void RemoveAt<T>(ref T[] array, int index) {
        T[] temp = new T[array.Length - 1];
        Array.Copy(array, temp, index);
        Array.Copy(array, index + 1, temp, index, array.Length - (index + 1));
        array = temp;
    }

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Copy<T>(this T[] src) {
        return CopyOf(src);
    }

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始数组</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CopyOf<T>(T[] src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (src.Length == 0) {
            return src;
        }
        T[] result = new T[src.Length];
        Array.Copy(src, result, src.Length);
        return result;
    }

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始数组</param>
    /// <param name="offset">拷贝的起始偏移量</param>
    /// <param name="len">要拷贝的长度；可大于或小于原始数组长度</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CopyOf<T>(T[] src, int offset, int len) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        T[] result = new T[len];
        int copyLen = Math.Min(src.Length - offset, len);
        Array.Copy(src, offset, result, 0, copyLen);
        return result;
    }

    /// <summary>
    /// 根据索引区间清理数组
    /// </summary>
    /// <param name="list"></param>
    /// <param name="star">起始下标</param>
    /// <param name="end">结束下标</param>
    /// <param name="mode">区间模式</param>
    /// <typeparam name="T"></typeparam>
    public static void Clear2<T>(T[] list, int star, int end, RangeMode mode = RangeMode.LeftClosed) {
        switch (mode) {
            case RangeMode.Closed: Array.Clear(list, star, end - star + 1); break;
            case RangeMode.Open: Array.Clear(list, star + 1, end - star); break;
            case RangeMode.LeftClosed: Array.Clear(list, star, end - star); break;
            case RangeMode.LeftOpen: Array.Clear(list, star + 1, end - star + 1); break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    /// <summary>
    /// java风格的Fill
    /// </summary>
    /// <param name="list"></param>
    /// <param name="startIndex">开始下标(包含)</param>
    /// <param name="endIndex">结束下标（不包含）</param>
    /// <param name="value">要填充的值</param>
    /// <typeparam name="T"></typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill2<T>(T[] list, int startIndex, int endIndex, T value) {
        int count = endIndex - startIndex;
        Array.Fill(list, value, startIndex, count);
    }

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this T[] list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this Span<T> list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }
#nullable restore

    /// <summary>
    /// 洗牌算法
    /// </summary>
    /// <param name="list">要打乱的列表</param>
    /// <param name="rnd">随机种子</param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(T[] list, Random? rnd = null) {
        rnd ??= MathCommon.SharedRandom;
        int size = list.Length;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

    /// <summary>
    /// 洗牌算法
    /// </summary>
    public static void Shuffle<T>(Span<T> list, Random? rnd = null) {
        rnd ??= MathCommon.SharedRandom;
        int size = list.Length;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckIndex(int index, int length) {
        if (index < 0 || index >= length) {
            throw new IndexOutOfRangeException($"length: {length}, index {index}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CheckInsert(int index, int length) {
        if (index < 0 || index > length) {
            throw new IndexOutOfRangeException($"length: {length}, index {index}");
        }
    }

    /// <summary>
    /// 检查索引合法性
    /// </summary>
    /// <param name="arrayLength">数组长度</param>
    /// <param name="fromIndex">包含</param>
    /// <param name="toIndex">不包含</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RangeCheck(int arrayLength, int fromIndex, int toIndex) {
        if (fromIndex > toIndex) {
            throw new ArgumentException($"fromIndex({fromIndex}) > toIndex({toIndex})");
        }
        if (fromIndex < 0) {
            throw new IndexOutOfRangeException($"fromIndex: {fromIndex} < 0");
        }
        if (toIndex > arrayLength) {
            throw new IndexOutOfRangeException($"toIndex: {toIndex} > arrayLength: {arrayLength}");
        }
    }

    /** 最大支持9阶 - 我都没见过3阶以上的数组... */
    private static readonly string[] arrayRankSymbols =
    {
        "[]",
        "[][]",
        "[][][]",
        "[][][][]",
        "[][][][][]",
        "[][][][][][]",
        "[][][][][][][]",
        "[][][][][][][][]",
        "[][][][][][][][][]"
    };

    /// <summary>
    /// 获取数组阶数对应的符号
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ArrayRankSymbol(int rank) {
        if (rank < 1 || rank > 9) {
            throw new ArgumentException("rank: " + rank);
        }
        return arrayRankSymbols[rank - 1];
    }

    /** 获取根元素的类型 -- 如果Type是数组，则返回最底层的元素类型；如果不是数组，则返回type */
    public static Type GetRootElementType(Type type) {
        while (type.IsArray) {
            type = type.GetElementType()!;
        }
        return type;
    }

    /** 获取数组的阶数 -- 如果不是数组，则返回0 */
    public static int GetArrayRank(Type type) {
        int r = 0;
        while (type.IsArray) {
            r++;
            type = type.GetElementType()!;
        }
        return r;
    }
}
}