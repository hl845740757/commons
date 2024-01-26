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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

#pragma warning disable CS1591

namespace Wjybxx.Commons;

public struct AsyncFutureMethodBuilder<T>
{
    private IPromise<T> promise;

    // 1. Static Create method.
    public static AsyncFutureMethodBuilder<T> Create() {
        return default;
    }

    // 2. TaskLike Task property.
    public UniTask<T> Task {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (runnerPromise != null) {
                return runnerPromise.Task;
            } else if (ex != null) {
                return UniTask.FromException<T>(ex);
            } else {
                return UniTask.FromResult(result);
            }
        }
    }

    // 3. SetException
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception) {
        if (runnerPromise == null) {
            ex = exception;
        } else {
            runnerPromise.SetException(exception);
        }
    }

    // 4. SetResult
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result) {
        if (runnerPromise == null) {
            this.result = result;
        } else {
            runnerPromise.SetResult(result);
        }
    }

    // 5. AwaitOnCompleted
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (runnerPromise == null) {
            AsyncUniTask<TStateMachine, T>.SetStateMachine(ref stateMachine, ref runnerPromise);
        }

        awaiter.OnCompleted(runnerPromise.MoveNext);
    }

    // 6. AwaitUnsafeOnCompleted
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        if (runnerPromise == null) {
            AsyncUniTask<TStateMachine, T>.SetStateMachine(ref stateMachine, ref runnerPromise);
        }

        awaiter.UnsafeOnCompleted(runnerPromise.MoveNext);
    }

    // 7. Start
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
        stateMachine.MoveNext();
    }
}