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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于支持await切换到指定executor线程。
/// PS：如果当前已在目标Executor线程，则没有开销。
/// </summary>
public readonly struct ExecutorAwaiter : ICriticalNotifyCompletion
{
    private readonly IExecutor _executor;

    public ExecutorAwaiter(IExecutor executor) {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted => Executors.InEventLoop(_executor);

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此空实现安全 -- 不可手动调用，不会阻塞
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() {
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        _executor.Execute(continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        _executor.Execute(continuation);
    }
}
}