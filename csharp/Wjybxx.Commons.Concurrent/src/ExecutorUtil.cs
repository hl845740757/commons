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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 并发工具类
/// </summary>
public static class ExecutorUtil
{
    #region exception

    /// <summary>
    ///  异步任务总是使用<see cref="CompletionException"/>包装异常，我们需要找到原始异常
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static Exception UnwrapCompletionException(Exception ex) {
        while (ex is CompletionException && ex.InnerException != null) {
            ex = ex.InnerException;
        }
        return ex;
    }

    #endregion

    #region set-future

    public static void SetPromise<TResult>(TaskCompletionSource<TResult> promise, Task<TResult> task) {
        TaskHelper<TResult>.SetPromise(promise, task);
    }

    public static void FlatSetPromise<TResult>(TaskCompletionSource<TResult> promise, Task<Task<TResult>> task) {
        TaskHelper<TResult>.FlatSetPromise(promise, task);
    }

    public static void SetPromise<T>(IPromise<T> promise, IFuture<T> task) {
        PromiseHelper<T>.SetPromise(promise, task);
    }

    public static void FlatSetPromise<T>(IPromise<T> promise, IFuture<IFuture<T>> task) {
        PromiseHelper<T>.FlatSetPromise(promise, task);
    }

    /// <summary>
    /// 该框架统一使用int代替void。
    /// </summary>
    /// <param name="promise"></param>
    /// <param name="task"></param>
    public static void SetVoidPromise(IPromise<int> promise, IFuture task) {
        PromiseHelper.SetVoidPromise(promise, task);
    }

    public static void FlatSetVoidPromise(IPromise<int> promise, IFuture<IFuture> task) {
        PromiseHelper.FlatSetVoidPromise(promise, task);
    }

#if NET6_0_OR_GREATER
    public static Task ToTask(IFuture future) {
        return TaskConverterHelper.ToTask(future);
    }
#endif

    public static Task<T> ToTask<T>(IFuture<T> future) {
        return TaskConverterHelper<T>.ToTask(future);
    }

    public static IFuture<T> ToFuture<T>(Task<T> task) {
        return TaskConverterHelper<T>.ToFuture(task);
    }

    public static IFuture ToFuture(Task task) {
        return TaskConverterHelper.ToFuture(task);
    }

    #endregion

    #region system-task

    /** 任务是否失败或被取消 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFailedOrCancelled(this Task task) {
        return task.IsCanceled || task.IsFaulted;
    }

    /** 用于忽略警告 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forget(this Task task) {
    }

    /** 用于忽略警告 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forget(this IFuture task) {
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaiter
    /// 
    /// c#的编译器并未支持该功能，因此需要用户显式调用该方法再await，示例如下：
    /// <code>
    ///     // await后的代码将在eventLoop线程执行
    ///     await future.GetAwaitable(eventLoop); 
    /// 
    ///     // 如果future是在eventLoop线程完成的，则同步执行await后的代码，不通过提交异步任务切换线程 
    ///     await future.GetAwaitable(eventLoop, TaskOption.STAGE_TRY_INLINE);
    /// </code>
    /// </summary>
    /// <param name="task">要等待的Task</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskAwaitable GetAwaitable(this Task task, IExecutor executor, int options = 0) {
        return new TaskAwaitable(task, executor, options);
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaiter
    /// 
    /// c#的编译器并未支持该功能，因此需要用户显式调用该方法再await，示例如下：
    /// <code>
    ///     // await后的代码将在eventLoop线程执行
    ///     await future.GetAwaitable(eventLoop); 
    /// 
    ///     // 如果future是在eventLoop线程完成的，则同步执行await后的代码，不通过提交异步任务切换线程 
    ///     await future.GetAwaitable(eventLoop, TaskOption.STAGE_TRY_INLINE);
    /// </code>
    /// </summary>
    /// <param name="task">要等待的Task</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskAwaitable<T> GetAwaitable<T>(this Task<T> task, IExecutor executor, int options = 0) {
        return new TaskAwaitable<T>(task, executor, options);
    }

    #endregion

    #region factory

    public static IPromise<T> NewPromise<T>(IExecutor? executor = null) {
        return new Promise<T>(executor);
    }

    public static IPromise<int> NewPromise(IExecutor? executor = null) {
        return new Promise<int>(executor);
    }

    public static FutureCombiner NewCombiner() {
        return new FutureCombiner();
    }

    #endregion

    #region submit

    public static ValueFuture<T> Submit<T>(IExecutor executor, in TaskBuilder<T> builder) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfBuilder(in builder, promise));
        return promise.Future;
    }

    // submit 方法不能定义为扩展方法，因为Promise有区别

    #region submit func

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfFunction(task, null, options, promise));
        return promise.Future;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, ICancelToken? cancelToken, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfFunction(task, cancelToken, options, promise));
        return promise.Future;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<object, T> task, object? ctx, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfFunction(task, ctx, options, promise));
        return promise.Future;
    }

    #endregion

    #region submit action

    public static ValueFuture SubmitAction(IExecutor executor, Action task, int options = 0) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(executor);
        executor.Execute(PromiseTask.OfAction(task, null, options, promise));
        return promise.VoidFuture;
    }

    public static ValueFuture SubmitAction(IExecutor executor, Action task, ICancelToken? cancelToken, int options) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(executor);
        executor.Execute(PromiseTask.OfAction(task, cancelToken, options, promise));
        return promise.VoidFuture;
    }

    public static ValueFuture SubmitAction(IExecutor executor, Action<object> task, object? ctx, int options) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(executor);
        executor.Execute(PromiseTask.OfAction(task, ctx, options, promise));
        return promise.VoidFuture;
    }

    #endregion

    #region execute

    public static void Execute(this IExecutor executor, Action action, ICancelToken? cancelToken, int options) {
        ITask task = ExecutorCoreUtil.ToTask(action, cancelToken, options);
        executor.Execute(task);
    }

    public static void Execute(this IExecutor executor, Action<object> action, object? ctx, int options) {
        ITask task = ExecutorCoreUtil.ToTask(action, ctx, options);
        executor.Execute(task);
    }

    public static void Execute(this IExecutor executor, Action action, CancellationToken cancellationToken, int options = 0) {
        ITask task = ExecutorCoreUtil.ToTask(action, cancellationToken, options);
        executor.Execute(task);
    }

    #endregion

    #endregion

    #region all

    public static IFuture<object> AnyOf(IEnumerable<IFuture> futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .AnyOf();
    }

    public static IFuture<object> AllOf(IEnumerable<IFuture> futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .SelectAll();
    }

    #endregion

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSuppressible(this SuppressedTypes self, TaskStatus status) {
        return (self.HasFlag(SuppressedTypes.Cancellation) && status == TaskStatus.Cancelled)
               || (self.HasFlag(SuppressedTypes.Error) && status == TaskStatus.Failed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSuppressible(this SuppressedTypes self, Exception ex) {
        if (ex is OperationCanceledException) {
            return self.HasFlag(SuppressedTypes.Cancellation);
        }
        return self.HasFlag(SuppressedTypes.Error);
    }

    #endregion


    #region future_helper

    private static class PromiseHelper
    {
        private static readonly Action<IFuture, object> _invokerSetVoidPromise = (future, state) => {
            IPromise<int> promise = (IPromise<int>)state;
            SetVoidPromise(promise, future);
        };

        private static readonly Action<IFuture<IFuture>, object> _invokerFlatSetPromise = (future, state) => {
            IPromise<int> promise = (IPromise<int>)state;
            FlatSetVoidPromise(promise, future);
        };

        public static void SetVoidPromise(IPromise<int> promise, IFuture task) {
            switch (task.Status) {
                case TaskStatus.Success: {
                    promise.TrySetResult(0);
                    break;
                }
                case TaskStatus.Failed:
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerSetVoidPromise, promise, TaskOptions.STAGE_UNCANCELLABLE_CTX);
                    break;
                }
            }
        }

        public static void FlatSetVoidPromise(IPromise<int> promise, IFuture<IFuture> task) {
            switch (task.Status) {
                case TaskStatus.Success: {
                    SetVoidPromise(promise, task.ResultNow());
                    break;
                }
                case TaskStatus.Failed:
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerFlatSetPromise, promise, TaskOptions.STAGE_UNCANCELLABLE_CTX);
                    break;
                }
            }
        }
    }

    private static class PromiseHelper<T>
    {
        private static readonly Action<IFuture<T>, object> _invokerSetPromise = (future, state) => {
            IPromise<T> promise = (IPromise<T>)state;
            SetPromise(promise, future);
        };

        private static readonly Action<IFuture<IFuture<T>>, object> _invokerFlatSetPromise = (future, state) => {
            IPromise<T> promise = (IPromise<T>)state;
            FlatSetPromise(promise, future);
        };

        public static void SetPromise(IPromise<T> promise, IFuture<T> task) {
            switch (task.Status) {
                case TaskStatus.Success: {
                    promise.TrySetResult(task.ResultNow());
                    break;
                }
                case TaskStatus.Failed:
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerSetPromise, promise, TaskOptions.STAGE_UNCANCELLABLE_CTX);
                    break;
                }
            }
        }

        public static void FlatSetPromise(IPromise<T> promise, IFuture<IFuture<T>> task) {
            switch (task.Status) {
                case TaskStatus.Success: {
                    SetPromise(promise, task.ResultNow());
                    break;
                }
                case TaskStatus.Failed:
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerFlatSetPromise, promise, TaskOptions.STAGE_UNCANCELLABLE_CTX);
                    break;
                }
            }
        }
    }

    private static class TaskHelper<T>
    {
        private static readonly Action<Task<T>, object> _invokerSetPromise = (future, state) => {
            TaskCompletionSource<T> promise = (TaskCompletionSource<T>)state;
            SetPromise(promise, future);
        };

        private static readonly Action<Task<Task<T>>, object> _invokerFlatSetPromise = (future, state) => {
            TaskCompletionSource<T> promise = (TaskCompletionSource<T>)state;
            FlatSetPromise(promise, future);
        };

        public static void SetPromise(TaskCompletionSource<T> promise, Task<T> task) {
            if (task.IsCompleted) {
                if (task.IsCompletedSuccessfully) {
                    promise.TrySetResult(task.Result);
                } else if (task.IsFaulted) {
                    promise.TrySetException(task.Exception!);
                } else {
                    promise.TrySetCanceled();
                }
            } else {
                task.ContinueWith(_invokerSetPromise, promise);
            }
        }

        public static void FlatSetPromise(TaskCompletionSource<T> promise, Task<Task<T>> task) {
            if (task.IsCompleted) {
                if (task.IsCompletedSuccessfully) {
                    SetPromise(promise, task.Result);
                } else if (task.IsFaulted) {
                    promise.TrySetException(task.Exception!);
                } else {
                    promise.TrySetCanceled();
                }
            } else {
                task.ContinueWith(_invokerFlatSetPromise, promise);
            }
        }
    }

    private static class TaskConverterHelper
    {
        #region converter

#if NET6_0_OR_GREATER
        private static readonly Action<IFuture, object> _invokerToTask = (future, state) => {
            TaskCompletionSource cts = (TaskCompletionSource)state;
            switch (future.Status) {
                case TaskStatus.Success: {
                    cts.TrySetResult();
                    break;
                }
                case TaskStatus.Cancelled: {
                    cts.TrySetCanceled();
                    break;
                }
                case TaskStatus.Failed: {
                    cts.TrySetException(future.ExceptionNow());
                    break;
                }
                default: {
                    throw new AssertionError();
                }
            }
        };

        public static Task ToTask(IFuture future) {
            switch (future.Status) {
                case TaskStatus.Success: {
                    return Task.FromResult(future.ResultNow());
                }
                case TaskStatus.Cancelled: {
                    return Task.FromCanceled(default);
                }
                case TaskStatus.Failed: {
                    return Task.FromException(future.ExceptionNow());
                }
                default: {
                    TaskCompletionSource source = new TaskCompletionSource();
                    future.OnCompleted(_invokerToTask, source);
                    return source.Task;
                }
            }
        }
#endif

        ////////////////////////////////////
        private static readonly Action<Task, object> _invokerToFuture = (task, state) => {
            IPromise<int> promise = (IPromise<int>)state;
            if (task.IsCompletedSuccessfully) {
                promise.TrySetResult(0);
            } else if (task.IsFaulted) {
                promise.TrySetException(task.Exception!);
            } else {
                promise.TrySetCancelled(CancelCodes.REASON_DEFAULT);
            }
        };

        public static IFuture ToFuture(Task task) {
            if (task.IsCompleted) {
                if (task.IsCompletedSuccessfully) {
                    return Promise<int>.COMPLETED;
                }
                if (task.IsFaulted) {
                    return Promise<int>.FromException(task.Exception!);
                }
                return Promise<int>.FromCancelled(CancelCodes.REASON_DEFAULT);
            }
            Promise<int> promise = new Promise<int>();
            task.ContinueWith(_invokerToFuture, promise);
            return promise;
        }

        #endregion
    }

    private static class TaskConverterHelper<T>
    {
        #region converter

        private static readonly Action<IFuture<T>, object> _invokerToTask = (future, state) => {
            TaskCompletionSource<T> cts = (TaskCompletionSource<T>)state;
            switch (future.Status) {
                case TaskStatus.Success: {
                    cts.TrySetResult(future.ResultNow());
                    break;
                }
                case TaskStatus.Cancelled: {
                    cts.TrySetCanceled();
                    break;
                }
                case TaskStatus.Failed: {
                    cts.TrySetException(future.ExceptionNow());
                    break;
                }
                default: {
                    throw new AssertionError();
                }
            }
        };

        public static Task<T> ToTask(IFuture<T> future) {
            switch (future.Status) {
                case TaskStatus.Success: {
                    return Task.FromResult(future.ResultNow());
                }
                case TaskStatus.Cancelled: {
                    return Task.FromCanceled<T>(default);
                }
                case TaskStatus.Failed: {
                    return Task.FromException<T>(future.ExceptionNow());
                }
                default: {
                    TaskCompletionSource<T> source = new TaskCompletionSource<T>();
                    future.OnCompleted(_invokerToTask, source);
                    return source.Task;
                }
            }
        }

        ////////////////////////////////////
        private static readonly Action<Task<T>, object> _invokerToFuture = (task, state) => {
            IPromise<T> promise = (IPromise<T>)state;
            if (task.IsCompletedSuccessfully) {
                promise.TrySetResult(task.Result);
            } else if (task.IsFaulted) {
                promise.TrySetException(task.Exception!);
            } else {
                promise.TrySetCancelled(CancelCodes.REASON_DEFAULT);
            }
        };

        public static IFuture<T> ToFuture(Task<T> task) {
            if (task.IsCompleted) {
                if (task.IsCompletedSuccessfully) {
                    return Promise<T>.FromResult(task.Result);
                }
                if (task.IsFaulted) {
                    return Promise<T>.FromException(task.Exception!);
                }
                return Promise<T>.FromCancelled(CancelCodes.REASON_DEFAULT);
            }
            Promise<T> promise = new Promise<T>();
            task.ContinueWith(_invokerToFuture, promise);
            return promise;
        }

        #endregion
    }

    #endregion
}
}