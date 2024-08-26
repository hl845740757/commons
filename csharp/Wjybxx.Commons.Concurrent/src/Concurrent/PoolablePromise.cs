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
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口，也可能导致类型测试时的混乱。
/// 2.统一在用户获得结果后触发回收。
/// 3.该实现并不是严格线程安全的，但在使用<see cref="ValueFuture{T}"/>的情况下是安全的。
/// </summary>
/// <typeparam name="T"></typeparam>
internal class PoolablePromise<T> : IPoolablePromise<T>
{
    /// <summary>
    /// 重入id（归还到池和从池中取出时都加1）
    /// </summary>
    private int _reentryId;
    /// <summary>
    /// 存储结果的Promise（未特殊实现结构体类型的Promise，不想重复编码）
    /// </summary>
    protected readonly Promise<T> _promise = new Promise<T>();

    /// <summary>
    /// 当前重入id
    /// </summary>
    public int ReentryId => _reentryId;

    /// <summary>
    /// 增加重入id(重用对象时调用)
    /// </summary>
    /// <returns>增加后的值</returns>
    public int IncReentryId() {
        return ++_reentryId;
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public virtual void Reset() {
        _reentryId++;
        _promise.Reset();
    }

    /// <summary>
    /// 用户已正常获取结果信息，可以尝试回收
    /// </summary>
    protected virtual void PrepareToRecycle() {
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
        Exception ex = _promise.ExceptionNow(false);
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }
        return ex;
    }

    public void GetVoidResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = _promise.Status;
        if (!status.IsCompleted()) {
            throw new IllegalStateException("Task has not completed");
        }

        Exception? ex = null;
        if (status != TaskStatus.Success) {
            ex = _promise.ExceptionNow(false);
        }
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

        if (ex != null) {
            throw status == TaskStatus.Cancelled ? ex : new CompletionException(null, ex);
        }
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
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

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

    public void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise) {
        ValidateReentryId(reentryId);
        Executors.SetVoidPromise(promise, _promise);
    }

    public void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise) {
        ValidateReentryId(reentryId);
        Executors.SetPromise(promise, _promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateReentryId(int reentryId, bool ignoreReentrant = false) {
        if (ignoreReentrant || reentryId == this._reentryId) {
            return;
        }
        throw new IllegalStateException("promise has been reused");
    }
}
}