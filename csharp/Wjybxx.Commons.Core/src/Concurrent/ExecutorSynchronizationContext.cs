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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于适配为<see cref="SynchronizationContext"/>。
///
/// C#的这个同步上下文设计真的跟屎一样，线程控制麻烦的要死
/// </summary>
public class ExecutorSynchronizationContext : SynchronizationContext
{
    private readonly IExecutor _executor;

    public ExecutorSynchronizationContext(IExecutor executor) {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// 用于await获取
    /// </summary>
    public IExecutor Executor => _executor;

    /// <summary>
    /// 
    /// </summary>
    public new static ExecutorSynchronizationContext? Current => SynchronizationContext.Current as ExecutorSynchronizationContext;

    /// <summary>
    /// 获取用于执行await回调的线程
    /// </summary>
    /// <param name="executor"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IExecutor? GetAwaitExecutor(IExecutor? executor) {
        if (executor != null) return executor;
        var context = SynchronizationContext.Current as ExecutorSynchronizationContext;
        return context != null ? context._executor : null;
    }

    public override void Post(SendOrPostCallback d, object? state) {
        // 不能随意内联，否则可能导致时序错误
        _executor.Execute(new PostCallbackWrapper(d, state));
    }

    private class PostCallbackWrapper : ITask
    {
        private readonly SendOrPostCallback _callback;
        private readonly object? _state;

        public PostCallbackWrapper(SendOrPostCallback callback, object? state) {
            this._callback = callback;
            this._state = state;
        }

        public void Run() {
            _callback(_state);
        }

        public int Options => 0;
    }
}
}