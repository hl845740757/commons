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
using System.Runtime.Serialization;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 记录了取消码的异常
/// </summary>
public class BetterCancellationException : OperationCanceledException
{
    /// <summary>
    /// 取消码
    /// </summary>
    public int Code { get; }

    public BetterCancellationException(int code)
        : base(FormatMessage(code, null)) {
        this.Code = CancelCodes.CheckCode(code);
    }

    public BetterCancellationException(int code, string? message)
        : base(FormatMessage(code, message)) {
        this.Code = CancelCodes.CheckCode(code);
    }

    public BetterCancellationException(int code, string? message, Exception? innerException)
        : base(FormatMessage(code, message), innerException) {
        this.Code = CancelCodes.CheckCode(code);
    }

    private static string FormatMessage(int code, string? message) {
        if (message == null) {
            return "The task was canceled, code: " + code;
        }
        return $"The task was canceled, code: {code}, message: {message}";
    }

    /// <summary>
    /// 捕获目标异常 -- 在目标异常的堆栈基础上增加当前堆栈。
    /// 作用：异步任务在重新抛出异常时应当记录当前堆栈，否则会导致用户的代码被中断而没有被记录。
    /// </summary>
    public static BetterCancellationException Capture(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        if (ex is StacklessCancellationException slex) {
            return new BetterCancellationException(slex.Code, slex.Message);
        }
        BetterCancellationException r;
        if (ex is BetterCancellationException ex2) {
            r = new BetterCancellationException(ex2.Code, ex2.Message, ex);
        } else {
            r = new BetterCancellationException(CancelCodes.REASON_DEFAULT, null, ex);
        }
        return r;
    }

    #region serial

    protected BetterCancellationException(SerializationInfo info, StreamingContext context)
        : base(info, context) {
        this.Code = info.GetInt32("code");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context) {
        base.GetObjectData(info, context);
        info.AddValue("code", Code);
    }

    #endregion
}
}