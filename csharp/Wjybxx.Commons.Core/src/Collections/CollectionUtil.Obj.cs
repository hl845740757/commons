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
using System.Linq;
using System.Text;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 该类用于实现Equals和ToString
/// </summary>
public static partial class CollectionUtil
{
    #region equals

    /// <summary>
    /// 比较两个集合的数据是否相等，忽略元素的顺序
    ///
    /// 1.真泛型下无法简单递归，因此依赖value自身也应调用该方法...
    /// 2.由于支持系统库类型，因此不设定为扩展方法
    /// 3.应当避免用于List类型，尽量只用于Set类型
    /// </summary>
    [Beta]
    public static bool DataEquals<TKey>(ICollection<TKey>? self, ICollection<TKey>? other) {
        if (ReferenceEquals(self, other)) return true;
        if (self == null || other == null) return false;
        if (self.Count != other.Count) return false;

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
    /// 1.真泛型下无法简单递归，因此依赖value自身也应调用该方法...
    /// 2.由于支持系统库类型，因此不设定为扩展方法
    /// 3.应当避免字典的key为null，因为部分字典并不支持null作为key
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <param name="valComparer">value的比较器，用于避免装箱</param>
    /// <returns></returns>
    [Beta]
    public static bool DataEquals<TKey, TValue>(IDictionary<TKey, TValue>? self, IDictionary<TKey, TValue>? other,
                                                IEqualityComparer<TValue>? valComparer = null) {
        if (ReferenceEquals(self, other)) return true;
        if (self == null || other == null) return false;
        if (self.Count != other.Count) return false;

        valComparer ??= EqualityComparer<TValue>.Default;
        int matchCount = 0;
        foreach (KeyValuePair<TKey, TValue> pair in self) {
            if (!other.TryGetValue(pair.Key, out TValue value) || !valComparer.Equals(pair.Value, value)) {
                return false;
            }
            matchCount++;
        }
        return matchCount == self.Count;
    }

    /// <summary>
    /// 比较集合的相等性
    /// (主要服务与代码生成，辅助处理null)
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool SequenceEqual<T>(ICollection<T>? self, ICollection<T>? other) {
        if (ReferenceEquals(self, other)) return true;
        if (self == null || other == null) return false;
        return self.SequenceEqual(other);
    }

    /// <summary>
    /// 比较字典的相等性
    /// (主要服务与代码生成，辅助处理null)
    /// </summary>
    public static bool SequenceEqual<TKey, TValue>(IDictionary<TKey, TValue>? self, IDictionary<TKey, TValue>? other) {
        if (ReferenceEquals(self, other)) return true;
        if (self == null || other == null) return false;
        // 转成元组执行值相等 -- 元组有更高效的Equals实现
        return self.Select(e => (e.Key, e.Value))
            .SequenceEqual(other.Select(e => (e.Key, e.Value)));
    }

    #endregion

    #region HashCode

    /// <summary>
    /// 计算集合数据的HashCode -- 主要服务与代码生成
    /// </summary>
    /// <param name="collection"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int HashCode<T>(ICollection<T>? collection) {
        if (collection == null) return 0;
        int r = 1;
        if (typeof(T).IsValueType) { // Nullable<T>也是安全的
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            foreach (T key in collection) {
                r = r * 31 + (key == null ? 0 : comparer.GetHashCode(key));
            }
        } else {
            foreach (T key in collection) {
                r = r * 31 + (key == null ? 0 : key.GetHashCode());
            }
        }
        return r;
    }

    /// <summary>
    /// 计算字典的HashCode -- 主要服务与代码生成
    /// </summary>
    /// <param name="dictionary"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public static int HashCode<TKey, TValue>(IDictionary<TKey, TValue>? dictionary) {
        if (dictionary == null) return 0;
        int r = 1;
        // 转成元组执行值相等 -- 根据值计算hashcode
        foreach (var tuple in dictionary.Select(e => (e.Key, e.Value))) {
            r = r * 31 + tuple.GetHashCode();
        }
        return r;
    }

    #endregion

    #region ToString

    /// <summary>
    /// 打印集合的详细信息
    /// （暂不递归）
    /// </summary>
    [Beta]
    public static string ToString<T>(IEnumerable<T>? collection, StringBuilder? sb = null) {
        if (collection == null) return "null";
        sb ??= new StringBuilder(32);
        sb.Append('[');
        bool first = true;
        foreach (T value in collection) {
            if (first) {
                first = false;
            } else {
                sb.Append(',');
            }
            sb.Append(value == null ? "null" : value.ToString()); // 系统库默认的约定是null不打印
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// 打印字典的详细信息
    /// （暂不递归）
    /// <code>
    /// [[k1=v1], [k1=v2],[k3=v3]... ]
    /// </code>
    /// </summary>
    [Beta]
    public static string ToString<TKey, TValue>(IDictionary<TKey, TValue>? dictionary, StringBuilder? sb = null) {
        if (dictionary == null) return "null";
        sb ??= new StringBuilder(64);
        sb.Append('[');
        bool first = true;
        foreach (KeyValuePair<TKey, TValue> pair in dictionary) {
            if (first) {
                first = false;
            } else {
                sb.Append(',');
            }
            sb.Append('[');
            sb.Append(pair.Key == null ? "null" : pair.Key.ToString());
            sb.Append(':');
            sb.Append(pair.Value == null ? "null" : pair.Value.ToString()); // 系统库默认的约定是null不打印
            sb.Append(']');
        }
        sb.Append(']');
        return sb.ToString();
    }

    #endregion
}
}