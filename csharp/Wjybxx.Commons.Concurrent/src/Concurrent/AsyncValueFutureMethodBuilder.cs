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
using System.Runtime.CompilerServices;
using System.Security;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Future的异步方法构建器
///
/// ps：AsyncMethodBuilder的作用只有一个：封装StateMachine的回调（驱动StateMachine），接收StateMachine的执行结果。
/// </summary>
public struct AsyncValueFutureMethodBuilder
{
    /// <summary>
    /// 当任务异步完成时有值
    /// 
    /// ps:如果task和ex都为null，表示任务已同步完成
    /// </summary>
    private IValueFutureStateMachineTask<int> _task;
    /// <summary>
    /// task绑定的重入id
    /// </summary>
    private int reentryId;
    /// <summary>
    /// 任务失败的原因 -- 任务同步失败时有值
    /// </summary>
    private Exception? _ex;

    // 1. Static Create method 
    public static AsyncValueFutureMethodBuilder Create() {
        return new AsyncValueFutureMethodBuilder();
    }

    // 2. Start -- 创建后立即调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
        // 由于任务可能同步完成，因此此时捕获StateMachine是不必要的，我们可以在Await的时候捕获其引用
        stateMachine.MoveNext();
    }

    // 3. TaskLike Task property -- 返回给方法调用者；Start后调用；其实命名Future更自然
    public ValueFuture Task {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (_task != null) {
                return _task.VoidFuture;
            }
            if (_ex != null) {
                return ValueFuture.FromException(_ex);
            }
            return ValueFuture.FromResult();
        }
    }

    // 4. SetException -- 同步或异步完成时；接收StateMachine的结果
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        if (_task != null) {
            _task.TrySetException(reentryId, exception);
        } else {
            this._ex = exception;
        }
    }

    // 5. SetResult -- 同步或异步完成时；接收StateMachine的结果
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult() {
        if (_task != null) {
            _task.TrySetResult(reentryId, 0);
        }
    }

    // 6. AwaitOnCompleted -- 未同步完成时/需要异步执行时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_task == null) {
            ValueFutureStateMachineTask<int, TStateMachine>.SetStateMachine(ref stateMachine, out _task, out reentryId);
        }
        awaiter.OnCompleted(_task.MoveToNext);
    }

    // 6. AwaitUnsafeOnCompleted -- 未同步完成时/需要异步执行时
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_task == null) {
            ValueFutureStateMachineTask<int, TStateMachine>.SetStateMachine(ref stateMachine, out _task, out reentryId);
        }
        awaiter.UnsafeOnCompleted(_task.MoveToNext);
    }

    // 8. SetStateMachine
    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }
}

/// <summary>
/// Future的异步方法构建器
/// </summary>
/// <typeparam name="T"></typeparam>
public struct AsyncValueFutureMethodBuilder<T>
{
    /// <summary>
    /// 当任务异步完成时有值
    ///
    /// ps:如果task和ex都为null，表示任务已同步完成
    /// </summary>
    private IValueFutureStateMachineTask<T>? _task;
    /// <summary>
    /// task绑定的重入id
    /// </summary>
    private int reentryId;

    /// <summary>
    /// 任务失败的原因 -- 任务同步失败时有值
    /// </summary>
    private Exception? _ex;
    /// <summary>
    /// 任务的执行结果 -- 任务同步成功时有值
    /// </summary>
    private T? _result;

    // 1. Static Create method 
    public static AsyncValueFutureMethodBuilder<T> Create() {
        return new AsyncValueFutureMethodBuilder<T>();
    }

    // 2. Start -- 创建后立即调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
        // 由于任务可能同步完成，因此此时捕获StateMachine是不必要的，我们可以在Await的时候捕获其引用
        stateMachine.MoveNext();
    }

    // 3. TaskLike Task property -- 返回给方法调用者；Start后调用；其实命名Future更自然
    public ValueFuture<T> Task {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (_task != null) {
                return _task.Future;
            }
            if (_ex != null) {
                return ValueFuture<T>.FromException(_ex);
            }
            return ValueFuture<T>.FromResult(_result);
        }
    }

    // 4. SetException -- 同步或异步完成时；接收StateMachine的结果
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        if (_task != null) {
            _task.TrySetException(reentryId, exception);
        } else {
            this._ex = exception;
        }
    }

    // 5. SetResult -- 同步或异步完成时；接收StateMachine的结果
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) {
        if (_task != null) {
            _task.TrySetResult(reentryId, result);
        } else {
            this._result = result;
        }
    }

    // 6. AwaitOnCompleted -- 未同步完成时/需要异步执行时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_task == null) {
            ValueFutureStateMachineTask<T, TStateMachine>.SetStateMachine(ref stateMachine, out _task, out reentryId);
        }
        awaiter.OnCompleted(_task.MoveToNext);
    }

    // 6. AwaitUnsafeOnCompleted -- 未同步完成时/需要异步执行时
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (_task == null) {
            ValueFutureStateMachineTask<T, TStateMachine>.SetStateMachine(ref stateMachine, out _task, out reentryId);
        }
        awaiter.UnsafeOnCompleted(_task.MoveToNext);
    }

    // 8. SetStateMachine
    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }
}
}