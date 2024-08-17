#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
/// 提供给用户的工具类
/// </summary>
public static class ValueFutureTask
{
    public static ValueFuture<T> Submit<T>(IExecutor executor, in TaskBuilder<T> builder) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(builder.Task, builder.Context, builder.Options, builder.Type);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask); // 可能会立即完成，因此需要提前保存future
        return promise;
    }

    #region submit func

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, null, options, TaskBuilder.TYPE_FUNC);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, ICancelToken cancelToken, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, cancelToken, options, TaskBuilder.TYPE_FUNC);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<IContext, T> task, IContext ctx, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, ctx, options, TaskBuilder.TYPE_FUNC_CTX);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    #endregion

    #region submit action

    public static ValueFuture SubmitAction(IExecutor executor, Action task, int options = 0) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, null, options, TaskBuilder.TYPE_ACTION);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture SubmitAction(IExecutor executor, Action task, ICancelToken cancelToken, int options) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, cancelToken, options, TaskBuilder.TYPE_ACTION);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture SubmitAction(IExecutor executor, Action<IContext> task, IContext ctx, int options) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, ctx, options, TaskBuilder.TYPE_ACTION_CTX);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    #endregion
}

/// <summary>
/// 用于封装用户的任务，并返回给用户<see cref="ValueFuture{T}"/>，
/// </summary>
/// <typeparam name="T"></typeparam>
internal class ValueFutureTask<T> : PromiseTask<T>, ITaskDriver<T>
{
    private static readonly ConcurrentObjectPool<ValueFutureTask<T>> POOL =
        new(() => new ValueFutureTask<T>(), task => task.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolConfig.TaskType.ValueFutureTask));

    /// <summary>
    /// 重入id（归还到池和从池中取出时都加1）
    /// </summary>
    private int _reentryId;

    private ValueFutureTask() {
        promise = new Promise<T>();
    }

    public static ValueFutureTask<T> Create(object action, object? ctx, int options, int taskType) {
        ValueFutureTask<T> futureTask = POOL.Acquire();
        futureTask.Init(action, ctx, options, futureTask.promise, taskType);
        return futureTask;
    }

    private void Reset() {
        Promise<T> promise = (Promise<T>)this.promise;
        try {
            _reentryId++;
            promise.Reset();
            base.Clear();
        }
        finally {
            this.promise = promise;
        }
    }

    /// <summary>
    /// 该方法可能被EventLoop调用，但我们空实现
    /// </summary>
    public override void Clear() {
    }

    public ValueFuture VoidFuture {
        get {
            TaskStatus status = promise.Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture();
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture(promise.ExceptionNow(false));
                }
                default: {
                    return new ValueFuture(this, _reentryId);
                }
            }
        }
    }

    public ValueFuture<T> Future {
        get {
            TaskStatus status = promise.Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture<T>(promise.ResultNow(), null);
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture<T>(default, promise.ExceptionNow(false));
                }
                default: {
                    return new ValueFuture<T>(this, _reentryId);
                }
            }
        }
    }

    public TaskStatus GetStatus(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        return promise.Status;
    }

    public Exception GetException(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        Exception ex = promise.ExceptionNow(false);
        // GetResult以后归还到池
        POOL.Release(this);
        return ex;
    }

    public T GetResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = promise.Status;
        if (!status.IsCompleted()) {
            throw new IllegalStateException("Task has not completed");
        }

        T r = default;
        Exception? ex = null;
        if (status == TaskStatus.Success) {
            r = promise.ResultNow();
        } else {
            ex = promise.ExceptionNow(false);
        }
        // GetResult以后归还到池
        POOL.Release(this);

        if (ex != null) {
            throw status == TaskStatus.Cancelled ? ex : new CompletionException(null, ex);
        }
        return r;
    }

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state, IExecutor? executor, int options = 0) {
        ValidateReentryId(reentryId);
        if (executor != null) {
            promise.OnCompletedAsync(executor, continuation, state, options);
        } else {
            promise.OnCompleted(continuation, state);
        }
    }

    public void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise) {
        ValidateReentryId(reentryId);
        IPromise.SetVoidPromise(promise, this.promise);
    }

    public void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise) {
        ValidateReentryId(reentryId);
        IPromise<T>.SetPromise(promise, this.promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateReentryId(int reentryId, bool ignoreReentrant = false) {
        if (ignoreReentrant || reentryId == this._reentryId) {
            return;
        }
        throw new Exception("ValueFutureDriver has been reused");
    }
}
}