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
///
/// PS：该类型由于不复用，因此可直接继承Promise。
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
/// <typeparam name="S">状态机类型</typeparam>
internal sealed class FutureStateMachineDriver<T, S> : Promise<T>, IFutureStateMachineDriver<T> where S : IAsyncStateMachine
{
    /// <summary>
    /// 任务状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 驱动状态机的委托
    /// </summary>
    private Action _moveToNext;

    private FutureStateMachineDriver() {
        _moveToNext = Run;
    }

    public static void SetStateMachine(ref S stateMachine, out IFutureStateMachineDriver<T> driver) {
        FutureStateMachineDriver<T, S> result = new FutureStateMachineDriver<T, S>();
        // driver是builder的属性，而builder是状态机的属性，需要在拷贝状态机之前完成初始化
        // init builder before copy state machine
        driver = result;

        // copy struct... 从栈拷贝到堆，此后栈上的状态机将被丢弃
        result._stateMachine = stateMachine;
    }

    public void Run() {
        _stateMachine.MoveNext();
    }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext;

    public IFuture VoidFuture => this;

    public IFuture<T> Future => this;

    public TaskStatus GetStatus(int reentryId, bool ignoreReentrant = false) {
        return Status;
    }

    public Exception GetException(int reentryId, bool ignoreReentrant = false) {
        return ExceptionNow(false);
    }

    public void GetVoidResult(int reentryId, bool ignoreReentrant = false) {
        ThrowIfFailedOrCancelled();
    }

    public T GetResult(int reentryId, bool ignoreReentrant = false) {
        return ResultNow();
    }

    public bool TrySetResult(int reentryId, T result) {
        return TrySetResult(result);
    }

    public bool TrySetException(int reentryId, Exception cause) {
        return TrySetException(cause);
    }

    public bool TrySetCancelled(int reentryId, int cancelCode) {
        return TrySetCancelled(cancelCode);
    }

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state, IExecutor? executor, int options = 0) {
        if (executor != null) {
            OnCompletedAsync(executor, continuation, state, options);
        } else {
            OnCompleted(continuation, state);
        }
    }

    public void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise) {
        Executors.SetPromise(promise, this);
    }

    public void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise) {
        Executors.SetVoidPromise(promise, this);
    }
}
}