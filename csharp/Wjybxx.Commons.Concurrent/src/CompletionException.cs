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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该异常表示在计算的过程中出现异常
/// 
/// 注：C#更习惯于抛出原始的计算异常和堆栈，而不记录中间过程可能执行的逻辑；如果中间过程期望附加自己的对象，则需要手动封装。
/// (不封装异常确实更易于理解和处理一些，使用Java的CompletableFuture的时候就有此疑惑...)
///（理论上取消也可以记录堆栈，但取消属于意料之中的异常，记录堆栈的成本较高 -- 目前可以记录信息和传递CTS）
/// </summary>
public class CompletionException : Exception
{
    public CompletionException() {
    }

    public CompletionException(string? message) : base(message) {
    }

    public CompletionException(string? message, Exception? innerException) : base(message, innerException) {
    }
}
}