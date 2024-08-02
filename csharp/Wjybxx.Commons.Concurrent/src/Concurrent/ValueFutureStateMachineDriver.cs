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
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
/// <typeparam name="S">状态机类型</typeparam>
internal sealed class ValueFutureStateMachineDriver<T, S> : IValueFutureStateMachineDriver<T> where S : IAsyncStateMachine
{
    private static readonly ConcurrentObjectPool<ValueFutureStateMachineDriver<T, S>> POOL =
        new(() => new ValueFutureStateMachineDriver<T, S>(), driver => driver.Reset(), 50);

    /// <summary>
    /// 任务状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 重入id（归还到池和从池中取出时都加1）
    /// </summary>
    private int _reentryId;
    /// <summary>
    /// 驱动状态机的委托
    /// </summary>
    private readonly Action _moveToNext;
    /// <summary>
    /// 不继承Promise，避免类型测试时bug
    /// </summary>
    private readonly Promise<T> _promise = new Promise<T>();

    private ValueFutureStateMachineDriver() {
        _moveToNext = Run;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stateMachine"></param>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static int SetStateMachine(ref S stateMachine, ref IValueFutureStateMachineDriver<T> driver) {
        ValueFutureStateMachineDriver<T, S> result = POOL.Acquire();
        driver = result; // set driver before copy -- 需要copy到栈
        result._stateMachine = stateMachine; // copy struct... 从栈拷贝到堆，ref也没用，不错一次不知道...
        return result._reentryId++; // 重用时也+1
    }

    public void Run() {
        _stateMachine.MoveNext();
    }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext;

    private void Reset() {
        _reentryId++;
        _stateMachine = default;
        _promise.Reset();
    }

    public ValueFuture VoidFuture {
        get {
            TaskStatus status = _promise.Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture();
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture(_promise.ExceptionNow(false));
                }
                default: {
                    return new ValueFuture(this, _reentryId);
                }
            }
        }
    }

    public ValueFuture<T> Future {
        get {
            TaskStatus status = _promise.Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture<T>(_promise.ResultNow(), null);
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture<T>(default, _promise.ExceptionNow(false));
                }
                default: {
                    return new ValueFuture<T>(this, _reentryId);
                }
            }
        }
    }

    public TaskStatus GetStatus(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        return _promise.Status;
    }

    public Exception GetException(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        return _promise.ExceptionNow(false);
    }

    public T GetResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = _promise.Status;
        if (!status.IsCompleted()) {
            throw new IllegalStateException("Task has not completed");
        }

        T r = default;
        Exception? ex = null;
        if (status == TaskStatus.Success) {
            r = _promise.ResultNow();
        } else {
            ex = _promise.ExceptionNow(false);
        }
        // GetResult以后归还到池
        POOL.Release(this);

        if (ex != null) {
            throw status == TaskStatus.Cancelled ? ex : new CompletionException(null, ex);
        }
        return r;
    }

    public bool TrySetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        return _promise.TrySetResult(result);
    }

    public bool TrySetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        return _promise.TrySetException(cause);
    }

    public bool TrySetCancelled(int reentryId, int cancelCode) {
        ValidateReentryId(reentryId);
        return _promise.TrySetCancelled(cancelCode);
    }

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state, IExecutor? executor, int options = 0) {
        ValidateReentryId(reentryId);
        if (executor != null) {
            _promise.OnCompletedAsync(executor, continuation, state, options);
        } else {
            _promise.OnCompleted(continuation, state);
        }
    }

    public void OnCompletedVoid(int reentryId, IPromise<int> promise) {
        ValidateReentryId(reentryId);
        _promise.OnCompleted(setVoidInvoker, promise);
    }

    public void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise) {
        ValidateReentryId(reentryId);
        IPromise<T>.SetPromise(promise, _promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateReentryId(int reentryId, bool ignoreReentrant = false) {
        if (ignoreReentrant || reentryId == this._reentryId) {
            return;
        }
        throw new Exception("ValueFutureDriver has been reused");
    }

    private static readonly Action<IFuture<T>, object> setVoidInvoker = (future, state) => {
        IPromise<int> promise = (IPromise<int>)state;
        if (future.IsSucceeded) {
            promise.TrySetResult(0);
        } else {
            promise.TrySetException(future.ExceptionNow(false));
        }
    };
}
}