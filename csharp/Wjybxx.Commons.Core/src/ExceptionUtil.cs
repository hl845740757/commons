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
    /// 捕获异常堆栈
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static ExceptionDispatchInfo? TryCapture(Exception? ex) {
        return ex == null ? null : ExceptionDispatchInfo.Capture(ex);
    }

    /// <summary>
    /// 恢复异常的堆栈
    /// </summary>
    /// <param name="dispatchInfo"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static Exception RestoreStackTrace(ExceptionDispatchInfo dispatchInfo) {
        if (dispatchInfo == null) throw new ArgumentNullException(nameof(dispatchInfo));
        // c# 没有开放接口直接恢复堆栈，我们通过重新抛出异常来恢复堆栈
        try {
            dispatchInfo.Throw();
            return dispatchInfo.SourceException;
        }
        catch (Exception e) {
            return e;
        }
    }
}
}