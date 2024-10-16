﻿#region LICENSE

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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 不打印堆栈的取消异常
/// </summary>
public sealed class StacklessCancellationException : BetterCancellationException
{
    // c# 的异常不适合单例，会导致堆栈冲突
    public static StacklessCancellationException Default => new StacklessCancellationException(1);
    public static StacklessCancellationException Timeout => new StacklessCancellationException(CancelCodes.REASON_TIMEOUT);
    public static StacklessCancellationException TriggerCountLimit => new StacklessCancellationException(CancelCodes.REASON_TRIGGER_COUNT_LIMIT);

    public StacklessCancellationException(int code) : base(code) {
    }

    public StacklessCancellationException(int code, string? message) : base(code, message) {
    }

    public StacklessCancellationException(int code, string? message, Exception? innerException) : base(code, message, innerException) {
    }

    public override string? StackTrace => null;

    public static StacklessCancellationException InstOf(int code) {
        return new StacklessCancellationException(code);
    }
}
}