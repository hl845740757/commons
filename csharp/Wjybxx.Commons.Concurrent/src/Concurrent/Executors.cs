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
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 并发工具类
/// </summary>
public static class Executors
{
    #region EventLoop

    /// <summary>
    /// 用于支持<code>await executor</code>语法
    /// </summary>
    /// <returns></returns>
    public static ExecutorAwaiter GetAwaiter(this IExecutor executor) => new ExecutorAwaiter(executor);

    /// <summary>
    /// 测试Executor是否是事件循环，且当前线程是否在事件循环线程内
    /// </summary>
    /// <param name="executor"></param>
    /// <returns></returns>
    public static bool InEventLoop(IExecutor executor) {
        if (executor is ISingleThreadExecutor singleThreadExecutor) {
            return singleThreadExecutor.InEventLoop();
        }
        return false;
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

    public static Task ToTask(IFuture future) {
        return TaskConverterHelper.ToTask(future);
    }

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

    /** 用于忽略警告 */
    public static void Forget(this Task task) {
    }

    /** 用于忽略警告 */
    public static void Forget(this IFuture task) {
    }

    public static bool IsFailedOrCancelled(this Task task) {
        return task.IsCanceled || task.IsFaulted;
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
    public static TaskAwaitable GetAwaitable(this Task task, IExecutor executor, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
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
    public static TaskAwaitable<T> GetAwaitable<T>(this Task<T> task, IExecutor executor, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
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

    public static IFuture<T> Submit<T>(IExecutor executor, in TaskBuilder<T> builder) {
        IPromise<T> promise = NewPromise<T>(executor);
        executor.Execute(PromiseTask.OfBuilder(builder, promise));
        return promise;
    }

    // submit 方法不能定义为扩展方法，因为Promise有区别

    #region submit func

    public static IFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, int options = 0) {
        IPromise<T> promise = NewPromise<T>(executor);
        executor.Execute(PromiseTask.OfFunction(task, null, options, promise));
        return promise;
    }

    public static IFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task, ICancelToken cancelToken, int options = 0) {
        IPromise<T> promise = NewPromise<T>(executor);
        executor.Execute(PromiseTask.OfFunction(task, cancelToken, options, promise));
        return promise;
    }

    public static IFuture<T> SubmitFunc<T>(IExecutor executor, Func<IContext, T> task, IContext ctx, int options = 0) {
        IPromise<T> promise = NewPromise<T>(executor);
        executor.Execute(PromiseTask.OfFunction(task, ctx, options, promise));
        return promise;
    }

    #endregion

    #region submit action

    public static IFuture SubmitAction(IExecutor executor, Action task, int options = 0) {
        IPromise<int> promise = NewPromise(executor);
        executor.Execute(PromiseTask.OfAction(task, null, options, promise));
        return promise;
    }

    public static IFuture SubmitAction(IExecutor executor, Action task, ICancelToken cancelToken, int options) {
        IPromise<int> promise = NewPromise(executor);
        executor.Execute(PromiseTask.OfAction(task, cancelToken, options, promise));
        return promise;
    }

    public static IFuture SubmitAction(IExecutor executor, Action<IContext> task, IContext ctx, int options) {
        IPromise<int> promise = NewPromise(executor);
        executor.Execute(PromiseTask.OfAction(task, ctx, options, promise));
        return promise;
    }

    #endregion

    #region execute

    public static void Execute(this IExecutor executor, Action action, ICancelToken cancelToken, int options) {
        ITask futureTask = ToTask(action, cancelToken, options);
        executor.Execute(futureTask);
    }

    public static void Execute(this IExecutor executor, Action<IContext> action, IContext ctx, int options) {
        ITask futureTask = ToTask(action, ctx, options);
        executor.Execute(futureTask);
    }

    public static void Execute(this IExecutor executor, Action action, CancellationToken cancellationToken, int options = 0) {
        executor.Execute(ToTask(action, cancellationToken, options));
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

    #region box

    public static ITask ToTask(Action action, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper1(action, options);
    }

    public static ITask ToTask(Action action, ICancelToken cancelToken, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (cancelToken == null) throw new ArgumentNullException(nameof(cancelToken));
        return new ActionWrapper2(action, cancelToken, options);
    }

    public static ITask ToTask(Action<IContext> action, IContext context, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper3(action, context, options);
    }

    public static ITask ToTask(Action action, CancellationToken cancelToken, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper4(action, cancelToken, options);
    }

    #endregion

    #region box-class

    private class ActionWrapper1 : ITask
    {
        private readonly Action action;
        private readonly int options;

        public ActionWrapper1(Action action, int options) {
            this.action = action;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            action();
        }
    }

    private class ActionWrapper2 : ITask
    {
        private readonly Action action;
        private readonly ICancelToken cancelToken;
        private readonly int options;

        public ActionWrapper2(Action action, ICancelToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancelRequested) {
                return;
            }
            action();
        }
    }

    private class ActionWrapper3 : ITask
    {
        private readonly Action<IContext> action;
        private readonly IContext context;
        private readonly int options;

        public ActionWrapper3(Action<IContext> action, IContext context, int options) {
            this.action = action;
            this.context = context;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (context.CancelToken.IsCancelRequested) {
                return;
            }
            action(context);
        }
    }

    private class ActionWrapper4 : ITask
    {
        private readonly Action action;
        private readonly CancellationToken cancelToken;
        private readonly int options;

        public ActionWrapper4(Action action, CancellationToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancellationRequested) {
                return;
            }
            action();
        }
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