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
using System.Linq;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections
{
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Capacity(int numMappings) {
        if (numMappings < 3) {
            return 4;
        }
        return (int)Math.Ceiling(numMappings / 0.75d);
    }

    /** behavior是否允许丢弃队列的首部 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AllowDiscardHead(this DequeOverflowBehavior behavior) {
        return behavior == DequeOverflowBehavior.CircleBuffer
               || behavior == DequeOverflowBehavior.DiscardHead;
    }

    /** behavior是否允许丢弃队列的尾部 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AllowDiscardTail(this DequeOverflowBehavior behavior) {
        return behavior == DequeOverflowBehavior.CircleBuffer
               || behavior == DequeOverflowBehavior.DiscardTail;
    }

    #region collection

    /// <summary>
    /// 判断集合是否为空或null
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>(ICollection<T>? self) => self == null || self.Count == 0;

    /// <summary>
    /// 判断集合是否为空
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<T>(this ICollection<T> self) => self.Count == 0;

    /// <summary>
    /// 获取集合的数量，如果集合为null，则返回0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<T>(ICollection<T>? self) => self == null ? 0 : self.Count;

    /// <summary>
    /// 尝试压缩空间
    /// </summary>
    public static void TrimExcess<T>(ICollection<T> c) {
        if (c is List<T> list) {
            list.TrimExcess();
        } else if (c is HashSet<T> hashSet) {
            hashSet.TrimExcess();
        } else if (c is IGenericCollection<T> genericCollection) {
            genericCollection.AdjustCapacity(c.Count);
        }
    }

    /// <summary>
    /// 批量Add元素
    /// </summary>
    public static void AddAll<T>(this ICollection<T> self, IEnumerable<T> items) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (self is IGenericCollection<T> generic) {
            int itemsCount = PredicateCount(items);
            if (itemsCount > 0) {
                generic.AdjustCapacity(generic.Count + itemsCount);
            }
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

    #region dictionary

    /// <summary>
    /// 判断字典是否为空或null
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<K, V>(IDictionary<K, V>? self) => self == null || self.Count == 0;

    /// <summary>
    /// 判断字典是否为空
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<K, V>(IDictionary<K, V> self) => self.Count == 0;

    /// <summary>
    /// 获取集合的数量，如果集合为null，则返回0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<K, V>(IDictionary<K, V>? self) => self == null ? 0 : self.Count;

    /// <summary>
    /// 尝试压缩空间
    /// </summary>
    public static void TrimExcess<K, V>(IDictionary<K, V> dic) {
        if (dic is Dictionary<K, V> dictionary) {
            dictionary.TrimExcess();
        } else if (dic is IGenericDictionary<K, V> genericDictionary) {
            genericDictionary.AdjustCapacity(dic.Count);
        }
    }

    /// <summary>
    /// 批量添加元素 -- 如果Key已存在，则抛出异常
    /// </summary>
    public static void AddAll<TKey, TValue>(this IDictionary<TKey, TValue> self, IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (pairs == null) throw new ArgumentNullException(nameof(pairs));
        if (self is IGenericDictionary<TKey, TValue> generic) {
            int itemsCount = PredicateCount(pairs);
            if (itemsCount > 0) {
                generic.AdjustCapacity(generic.Count + itemsCount);
            }
        }
        foreach (KeyValuePair<TKey, TValue> pair in pairs) {
            self.Add(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// 批量添加元素 -- 如果Key已存在，则覆盖
    /// </summary>
    public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> self, IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (pairs == null) throw new ArgumentNullException(nameof(pairs));
        if (self is IGenericDictionary<TKey, TValue> generic) {
            int itemsCount = PredicateCount(pairs);
            if (itemsCount > 0) {
                generic.AdjustCapacity(generic.Count + itemsCount);
            }
        }
        foreach (KeyValuePair<TKey, TValue> pair in pairs) {
            self[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// 获取key关联的值，如果关联的值不存在，则返回给定的默认值。
    /// </summary>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defValue) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        return self.TryGetValue(key, out TValue value) ? value : defValue;
    }

    /** 简单测试items的数量，返回-1表示未知 */
    private static int PredicateCount<T>(IEnumerable<T> items) {
        if (items is ICollection<T> other1) { // C#的数组实现了ICollection...
            return other1.Count;
        }
        if (items is IReadOnlyCollection<T> other2) {
            return other2.Count;
        }
        return -1;
    }

    #endregion

    #region linq

    public static ImmutableList<T> ToImmutableList2<T>(this IEnumerable<T> source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source is ImmutableList<T> list) {
            return list;
        }
        return ImmutableList<T>.CreateRange(source);
    }

    public static ImmutableLinkedHastSet<T> ToImmutableLinkedHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? keyComparer = null) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source is ImmutableLinkedHastSet<T> hastSet) {
            return hastSet;
        }
        return ImmutableLinkedHastSet<T>.CreateRange(source, keyComparer);
    }

    public static ImmutableLinkedDictionary<TKey, TValue> ToImmutableLinkedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> elementSelector,
        IEqualityComparer<TKey>? keyComparer = null) {
        //
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

        IEnumerable<KeyValuePair<TKey, TValue>> pairs = source.Select(e => new KeyValuePair<TKey, TValue>(keySelector(e), elementSelector(e)));
        return ImmutableLinkedDictionary<TKey, TValue>.CreateRange(pairs, keyComparer);
    }

    public static ImmutableLinkedDictionary<TKey, TValue> ToImmutableLinkedDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source,
        IEqualityComparer<TKey>? keyComparer = null) {
        //
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keyComparer == null && source is ImmutableLinkedDictionary<TKey, TValue> dictionary) {
            return dictionary;
        }
        return ImmutableLinkedDictionary<TKey, TValue>.CreateRange(source, keyComparer);
    }

    #endregion
}
}