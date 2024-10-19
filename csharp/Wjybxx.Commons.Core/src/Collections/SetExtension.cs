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

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// Set工具类
/// </summary>
public static class SetExtension
{
    // other可能是Readonly类型接口
    internal static int GetCount<T>(IEnumerable<T> enumerable) {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
        if (enumerable is ICollection<T> collection) {
            return collection.Count;
        }
        if (enumerable is IReadOnlyCollection<T> readOnlyCollection) {
            return readOnlyCollection.Count;
        }
        throw new ArgumentException("unsupported type: " + enumerable.GetType());
    }

    private static bool ContainsAll<T>(ICollection<T> collection, IEnumerable<T> other) {
        foreach (T item in other) {
            if (!collection.Contains(item)) return false;
        }
        return true;
    }

    // IReadonlyCollection居然没有Contains接口
    private static bool ContainsAll<T>(IReadOnlySet<T> collection, IEnumerable<T> other) {
        foreach (T item in other) {
            if (!collection.Contains(item)) return false;
        }
        return true;
    }

    private static bool ContainsAny<T>(ICollection<T> collection, IEnumerable<T> other) {
        foreach (T item in other) {
            if (collection.Contains(item)) return true;
        }
        return false;
    }

    private static bool ContainsAny<T>(IReadOnlySet<T> collection, IEnumerable<T> other) {
        foreach (T item in other) {
            if (collection.Contains(item)) return true;
        }
        return false;
    }

    /// <summary>
    /// 当前集合是否是目标集合的子集
    /// </summary>
    public static bool IsSubsetOf<T>(ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (self is IReadOnlySet<T> selfAsReadonlySet) {
            return selfAsReadonlySet.IsSubsetOf(other);
        }
        if (self is ISet<T> selfAsSet) {
            return selfAsSet.IsSubsetOf(other);
        }
        // -------------------------------------------------------------
        // 任意集合是自身的普通子集
        if (self == other) {
            return true;
        }
        int selfCount = self.Count;
        int otherCount = GetCount(other);
        // 空集合是任何集合的普通子集
        if (selfCount == 0) {
            return true;
        }
        // 元素个数更多，一定不是子集 -- 其实由于Hash和Equals的策略不同，两个集合的Count其实并不能直接比较，我们暂不处理 -- 这种特殊情况由用户自身处理
        if (selfCount > otherCount) {
            return false;
        }
        // 测试自身元素在目标集合中是否存在
        if (other is ICollection<T> otherAsCollection) {
            return ContainsAll(otherAsCollection, self);
        }
        if (other is IReadOnlySet<T> readOnlySet) {
            return ContainsAll(readOnlySet, self);
        }
        throw new ArgumentException("unsupported type: " + other.GetType());
    }

    /// <summary>
    /// 当前集合是否是目标集合的真子集（不包括全集）
    ///
    /// ps:系统库对other约定的是<see cref="IEnumerable{T}"/>类型，太麻烦了...
    /// </summary>
    public static bool IsProperSubsetOf<T>(ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (self is IReadOnlySet<T> selfAsReadonlySet) {
            return selfAsReadonlySet.IsProperSubsetOf(other);
        }
        if (self is ISet<T> selfAsSet) {
            return selfAsSet.IsProperSubsetOf(other);
        }
        // -------------------------------------------------------------
        // 集合不是自身的真子集
        if (self == other) {
            return false;
        }
        int selfCount = self.Count;
        int otherCount = GetCount(other);
        // 空集合是任意非空集合的真子集
        if (selfCount == 0) {
            return otherCount > 0;
        }
        // 元素个数相等或更多，一定不是真子集
        if (selfCount >= otherCount) {
            return false;
        }
        // 测试自身元素在目标集合中是否存在
        if (other is ICollection<T> otherAsCollection) {
            return ContainsAll(otherAsCollection, self);
        }
        if (other is IReadOnlySet<T> readOnlySet) {
            return ContainsAll(readOnlySet, self);
        }
        throw new ArgumentException("unsupported type: " + other.GetType());
    }

    /// <summary>
    /// 当前集合是否是目标集合的超集 -- 包含目标集合的所有元素
    /// </summary>
    public static bool IsSupersetOf<T>(ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (self is IReadOnlySet<T> selfAsReadonlySet) {
            return selfAsReadonlySet.IsSupersetOf(other);
        }
        if (self is ISet<T> selfAsSet) {
            return selfAsSet.IsSupersetOf(other);
        }
        // -------------------------------------------------------------
        if (self == other) {
            return true;
        }
        int selfCount = self.Count;
        int otherCount = GetCount(other);
        // 任意集合是空集合的超集
        if (otherCount == 0) {
            return true;
        }
        // 元素个数更少，一定不是超集
        if (selfCount < otherCount) {
            return false;
        }
        return ContainsAll(self, other);
    }

    /// <summary>
    /// 当前集合是否是目标集合的真超集 -- 包含目标集合的所有元素，且包含
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsProperSupersetOf<T>(ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (self is IReadOnlySet<T> selfAsReadonlySet) {
            return selfAsReadonlySet.IsProperSupersetOf(other);
        }
        if (self is ISet<T> selfAsSet) {
            return selfAsSet.IsProperSupersetOf(other);
        }
        // -------------------------------------------------------------
        if (self == other) {
            return false;
        }
        int selfCount = self.Count;
        int otherCount = GetCount(other);
        // 任意非空集合是空集合的真超集
        if (otherCount == 0) {
            return selfCount > 0;
        }
        // 元素个数相等或更少，一定不是真超集
        if (selfCount <= otherCount) {
            return false;
        }
        return ContainsAll(self, other);
    }

    /// <summary>
    /// 测试两个集合是否相交
    /// </summary>
    public static bool Overlaps<T>(ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        foreach (T item in other) {
            if (self.Contains(item)) {
                return true;
            }
        }
        return false;
    }
}
}