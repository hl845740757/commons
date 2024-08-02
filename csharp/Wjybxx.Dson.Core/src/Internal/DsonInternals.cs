#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Internal
{
/// <summary>
/// Dson内部工具类
/// </summary>
internal static class DsonInternals
{
    /** 上下文缓存池大小 */
    public const int CONTEXT_POOL_SIZE = 64;

    /** 是否设置了任意bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnySet(int value, int mask) {
        return (value & mask) != 0;
    }

    /** 是否设置了mask关联的所有bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(int value, int mask) {
        return (value & mask) == mask;
    }

    /** Name是否是字符串类型 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStringKey<TName>() {
        if (typeof(TName) == typeof(string)) {
            return true;
        }
        if (typeof(TName) == typeof(FieldNumber)) {
            return false;
        }
        throw new InvalidCastException("Cant cast TName to string or FieldNumber, type: " + typeof(TName));
    }

    #region 集合Util

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGenericDictionary<TK, DsonValue> NewLinkedDictionary<TK>(int capacity = 0) {
        return new LinkedDictionary<TK, DsonValue>(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGenericDictionary<TK, DsonValue> NewLinkedDictionary<TK>(IDictionary<TK, DsonValue> src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        var dictionary = new LinkedDictionary<TK, DsonValue>();
        dictionary.PutAll(src);
        return dictionary;
    }

    public static string ToString<T>(ICollection<T> collection) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        StringBuilder sb = new StringBuilder(64);
        sb.Append('[');
        bool first = true;
        foreach (T value in collection) {
            if (first) {
                first = false;
            } else {
                sb.Append(',');
            }
            if (value == null) {
                sb.Append("null");
            } else {
                sb.Append(value.ToString());
            }
        }
        sb.Append(']');
        return sb.ToString();
    }

    #endregion
}
}