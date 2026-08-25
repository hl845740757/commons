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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 并发工具类
/// </summary>
public static class ExecutorUtil
{
    #region extension

    /// <summary>
    /// 获取用于等待的Awaiter
    /// 1. await时，如果Future已进入完成状态，回调在当前线程执行 —— C#语言机制。
    /// 2. 如果Future尚未进入完成状态，则默认在使Future进入完成状态的线程执行回调，即同步执行回调。
    /// </summary>
    /// <returns></returns>
    public static FutureAwaiter GetAwaiter(this IFuture future) {
        return new FutureAwaiter(future);
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaitable对象。
    /// 
    /// c#的编译器并未支持该功能，因此需要用户显式调用该方法再await，示例如下：
    /// <code>
    ///     // await后的代码将在eventLoop线程执行
    ///     await future.GetAwaitable(eventLoop); 
    /// 
    ///     // 如果future是在eventLoop线程完成的，则同步执行await后的代码，不通过提交异步任务切换线程 
    ///     await future.GetAwaitable(eventLoop, TaskOption.STAGE_TRY_INLINE);
    /// </code>
    /// 注意：指定取消令牌的情况下，回调任务可能不被执行；正常await逻辑不应该传入取消令牌。
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    /// <returns></returns>
    public static FutureAwaitable GetAwaitable(this IFuture future, IExecutor executor, int options = 0) {
        return new FutureAwaitable(future, executor, options);
    }

    /// <summary>
    /// 返回<see cref="TaskResult"/>形式的结果，可禁止异常抛出
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    /// <returns></returns>
    public static FutureAwaitable2 GetAwaitable2(this IFuture future, IExecutor? executor,
                                                 int options = TaskOptions.SUPPRESS_ALL_THROW) {
        return new FutureAwaitable2(future, executor, options);
    }

    /// <summary>
    /// 我们通常仅想禁用异常抛出，因此提供快捷方法
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="options">awaiter的调度选项</param>
    /// <returns></returns>
    public static FutureAwaitable2 GetAwaitable2(this IFuture future,
                                                 int options = TaskOptions.SUPPRESS_ALL_THROW) {
        return new FutureAwaitable2(future, null, options);
    }

    /// <summary>
    /// 获取用于等待的Awaiter
    /// 1. await时，如果Future已进入完成状态，回调在当前线程执行 —— C#语言机制。
    /// 2. 如果Future尚未进入完成状态，则默认在使Future进入完成状态的线程执行回调，即同步执行回调。
    ///
    /// ps：await语法底层的实现，导致我们无法精确控制await的回调线程；必须在Executor上进行等待才可确保线程。
    /// </summary>
    /// <returns></returns>
    public static FutureAwaiter<T> GetAwaiter<T>(this IFuture<T> future) {
        return new FutureAwaiter<T>(future);
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
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    /// <returns></returns>
    public static FutureAwaitable<T> GetAwaitable<T>(this IFuture<T> future, IExecutor executor, int options = 0) {
        return new FutureAwaitable<T>(future, executor, options);
    }

    /// <summary>
    /// 返回<see cref="TaskResult{T}"/>形式的结果，可禁止异常抛出
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    /// <returns></returns>
    public static FutureAwaitable2<T> GetAwaitable2<T>(this IFuture<T> future, IExecutor? executor,
                                                      int options = TaskOptions.SUPPRESS_ALL_THROW) {
        return new FutureAwaitable2<T>(future, executor, options);
    }

    /// <summary>
    /// 我们通常仅想禁用异常抛出，因此提供快捷方法
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="options">awaiter的调度选项</param>
    /// <returns></returns>
    public static FutureAwaitable2<T> GetAwaitable2<T>(this IFuture<T> future,
                                                      int options = TaskOptions.SUPPRESS_ALL_THROW) {
        return new FutureAwaitable2<T>(future, null, options);
    }

    /// <summary>
    /// 任务失败的情况下抛出异常
    /// (不返回结果以避免装箱)
    /// </summary>
    public static void ThrowIfFailedOrCancelled(this IFuture future) {
        switch (future.Status) {
            case TaskStatus.Success: {
                break;
            }
            case TaskStatus.Failed:
            case TaskStatus.Cancelled: {
                future.Join();
                break;
            }
            case TaskStatus.Pending:
            case TaskStatus.Computing:
            default: {
                throw new InvalidOperationException("Task has not completed");
            }
        }
    }

    /// <summary>
    /// 是否表示完成状态
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(this TaskStatus state) {
        return state >= TaskStatus.Success;
    }

    /// <summary>
    /// 是否表示失败或被取消
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFailedOrCancelled(this TaskStatus state) {
        return state > TaskStatus.Success;
    }

    /// <summary>
    /// 测试目标状态是否在筛选范围
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMatch(this TaskStatusFilters filters, TaskStatus status) {
        return status switch
        {
            TaskStatus.Success => (filters & TaskStatusFilters.Success) != 0,
            TaskStatus.Failed => (filters & TaskStatusFilters.Failed) != 0,
            TaskStatus.Cancelled => (filters & TaskStatusFilters.Cancelled) != 0,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture ToValueFuture(this IFuture future) {
        return new ValueFuture(future);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> ToValueFuture<T>(this IFuture<T> future) {
        return new ValueFuture<T>(future);
    }

    #endregion

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
    /// <see cref="GetAwaitable(IFuture, IExecutor, CancellationToken, int)"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskAwaitable GetAwaitable(this Task task, IExecutor executor, int options = 0) {
        return new TaskAwaitable(task, executor, options);
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaiter
    /// <see cref="GetAwaitable(IFuture, IExecutor, CancellationToken, int)"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskAwaitable<T> GetAwaitable<T>(this Task<T> task, IExecutor executor, int options = 0) {
        return new TaskAwaitable<T>(task, executor, options);
    }

    #endregion

    #region event-loop

    /// <summary>
    /// 用于支持<code>await executor</code>语法
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExecutorAwaiter GetAwaiter(this IExecutor executor) => new ExecutorAwaiter(executor);

    /// <summary>
    /// 测试Executor是否是事件循环，且当前线程是否在事件循环线程内
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InEventLoop(IExecutor e) {
        return e is ISingleThreadExecutor eventLoop && eventLoop.InEventLoop();
    }

    /// <summary>
    /// 如果当前不在事件循环线程则抛出异常
    /// </summary>
    /// <exception cref="GuardedOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInEventLoop(this ISingleThreadExecutor eventLoop) {
        if (!eventLoop.InEventLoop()) {
            throw new GuardedOperationException("Method must be called from eventLoop thread");
        }
    }

    /// <summary>
    /// 如果当前不在事件循环线程则抛出异常
    /// </summary>
    /// <exception cref="GuardedOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInEventLoop(this ISingleThreadExecutor eventLoop, string method) {
        if (!eventLoop.InEventLoop()) {
            throw new GuardedOperationException("The " + method + " must be called from eventLoop thread");
        }
    }

    /// <summary>
    /// 如果当前在事件循环异常则抛出异常
    /// </summary>
    /// <exception cref="BlockingOperationException"></exception>
    public static void ThrowIfInEventLoop(this ISingleThreadExecutor eventLoop, string method) {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (eventLoop.InEventLoop()) {
            throw new BlockingOperationException("Calling " + method + " from within the eventLoop is not allowed");
        }
    }

    #endregion

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IPromise<T> NewPromise<T>(IExecutor? executor = null) {
        return new Promise<T>(executor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IPromise<int> NewPromise(IExecutor? executor = null) {
        return new Promise<int>(executor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FutureCombiner NewCombiner() {
        return new FutureCombiner();
    }

    #endregion

    #region submit

    public static ValueFuture<T> Submit<T>(IExecutor executor, in TaskBuilder<T> builder) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfBuilder(promise, in builder));
        return promise.Future;
    }

    // submit 方法不能定义为扩展方法，因为Promise有区别

    #region submit

    public static ValueFuture SubmitAction(IExecutor executor, Action task,
                                           int options = 0, CancellationToken cancelToken = default) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(executor);
        executor.Execute(PromiseTask.OfAction(promise, task, options, cancelToken));
        return promise.VoidFuture;
    }

    public static ValueFuture SubmitAction(IExecutor executor, Action<object> task, object? state,
                                           int options = 0, CancellationToken cancelToken = default) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(executor);
        executor.Execute(PromiseTask.OfAction(promise, task, state, options, cancelToken));
        return promise.VoidFuture;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<T> task,
                                               int options = 0, CancellationToken cancelToken = default) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfFunction(promise, task, options, cancelToken));
        return promise.Future;
    }

    public static ValueFuture<T> SubmitFunc<T>(IExecutor executor, Func<object, T> task, object? state,
                                               int options = 0, CancellationToken cancelToken = default) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(executor);
        executor.Execute(PromiseTask.OfFunction(promise, task, state, options, cancelToken));
        return promise.Future;
    }

    #endregion

    #region execute

    public static void Execute(this IExecutor executor, Action action,
                               int options = 0, CancellationToken cancelToken = default) {
        ITask task = ToTask(action, options, cancelToken);
        executor.Execute(task);
    }

    public static void Execute(this IExecutor executor, Action<object> action, object? state,
                               int options = 0, CancellationToken cancelToken = default) {
        ITask task = ToTask(action, state, options, cancelToken);
        executor.Execute(task);
    }

    #endregion

    #endregion

    #region aggregate

    public static IFuture<object> WhenAny(params IFuture[] futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenAny();
    }

    public static IFuture<object> WhenAny(IEnumerable<IFuture> futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenAny();
    }

    public static IFuture<object> WhenAll(params IFuture[] futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenAll();
    }

    public static IFuture<object> WhenAll(IEnumerable<IFuture> futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenAll();
    }

    public static IFuture<object> WhenNSuccess(int required, params IFuture[] futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenNSuccess(required);
    }

    public static IFuture<object> WhenNSuccess(int required, IEnumerable<IFuture> futures) {
        return new FutureCombiner()
            .AddAll(futures)
            .WhenNSuccess(required);
    }

    #endregion

    #region internal

    /// <summary>
    /// 判断是否可以不提交任务，而是立即执行
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInlinable([NotNullWhen(false)] IExecutor? e, int options) {
        if (e == null) return true;
        return TaskOptions.IsEnabled(options, TaskOptions.STAGE_TRY_INLINE)
               && e is ISingleThreadExecutor eventLoop
               && eventLoop.InEventLoop();
    }

    /// <summary>
    /// 是否压制异常抛出
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSuppressible(int options, TaskStatus status) {
        return status switch
        {
            TaskStatus.Failed => (options & TaskOptions.SUPPRESS_ERROR_THROW) != 0,
            TaskStatus.Cancelled => (options & TaskOptions.SUPPRESS_CANCELLATION_THROW) != 0,
            _ => false
        };
    }

    #endregion

    #region box

    public static ITask ToTask(Action action, int options = 0, CancellationToken cancelToken = default) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper2(action, options, cancelToken);
    }

    public static ITask ToTask(Action<object> action, object? state, int options = 0, CancellationToken cancelToken = default) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper3(action, state, options, cancelToken);
    }

    #endregion

    #region box-class

    private class ActionWrapper2 : ITask
    {
        private readonly Action action;
        private readonly CancellationToken cancelToken;
        private readonly int options;

        public ActionWrapper2(Action action, int options, CancellationToken cancelToken) {
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

        public override string ToString() {
            return $"{nameof(action)}: {action}, {nameof(options)}: {options}";
        }
    }

    private class ActionWrapper3 : ITask
    {
        private readonly Action<object> action;
        private readonly object? state;
        private readonly CancellationToken cancelToken;
        private readonly int options;

        public ActionWrapper3(Action<object> action, object? state, int options, CancellationToken cancelToken) {
            this.action = action;
            this.state = state;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancellationRequested) {
                return;
            }
            action(state);
        }

        public override string ToString() {
            return $"{nameof(action)}: {action}, {nameof(options)}: {options}";
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
                    task.OnCompleted(_invokerSetVoidPromise, promise);
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
                    task.OnCompleted(_invokerFlatSetPromise, promise);
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
                case TaskStatus.Failed: {
                    ExceptionDispatchInfo dispatchInfoNow = (ExceptionDispatchInfo)task.ExceptionOrDispatchInfoNow();
                    promise.TrySetException(dispatchInfoNow);
                    break;
                }
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerSetPromise, promise);
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
                case TaskStatus.Failed: {
                    ExceptionDispatchInfo dispatchInfoNow = (ExceptionDispatchInfo)task.ExceptionOrDispatchInfoNow();
                    promise.TrySetException(dispatchInfoNow);
                    break;
                }
                case TaskStatus.Cancelled: {
                    promise.TrySetException(task.ExceptionNow(false));
                    break;
                }
                default: {
                    task.OnCompleted(_invokerFlatSetPromise, promise);
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
                case TaskStatus.Cancelled: { // 必须传入已取消的Token...
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel(throwOnFirstException: false);
                    return Task.FromCanceled(cts.Token);
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
                promise.TrySetCancelled();
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
                return Promise<int>.FromCancelled();
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
                case TaskStatus.Cancelled: { // 必须传入已取消的Token...
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel(throwOnFirstException: false);
                    return Task.FromCanceled<T>(cts.Token);
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
                promise.TrySetCancelled();
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
                return Promise<T>.FromCancelled();
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