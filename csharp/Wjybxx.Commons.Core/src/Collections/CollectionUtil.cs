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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 集合工具类
/// </summary>
public static partial class CollectionUtil
{
    /** 元素不存在时的索引 */
    public const int IndexNotFound = -1;

    /// <summary>
    /// 计算hash结构的空间
    /// </summary>
    /// <param name="numMappings">期望的元素数量</param>
    /// <returns></returns>
    public static int Capacity(int numMappings) {
        if (numMappings < 3) {
            return 4;
        }
        return (int)Math.Ceiling(numMappings / 0.75d);
    }

    /** behavior是否允许丢弃队列的首部 */
    public static bool AllowDiscardHead(this DequeOverflowBehavior behavior) {
        return behavior == DequeOverflowBehavior.CircleBuffer
               || behavior == DequeOverflowBehavior.DiscardHead;
    }

    /** behavior是否允许丢弃队列的尾部 */
    public static bool AllowDiscardTail(this DequeOverflowBehavior behavior) {
        return behavior == DequeOverflowBehavior.CircleBuffer
               || behavior == DequeOverflowBehavior.DiscardTail;
    }

    #region list

    /** 查对象引用在数组中的下标 */
    public static int IndexOfRef<T>(this IList<T?> list, object? element, int startIndex = 0) where T : class {
        if (startIndex < 0) {
            startIndex = 0;
        }
        if (element == null) {
            for (int idx = startIndex, size = list.Count; idx < size; idx++) {
                if (list[idx] == null) {
                    return idx;
                }
            }
        } else {
            for (int idx = startIndex, size = list.Count; idx < size; idx++) {
                if (ReferenceEquals(list[idx], element)) {
                    return idx;
                }
            }
        }
        return -1;
    }

    /** 查对象引用在数组中的下标 */
    public static int LastIndexOfRef<T>(this IList<T?> list, object? element, int? startIndex = null) where T : class {
        int sindex;
        if (startIndex.HasValue) {
            sindex = Math.Min(list.Count, startIndex.Value);
        } else {
            sindex = list.Count - 1;
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
    public static bool ContainsRef<T>(this IList<T> list, T element) where T : class {
        return IndexOfRef(list, element) >= 0;
    }

    /** 根据引用删除元素 */
    public static bool RemoveRef<T>(this IList<T> list, object element) where T : class {
        int index = IndexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(IList<T> list, int i, int j) {
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
        rnd ??= Random.Shared;
        int size = list.Count;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }

    /** 创建一个单元素的List */
    public static List<T> NewList<T>(T first) {
        return new List<T>(1) { first };
    }

    /** 创建2个单元素的List */
    public static List<T> NewList<T>(T first, T second) {
        return new List<T>(2) { first, second };
    }

    /** 创建3个单元素的List */
    public static List<T> NewList<T>(T first, T second, T third) {
        return new List<T>(3) { first, second, third };
    }

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
        throw CollectionUtil.CollectionEmptyException();
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
        throw CollectionUtil.CollectionEmptyException();
    }

    /// <summary>
    /// 获取List的首个元素
    /// </summary>
    /// <exception cref="ArgumentNullException">如果List为null</exception>
    public static bool TryPeekFirst<T>(this IList<T> list, out T value) {
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
    public static bool TryPeekLast<T>(this IList<T> list, out T value) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        int count = list.Count;
        if (count > 0) {
            value = list[count - 1];
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region collection

    /// <summary>
    /// 批量Add元素
    /// </summary>
    public static void AddAll<T>(this ICollection<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (self is IGenericCollection<T> generic && items is ICollection<T> otherCollection) {
            generic.AdjustCapacity(generic.Count + otherCollection.Count);
        }
        foreach (T item in items) {
            self.Add(item);
        }
    }

    /// <summary>
    /// 批量Add元素
    /// </summary>
    public static void AddAll<T>(this IGenericCollection<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (items is ICollection<T> otherCollection) {
            self.AdjustCapacity(self.Count + otherCollection.Count);
        }
        foreach (T item in items) {
            self.Add(item);
        }
    }

    /// <summary>
    /// 批量删除元素
    /// </summary>
    /// <returns>删除的元素个数</returns>
    public static int RemoveAll<T>(this ICollection<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        int r = 0;
        foreach (T key in items) {
            if (self.Remove(key)) r++;
        }
        return r;
    }

    /// <summary>
    /// 删除不在保留集合中的元素
    /// （C#的迭代器不支持迭代时删除，因此只能先收集key；由于开销可能较大，不定义为扩展方法，需要显式调用）
    /// </summary>
    /// <param name="self">需要操作的集合</param>
    /// <param name="retainItems">需要保留的元素</param>
    /// <typeparam name="T"></typeparam>
    public static void RetainAll<T>(ICollection<T> self, ICollection<T> retainItems) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (retainItems == null) throw new ArgumentNullException(nameof(retainItems));
        IEnumerator<T> itr = self.GetEnumerator();
        if (itr is IUnsafeIterator<T> unsafeItr) {
            while (unsafeItr.MoveNext()) {
                if (!retainItems.Contains(unsafeItr.Current)) {
                    unsafeItr.Remove();
                }
            }
        } else {
            List<T> needRemoveKeys = new List<T>();
            while (itr.MoveNext()) {
                if (!retainItems.Contains(itr.Current)) {
                    needRemoveKeys.Add(itr.Current);
                }
            }
            for (var i = 0; i < needRemoveKeys.Count; i++) {
                self.Remove(needRemoveKeys[i]);
            }
        }
    }

    #endregion

    #region Set

    /// <summary>
    /// 批量Add元素
    /// </summary>
    /// <returns>新插入的元素个数</returns>
    public static int AddAll<T>(this ISet<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (self is IGenericCollection<T> generic && items is ICollection<T> otherCollection) {
            generic.AdjustCapacity(self.Count + otherCollection.Count);
        }
        int r = 0;
        foreach (T item in items) {
            if (self.Add(item)) r++;
        }
        return r;
    }

    /// <summary>
    /// 批量Add元素
    /// (没有继承ISet接口，因此需要独立的扩展方法)
    /// </summary>
    /// <returns>新插入的元素个数</returns>
    public static int AddAll<T>(this IGenericSet<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (items is ICollection<T> otherCollection) {
            self.AdjustCapacity(self.Count + otherCollection.Count);
        }
        int r = 0;
        foreach (T item in items) {
            if (self.Add(item)) r++;
        }
        return r;
    }

    #endregion

    #region dictionary

    /// <summary>
    /// 批量添加元素 -- 如果Key已存在，则覆盖
    /// </summary>
    public static void PutAll<TKey, TValue>(this IGenericDictionary<TKey, TValue> self, IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (pairs == null) throw new ArgumentNullException(nameof(pairs));
        if (pairs is ICollection<KeyValuePair<TKey, TValue>> collection) {
            self.AdjustCapacity(self.Count + collection.Count);
        }
        foreach (KeyValuePair<TKey, TValue> pair in pairs) {
            self.Put(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// 获取key关联的值，如果关联的值不存在，则返回给定的默认值。
    /// </summary>
    public static TValue GetValueOrDefault<TKey, TValue>(this IGenericDictionary<TKey, TValue> self, TKey key, TValue defValue) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        return self.TryGetValue(key, out TValue value) ? value : defValue;
    }

    #endregion

    #region internal

    internal static InvalidOperationException CollectionFullException() {
        return new InvalidOperationException("Collection is full");
    }

    internal static InvalidOperationException CollectionEmptyException() {
        return new InvalidOperationException("Collection is Empty");
    }

    internal static KeyNotFoundException KeyNotFoundException(object? key) {
        return new KeyNotFoundException(key == null ? "null" : key.ToString());
    }

    #endregion
}