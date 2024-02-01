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
using System.Security;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// Future的异步方法构建器
/// </summary>
/// <typeparam name="T"></typeparam>
public struct AsyncFutureMethodBuilder<T>
{
    /// <summary>
    /// 当任务未同步完成时有值
    /// </summary>
    private IFutureTask<T>? _futureTask;

    /// <summary>
    /// 任务同步失败时有值
    /// </summary>
    private Exception? _ex;
    /// <summary>
    /// 如果futureTask和ex都为null，表示任务已同步完成
    /// </summary>
    private T? _result;

    // 1. Static Create method 
    public static AsyncFutureMethodBuilder<T> Create() {
        return new AsyncFutureMethodBuilder<T>();
    }

    // 2. Start -- 创建后立即调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
        // 由于任务可能同步完成，因此此时捕获StateMachine是不必要的，我们可以在Await的时候捕获其引用
        stateMachine.MoveNext();
    }

    // 3. TaskLike Task property -- 返回给方法调用者
    public ValueFuture<T> Task {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (_futureTask != null) {
                return new ValueFuture<T>(_futureTask.Future);
            }
            if (_ex != null) {
                return ValueFuture<T>.FromException(_ex);
            }
            return ValueFuture<T>.FromResult(_result);
        }
    }

    // 4. SetException -- 同步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        if (_futureTask != null) {
            _futureTask.Future.TrySetException(exception);
        } else {
            this._ex = exception;
        }
    }

    // 5. SetResult -- 同步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) {
        if (_futureTask != null) {
            _futureTask.Future.TrySetResult(result);
        } else {
            this._result = result;
        }
    }

    // 6. AwaitOnCompleted -- 异步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        
        if (_futureTask == null) {
            PromiseTask<T,TStateMachine> promiseTask = new PromiseTask<T, TStateMachine>();
            promiseTask.SetStateMachine(ref stateMachine);
            _futureTask = promiseTask;
        }
        awaiter.OnCompleted(_futureTask.MoveToNext);
    }

    // 6. AwaitUnsafeOnCompleted -- 异步完成时
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        
        if (_futureTask == null) {
            PromiseTask<T,TStateMachine> promiseTask = new PromiseTask<T, TStateMachine>();
            promiseTask.SetStateMachine(ref stateMachine);
            _futureTask = promiseTask;
        }
        awaiter.UnsafeOnCompleted(_futureTask.MoveToNext);
    }

    // 8. SetStateMachine
    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }
}