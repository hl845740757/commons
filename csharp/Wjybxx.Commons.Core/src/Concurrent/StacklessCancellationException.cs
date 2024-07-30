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
/// 不打印堆栈的取消异常
/// </summary>
public sealed class StacklessCancellationException : BetterCancellationException
{
    public static readonly StacklessCancellationException INST1 = new StacklessCancellationException(1);
    private static readonly StacklessCancellationException INST2 = new StacklessCancellationException(2);
    private static readonly StacklessCancellationException INST3 = new StacklessCancellationException(3);
    private static readonly StacklessCancellationException INST4 = new StacklessCancellationException(4);

    public StacklessCancellationException(int code) : base(code) {
    }

    public StacklessCancellationException(int code, string? message) : base(code, message) {
    }

    public StacklessCancellationException(int code, string? message, Exception? innerException) : base(code, message, innerException) {
    }

    public StacklessCancellationException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public override string? StackTrace => null;

    public static StacklessCancellationException InstOf(int code) {
        return code switch
        {
            1 => INST1,
            2 => INST2,
            3 => INST3,
            4 => INST4,
            _ => new StacklessCancellationException(code)
        };
    }
}
}