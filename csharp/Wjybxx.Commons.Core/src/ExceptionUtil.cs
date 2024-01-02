#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons;

/// <summary>
/// 异常工具类
/// </summary>
public static class ExceptionUtil
{
    /// <summary>
    /// 获取异常的根
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static Exception? GetRootCause(Exception? ex) {
        List<Exception> list = GetExceptionList(ex);
        return list.Count == 0 ? null : list[list.Count - 1];
    }

    /// <summary>
    /// 获取异常包含的所有异常
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static List<Exception> GetExceptionList(Exception? ex) {
        List<Exception> list = new List<Exception>();
        while (ex != null && !CollectionUtil.ContainsRef(list, ex)) {
            list.Add(ex);
            ex = ex.InnerException;
        }
        return list;
    }

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexOutOfRangeException IndexOutOfRange(int index) {
        return new IndexOutOfRangeException("Index out of range: " + index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexOutOfRangeException IndexOutOfRange(int index, int length) {
        return new IndexOutOfRangeException($"Index out of range: {index}, {length}");
    }

    #endregion
}