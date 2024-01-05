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

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 该类用于实现Equals
/// </summary>
public static partial class CollectionUtil
{
    /// <summary>
    /// 比较两个Set集合的内容是否相等
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static bool ContentEquals<TKey>(IGenericSet<TKey> self, IGenericSet<TKey>? other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) {
            return false;
        }
        if (ReferenceEquals(self, other)) {
            return true;
        }
        
        if (self.Count != other.Count) {
            return false;
        }
        int matchCount = 0;
        foreach (TKey key in self) {
            if (!other.Contains(key)) {
                return false;
            }
            matchCount++;
        }
        return matchCount == self.Count;
    }

    /// <summary>
    /// 比较两个字典的内容是否相等，忽略KV顺序
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public static bool ContentEquals<TKey, TValue>(IDictionary<TKey, TValue> self, IDictionary<TKey, TValue>? other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) {
            return false;
        }
        if (ReferenceEquals(self, other)) {
            return true;
        }
        if (typeof(TValue).IsValueType) {
            return ContentEquals(self, other, EqualityComparer<TValue>.Default);
        }
        
        if (self.Count != other.Count) {
            return false;
        }
        int matchCount = 0;
        foreach (KeyValuePair<TKey, TValue> pair in self) {
            try {
                // 部分字典key为null直接抛出异常，而不是返回false...
                if (!other.TryGetValue(default!, out TValue value)) {
                    return false;
                }
                if (!pair.Value.Equals(value)) {
                    return false;
                }
                matchCount++;
            }
            catch (ArgumentException) {
                return false;
            }
        }
        return matchCount == self.Count;
    }

    /// <summary>
    /// 比较两个字典的内容是否相等，忽略KV顺序
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <param name="comparer">value的比较器，用于避免装箱</param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public static bool ContentEquals<TKey, TValue>(IDictionary<TKey, TValue> self, IDictionary<TKey, TValue>? other,
                                                   IEqualityComparer<TValue> comparer) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (comparer == null) throw new ArgumentNullException(nameof(comparer));
        if (other == null) {
            return false;
        }
        if (ReferenceEquals(self, other)) {
            return true;
        }
        
        if (self.Count != other.Count) {
            return false;
        }
        int matchCount = 0;
        foreach (KeyValuePair<TKey, TValue> pair in self) {
            try {
                // 部分字典key为null直接抛出异常，而不是返回false...
                if (!other.TryGetValue(default!, out TValue value)) {
                    return false;
                }
                if (!comparer.Equals(pair.Value, value)) {
                    return false;
                }
                matchCount++;
            }
            catch (ArgumentException) {
                return false;
            }
        }
        return matchCount == self.Count;
    }
}