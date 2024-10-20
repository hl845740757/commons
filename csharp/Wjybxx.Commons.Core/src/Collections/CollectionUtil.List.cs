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

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 该类用于增加List工具方法
/// </summary>
public static partial class CollectionUtil
{
    #region factory

    /** 创建一个元素的List */
    public static List<T> NewList<T>(T first) {
        return new List<T>(1) { first };
    }

    /** 创建2个元素的List */
    public static List<T> NewList<T>(T first, T second) {
        return new List<T>(2) { first, second };
    }

    /** 创建3个元素的List */
    public static List<T> NewList<T>(T first, T second, T third) {
        return new List<T>(3) { first, second, third };
    }

    #endregion

    #region equals/hashcode

    public static int HashCode<T>(IList<T?>? list) where T : class {
        if (list == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < list.Count; i++) {
            T e = list[i];
            r = r * 31 + (e == null ? 0 : e.GetHashCode());
        }
        return r;
    }

    public static int HashCode<T>(IList<T?>? list, Func<T, int> hashFunc) {
        if (list == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < list.Count; i++) {
            T e = list[i];
            r = r * 31 + hashFunc(e);
        }
        return r;
    }

    #endregion

#nullable disable

    #region index

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf<T>(this IList<T> list, T element, IEqualityComparer<T> comparer = null) {
        return IndexOf(list, element, 0, list.Count, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOf<T>(this IList<T> list, T element, IEqualityComparer<T> comparer = null) {
        return LastIndexOf(list, element, 0, list.Count, comparer);
    }

    /** 查对象在数组中的下标 */
    public static int IndexOf<T>(this IList<T> list, T element, int start, int end, IEqualityComparer<T> comparer = null) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (element == null) {
            for (int i = start; i < end; i++) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            if (comparer == null) {
                comparer = EqualityComparer<T>.Default;
            }
            for (int i = start; i < end; i++) {
                if (comparer.Equals(element, list[i])) {
                    return i;
                }
            }
        }
        return -1;
    }

    /** 反向查对象在数组中的下标 */
    public static int LastIndexOf<T>(this IList<T> list, T element, int start, int end, IEqualityComparer<T> comparer = null) {
        if (element == null) {
            for (int i = end - 1; i >= start; i--) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            if (comparer == null) {
                comparer = EqualityComparer<T>.Default;
            }
            for (int i = end - 1; i >= start; i--) {
                if (comparer.Equals(element, list[i])) {
                    return i;
                }
            }
        }
        return -1;
    }

    #endregion

    #region indexref

    /** 查询List中是否包含指定对象引用 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsRef<T>(this IList<T> list, T element) where T : class {
        return IndexOfRef(list, element, 0, list.Count) >= 0;
    }

    /** 查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfRef<T>(this IList<T> list, object element) where T : class {
        return IndexOfRef(list, element, 0, list.Count);
    }

    /** 反向查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfRef<T>(this IList<T> list, object element) where T : class {
        return LastIndexOfRef(list, element, 0, list.Count);
    }

    /// <summary>
    /// 查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="element">要查找的元素</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int IndexOfRef<T>(this IList<T> list, object element, int start, int end) where T : class {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (element == null) {
            for (int i = start; i < end; i++) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = start; i < end; i++) {
                if (element == list[i]) {
                    return i;
                }
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
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int LastIndexOfRef<T>(this IList<T> list, object element, int start, int end) where T : class {
        if (element == null) {
            for (int i = end - 1; i >= start; i--) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = end - 1; i >= start; i--) {
                if (element == list[i]) {
                    return i;
                }
            }
        }
        return -1;
    }

    /** 从List中删除指定引用 */
    public static bool RemoveRef<T>(this IList<T> list, object element) where T : class {
        int index = IndexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.RemoveAt(index);
        return true;
    }

    #endregion

    #region indexOfCustom

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfCustom<T>(this IList<T> list, Predicate<T> filter) {
        return IndexOfCustom(list, filter, 0, list.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfCustom<T>(this IList<T> list, Predicate<T> filter) {
        return LastIndexOfCustom(list, filter, 0, list.Count);
    }

    /// <summary>
    /// 查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="filter">筛选条件</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int IndexOfCustom<T>(this IList<T> list, Predicate<T> filter, int start, int end) {
        for (int idx = start; idx < end; idx++) {
            if (filter(list[idx])) {
                return idx;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="filter">筛选条件</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int LastIndexOfCustom<T>(this IList<T> list, Predicate<T> filter, int start, int end) {
        for (int i = end - 1; i >= start; i--) {
            if (filter(list[i])) {
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
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(List<T> array, T value, Comparer<T> comparer) {
        return ArraySortHelper.BinarySearch(array, 0, array.Count, value, comparer);
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
    public static int BinarySearch<T>(List<T> array, T value, Comparer<T> comparer, int fromIndex, int toIndex) {
        ArrayUtil.RangeCheck(array.Count, fromIndex, toIndex);
        return ArraySortHelper.BinarySearch(array, fromIndex, toIndex, value, comparer);
    }

    /// <summary>
    /// 自定义二分查找(适用无法构建T时)
    /// </summary>
    /// <param name="array"></param>
    /// <param name="comparer">比较器</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(List<T> array, Func<T, int> comparer) {
        return ArraySortHelper.BinarySearch(array, 0, array.Count, comparer);
    }

    /// <summary>
    /// 自定义二分查找(适用无法构建T时)
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="comparer">比较器</param>
    /// <param name="fromIndex">包含</param>
    /// <param name="toIndex">不包含</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T>(List<T> array, Func<T, int> comparer, int fromIndex, int toIndex) {
        ArrayUtil.RangeCheck(array.Count, fromIndex, toIndex);
        return ArraySortHelper.BinarySearch(array, fromIndex, toIndex, comparer);
    }

    #endregion

#nullable enable

    #region peek

    /// <summary>
    /// 获取List的首个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    /// <exception cref="InvalidOperationException">如果集合为空</exception>
    public static T PeekFirst<T>(this IList<T> list) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            return list[0];
        }
        throw ThrowHelper.CollectionEmptyException();
    }

    /// <summary>
    /// 获取List的最后一个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    /// <exception cref="InvalidOperationException">如果集合为空</exception>
    public static T PeekLast<T>(this IList<T> list) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            return list[count - 1];
        }
        throw ThrowHelper.CollectionEmptyException();
    }

    /// <summary>
    /// 获取List的首个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    public static bool TryPeekFirst<T>(this IList<T> list, out T? value) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            value = list[0];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 获取List的最后一个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    public static bool TryPeekLast<T>(this IList<T> list, out T? value) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            value = list[count - 1];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 删除List的首个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    public static bool TryRemoveFirst<T>(this IList<T> list, out T? value) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            value = list[0];
            list.RemoveAt(0);
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 删除List的最后一个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    public static bool TryRemoveLast<T>(this IList<T> list, out T? value) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            value = list[count - 1];
            list.RemoveAt(count - 1);
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region misc

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this IList<T> list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }

    /// <summary>
    /// 洗牌算法
    /// 1.尽量只用于数组列表
    /// 2.DotNet8开始自带洗牌算法
    /// </summary>
    /// <param name="list">要打乱的列表</param>
    /// <param name="rnd">随机种子</param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(IList<T> list, Random? rnd = null) {
        rnd ??= MathCommon.SharedRandom;
        int size = list.Count;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

    /// <summary>
    /// 拼接两个List为新的List
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> Concat<T>(IList<T>? lhs, IList<T>? rhs) {
        List<T> result = new List<T>(Count(lhs) + Count(rhs));
        if (lhs != null && lhs.Count > 0) {
            result.AddRange(lhs);
        }
        if (rhs != null && rhs.Count > 0) {
            result.AddRange(rhs);
        }
        return result;
    }

    /// <summary>
    /// 拼接多个List为单个List
    /// </summary>
    /// <param name="listArray"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> Concat<T>(params IList<T>?[] listArray) {
        List<T> result = new List<T>();
        foreach (IList<T>? list in listArray) {
            if (list != null && list.Count > 0) {
                result.AddRange(list);
            }
        }
        return result;
    }

    #endregion
}
}