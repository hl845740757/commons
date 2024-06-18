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
/// 该类用于增加字典工具方法
/// </summary>
public static partial class CollectionUtil
{
    #region factory

    /** 创建一个元素的字典 */
    public static Dictionary<TKey, TValue> NewDictionary<TKey, TValue>(TKey key, TValue value) {
        Dictionary<TKey, TValue>? values = new Dictionary<TKey, TValue>(2);
        values.Add(key, value);
        return values;
    }

    /** 创建2个元素字典 */
    public static Dictionary<TKey, TValue> NewDictionary<TKey, TValue>(TKey key1, TValue value1,
                                                                       TKey key2, TValue value2) {
        Dictionary<TKey, TValue>? values = new Dictionary<TKey, TValue>(2);
        values.Add(key1, value1);
        values.Add(key2, value2);
        return values;
    }

    /** 创建3个元素的字典 */
    public static Dictionary<TKey, TValue> NewDictionary<TKey, TValue>(TKey key1, TValue value1,
                                                                       TKey key2, TValue value2,
                                                                       TKey key3, TValue value3) {
        Dictionary<TKey, TValue>? values = new Dictionary<TKey, TValue>(4);
        values.Add(key1, value1);
        values.Add(key2, value2);
        values.Add(key3, value3);
        return values;
    }

    #endregion

    /// <summary>
    /// 如果key存在，则返回key关联的value；如果key不存在，则执行给定的action，并将value放入字典；
    /// </summary>
    public static TValue ComputeIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> action) {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        if (dictionary.TryGetValue(key, out TValue value)) {
            return value;
        }
        value = action(key);
        dictionary[key] = value;
        return value;
    }
}