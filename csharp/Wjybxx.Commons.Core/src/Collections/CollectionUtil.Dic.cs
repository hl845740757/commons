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
using System.Runtime.InteropServices;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 该类用于增加字典工具方法
/// </summary>
public static partial class CollectionUtil
{
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary, TKey key, out bool exists) where TKey : notnull {
        return ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out exists);
    }
#endif

    /// <summary>
    /// 如果key存在，则返回key关联的value；如果key不存在，则执行给定的action，并将value放入字典；
    /// (适用于非并发字典)
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
}