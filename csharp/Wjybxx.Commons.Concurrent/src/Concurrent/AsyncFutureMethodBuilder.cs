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

public struct AsyncFutureMethodBuilder<T>
{
    private IPromise<T> promise;

    public AsyncFutureMethodBuilder(IPromise<T> promise) {
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));
    }

    // 1. Static Create method.
    public static AsyncFutureMethodBuilder<T> Create() {
        return new AsyncFutureMethodBuilder<T>(new Promise<T>());
    }

    // 2. TaskLike Task property.
    public IFuture<T> Task {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => promise;
    }

    // 3. SetException -- 同步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        promise.TrySetException(exception);
    }

    // 4. SetResult -- 同步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) {
        promise.TrySetResult(result);
    }

    // 5. AwaitOnCompleted -- 异步完成时
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    // 6. AwaitUnsafeOnCompleted -- 异步完成时
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
    }

    // 7. Start
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
        stateMachine.MoveNext();
    }

    // 8. SetStateMachine
    public void SetStateMachine(IAsyncStateMachine stateMachine) {
        // don't use boxed stateMachine.
    }
}