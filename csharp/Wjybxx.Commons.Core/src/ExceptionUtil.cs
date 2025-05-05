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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Wjybxx.Commons.Collections;

#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Wjybxx.Commons
{
/// <summary>
/// 异常工具类
/// </summary>
public static class ExceptionUtil
{
    /// <summary>
    /// 用于恢复异常的堆栈数据，比抛出异常再捕获肯定是更高效的
    /// </summary>
    private static readonly MethodInfo ref_RestoreDispatchState;
    private static readonly FieldInfo ref_DispatchState;

    static ExceptionUtil() {
        ref_RestoreDispatchState = typeof(Exception).GetMethod("RestoreDispatchState", BindingFlags.NonPublic | BindingFlags.Instance)
                                   ?? throw new Exception("Method 'Exception.RestoreDispatchState' not found");
        ref_DispatchState = typeof(ExceptionDispatchInfo).GetField("_dispatchState", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? throw new Exception("Field 'ExceptionDispatchInfo._dispatchState' not found");
    }

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

    /// <summary>
    /// 恢复异常的堆栈
    /// </summary>
    /// <param name="dispatchInfo"></param>
    /// <returns></returns>
    public static Exception RestoreStackTrace(ExceptionDispatchInfo dispatchInfo) {
        if (dispatchInfo == null) throw new ArgumentNullException(nameof(dispatchInfo));
        // 这里会产生装箱，但也比抛出异常再捕获强得多
        object state = ref_DispatchState.GetValue(dispatchInfo);
        ref_RestoreDispatchState.Invoke(dispatchInfo.SourceException, new[] { state });
        return dispatchInfo.SourceException;
    }
}
}