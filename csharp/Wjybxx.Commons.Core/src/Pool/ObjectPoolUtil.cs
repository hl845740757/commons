#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Text;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Pool
{
public static class ObjectPoolUtil
{
    #region 扩展方法

    /// <summary>
    /// 该扩展方法用于更方便地使用using
    /// </summary>
    /// <param name="pool"></param>
    /// <param name="r"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReleaseHelper<T> Acquire<T>(this IObjectPool<T> pool, out T r) where T : class {
        r = pool.Acquire();
        return new ReleaseHelper<T>(pool, r);
    }

    /// <summary>
    /// 该扩展方法用于更方便地使用using
    /// </summary>
    /// <param name="pool">对象池</param>
    /// <param name="minLen">数组的最小长度</param>
    /// <param name="r"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReleaseHelper<T[]> Acquire<T>(this IArrayPool<T> pool, int minLen, out T[] r) {
        r = pool.Acquire(minLen);
        return new ReleaseHelper<T[]>(pool, r);
    }

    /// <summary>
    /// 批量归还对象
    /// </summary>
    /// <param name="pool"></param>
    /// <param name="objects"></param>
    /// <typeparam name="T"></typeparam>
    public static void ReleaseAll<T>(this IObjectPool<T> pool, IEnumerable<T?> objects) {
        if (pool == null) throw new ArgumentNullException(nameof(pool));
        if (objects is List<T> arrayList) { // struct enumerator
            for (int i = 0, n = arrayList.Count; i < n; i++) {
                T obj = arrayList[i];
                if (null == obj) {
                    continue;
                }
                pool.Release(obj);
            }
        } else {
            foreach (T obj in objects) {
                if (null == obj) {
                    continue;
                }
                pool.Release(obj);
            }
        }
    }

    #endregion

    #region factory

    public static readonly Func<StringBuilder> stringBuilderFactory = () => new StringBuilder();
    public static readonly Action<StringBuilder> stringBuilderCleaner = sb => sb.Clear();

    // 默认容量版
    public static ObjectPool<StringBuilder> NewStringBuilderPool(int poolSize) {
        return new ObjectPool<StringBuilder>(stringBuilderFactory, stringBuilderCleaner, poolSize);
    }

    public static ObjectPool<List<T>> NewListPool<T>(int poolSize) {
        return new ObjectPool<List<T>>(ObjectPoolUtil<T>.listFactory, ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<HashSet<T>> NewHashSetPool<T>(int poolSize) {
        return new ObjectPool<HashSet<T>>(ObjectPoolUtil<T>.hashSetFactory, ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<LinkedHashSet<T>> NewLinkedHashSetPool<T>(int poolSize) {
        return new ObjectPool<LinkedHashSet<T>>(ObjectPoolUtil<T>.linkedHashSetFactory, ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<Queue<T>> NewQueuePool<T>(int poolSize) {
        return new ObjectPool<Queue<T>>(ObjectPoolUtil<T>.queueFactory, ObjectPoolUtil<T>.queueCleaner, poolSize);
    }

    public static ObjectPool<Stack<T>> NewStackPool<T>(int poolSize) {
        return new ObjectPool<Stack<T>>(ObjectPoolUtil<T>.stackFactory, ObjectPoolUtil<T>.stackCleaner, poolSize);
    }

    public static ObjectPool<Dictionary<K, V>> NewDictionaryPool<K, V>(int poolSize) {
        return new ObjectPool<Dictionary<K, V>>(ObjectPoolUtil<K, V>.dictionaryFactory, ObjectPoolUtil<K, V>.cleaner, poolSize);
    }

    public static ObjectPool<LinkedDictionary<K, V>> NewLinkedDictionaryPool<K, V>(int poolSize) {
        return new ObjectPool<LinkedDictionary<K, V>>(ObjectPoolUtil<K, V>.linkedDictionaryFactory, ObjectPoolUtil<K, V>.cleaner, poolSize);
    }

    // 可指定初始容量版
    public static ObjectPool<List<T>> NewListPool<T>(int initCapacity, int poolSize) {
        return new ObjectPool<List<T>>(() => new List<T>(initCapacity), ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<HashSet<T>> NewHashSetPool<T>(int initCapacity, int poolSize) {
        return new ObjectPool<HashSet<T>>(() => new HashSet<T>(initCapacity), ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<LinkedHashSet<T>> NewLinkedHashSetPool<T>(int initCapacity, int poolSize) {
        return new ObjectPool<LinkedHashSet<T>>(() => new LinkedHashSet<T>(initCapacity), ObjectPoolUtil<T>.cleaner, poolSize);
    }

    public static ObjectPool<Queue<T>> NewQueuePool<T>(int initCapacity, int poolSize) {
        return new ObjectPool<Queue<T>>(() => new Queue<T>(initCapacity), ObjectPoolUtil<T>.queueCleaner, poolSize);
    }

    public static ObjectPool<Stack<T>> NewStackPool<T>(int initCapacity, int poolSize) {
        return new ObjectPool<Stack<T>>(() => new Stack<T>(initCapacity), ObjectPoolUtil<T>.stackCleaner, poolSize);
    }

    public static ObjectPool<Dictionary<K, V>> NewDictionaryPool<K, V>(int initCapacity, int poolSize) {
        return new ObjectPool<Dictionary<K, V>>(() => new Dictionary<K, V>(initCapacity), ObjectPoolUtil<K, V>.cleaner, poolSize);
    }

    public static ObjectPool<LinkedDictionary<K, V>> NewLinkedDictionaryPool<K, V>(int initCapacity, int poolSize) {
        return new ObjectPool<LinkedDictionary<K, V>>(() => new LinkedDictionary<K, V>(initCapacity), ObjectPoolUtil<K, V>.cleaner, poolSize);
    }
    #endregion

    #region 集合辅助方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> ClearAndReturn<T>(this List<T> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<T> ClearAndReturn<T>(this HashSet<T> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LinkedHashSet<T> ClearAndReturn<T>(this LinkedHashSet<T> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Queue<T> ClearAndReturn<T>(this Queue<T> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Stack<T> ClearAndReturn<T>(this Stack<T> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dictionary<K, V> ClearAndReturn<K, V>(this Dictionary<K, V> self) {
        self.Clear();
        return self;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LinkedDictionary<K, V> ClearAndReturn<K, V>(this LinkedDictionary<K, V> self) {
        self.Clear();
        return self;
    }

    #endregion
}

public static class ObjectPoolUtil<T>
{
    public static readonly Func<List<T>> listFactory = () => new List<T>();
    public static readonly Func<HashSet<T>> hashSetFactory = () => new HashSet<T>();
    public static readonly Func<LinkedHashSet<T>> linkedHashSetFactory = () => new LinkedHashSet<T>();
    public static readonly Func<Queue<T>> queueFactory = () => new Queue<T>();
    public static readonly Func<Stack<T>> stackFactory = () => new Stack<T>();

    public static readonly Action<ICollection<T>> cleaner = collection => collection.Clear();
    public static readonly Action<Queue<T>> queueCleaner = queue => queue.Clear();
    public static readonly Action<Stack<T>> stackCleaner = stack => stack.Clear();
}

public static class ObjectPoolUtil<K, V>
{
    public static readonly Func<Dictionary<K, V>> dictionaryFactory = () => new Dictionary<K, V>();
    public static readonly Func<LinkedDictionary<K, V>> linkedDictionaryFactory = () => new LinkedDictionary<K, V>();
    public static readonly Action<IDictionary<K, V>> cleaner = dictionary => dictionary.Clear();
}
}