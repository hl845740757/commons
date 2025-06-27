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
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Ext;

namespace Wjybxx.Dson.Internal
{
/// <summary>
/// Dson内部工具类
/// </summary>
internal static class DsonInternals
{
    /** 上下文缓存池大小 */
    public const int CONTEXT_POOL_SIZE = 256;

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
        if (typeof(TName) == typeof(int)) {
            return false;
        }
        throw new InvalidCastException("Cant cast TName to string or int, type: " + typeof(TName));
    }
}
}