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
using Wjybxx.Commons.IO;

#pragma warning disable CS1591

namespace Wjybxx.Commons;

/// <summary>
/// 数组工具类
/// </summary>
public static class ArrayUtil
{
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
        if (objA == null || objB == null) {
            return false;
        }
        ReadOnlySpan<T> first = objA;
        ReadOnlySpan<T> second = objB;
        return first.SequenceEqual(second);
    }

    public static int HashCode<T>(T?[]? data) where T : class {
        if (data == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < data.Length; i++) {
            T e = data[i];
            r = r * 31 + (e == null ? 0 : e.GetHashCode());
        }
        return r;
    }

    public static int HashCode<T>(T?[]? data, Func<T, int> hashFunc) {
        if (data == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < data.Length; i++) {
            T e = data[i];
            r = r * 31 + hashFunc(e);
        }
        return r;
    }

    public static int HashCode(byte[]? data) {
        if (data == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < data.Length; i++) {
            r = r * 31 + data[i];
        }
        return r;
    }

    public static int HashCode(int[]? data) {
        if (data == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < data.Length; i++) {
            r = r * 31 + data[i];
        }
        return r;
    }

    #endregion

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] Copy<T>(this T[] src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        T[] result = new T[src.Length];
        Array.Copy(src, result, src.Length);
        return result;
    }

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始四组</param>
    /// <param name="offset">拷贝的起始偏移量</param>
    /// <param name="newLen">可大于或小于原始数组长度</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] CopyOf<T>(T[] src, int offset, int newLen) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (offset < 0) throw new ArgumentException("offset cant be negative");
        if (newLen < 0) throw new ArgumentException("newLen cant be negative");
        T[] result = new T[newLen];
        Array.Copy(src, offset, result, 0, Math.Min(src.Length - offset, newLen));
        return result;
    }

    /** 查对象引用在数组中的下标 */
    public static int IndexOfRef<T>(T?[] list, object? element, int startIndex = 0) where T : class {
        if (startIndex < 0) {
            startIndex = 0;
        }
        if (element == null) {
            for (int idx = startIndex, size = list.Length; idx < size; idx++) {
                if (list[idx] == null) {
                    return idx;
                }
            }
        } else {
            for (int idx = startIndex, size = list.Length; idx < size; idx++) {
                if (ReferenceEquals(list[idx], element)) {
                    return idx;
                }
            }
        }
        return -1;
    }

    /** 查对象引用在数组中的下标 */
    public static int LastIndexOfRef<T>(T?[] list, object? element, int? startIndex = null) where T : class {
        int sindex;
        if (startIndex.HasValue) {
            sindex = Math.Min(list.Length - 1, startIndex.Value);
        } else {
            sindex = list.Length - 1;
        }
        if (element == null) {
            for (int idx = sindex; idx >= 0; idx--) {
                if (list[idx] == null) {
                    return idx;
                }
            }
        } else {
            for (int idx = sindex; idx >= 0; idx--) {
                if (ReferenceEquals(list[idx], element)) {
                    return idx;
                }
            }
        }
        return -1;
    }

    /** 查询List中是否包含指定对象引用 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsRef<T>(T[] list, T element) where T : class {
        return IndexOfRef(list, element) >= 0;
    }

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(T[] list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }

    /// <summary>
    /// 洗牌算法
    /// </summary>
    /// <param name="list">要打乱的列表</param>
    /// <param name="rnd">随机种子</param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(T[] list, Random? rnd = null) {
        rnd ??= Random.Shared;
        int size = list.Length;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(Span<T> list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }

    /// <summary>
    /// 洗牌算法
    /// </summary>
    public static void Shuffle<T>(Span<T> list, Random? rnd = null) {
        rnd ??= Random.Shared;
        int size = list.Length;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

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
}