#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
public readonly struct CancelTokenAwaiter : ICriticalNotifyCompletion
{
    private readonly ICancelToken _cts;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancelToken">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    public CancelTokenAwaiter(ICancelToken cancelToken, IExecutor? executor = null, int options = 0) {
        _cts = cancelToken ?? throw new ArgumentNullException(nameof(cancelToken));
        _executor = executor;
        _options = options;
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted {
        get {
            if (!_cts.IsRequested) return false;
            return ExecutorUtil.IsInlinable(_executor, _options);
        }
    }

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetResult() {
        return _cts.CancelCode;
    }

    private static readonly Action<object> invoker = state => ((Action)state).Invoke();

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, int extraOptions = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _cts.ThenRun(continuation, state, _options | extraOptions);
        } else {
            _cts.ThenRunAsync(_executor, continuation, state, _options | extraOptions);
        }
    }
}
}