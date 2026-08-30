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
using System.Threading;
using Wjybxx.Commons.Ex;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 不打印堆栈的取消异常
/// </summary>
public sealed class StacklessCancellationException : OperationCanceledException, NoLogRequiredException
{
    // c# 的异常不适合单例，会导致堆栈冲突
    public static StacklessCancellationException Default => new StacklessCancellationException();

    public StacklessCancellationException() {
    }

    public StacklessCancellationException(string? message) : base(message) {
    }

    public StacklessCancellationException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public StacklessCancellationException(string? message, Exception? innerException, CancellationToken token) : base(message, innerException, token) {
    }

    public StacklessCancellationException(string? message, CancellationToken token) : base(message, token) {
    }

    public StacklessCancellationException(CancellationToken token) : base(token) {
    }

    public override string? StackTrace => null;
}
}