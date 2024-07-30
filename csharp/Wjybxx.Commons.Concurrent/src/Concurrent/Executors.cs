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
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 并发工具类
/// </summary>
public static class Executors
{
    #region box

    public static ITask BoxAction(Action action, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper1(action, options);
    }

    public static ITask BoxAction(Action action, CancellationToken cancelToken, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper2(action, cancelToken, options);
    }

    public static ITask BoxAction(Action action, ICancelToken cancelToken, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (cancelToken == null) throw new ArgumentNullException(nameof(cancelToken));
        return new ActionWrapper3(action, cancelToken, options);
    }

    public static ITask BoxAction(Action<IContext> action, IContext context, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper4(action, context, options);
    }

    public static Action CancellableAction(Action action, CancellationToken cancelToken) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return () => {
            if (cancelToken.IsCancellationRequested) {
                return;
            }
            action.Invoke();
        };
    }

    #endregion

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


    /// <summary>
    /// 将future结果传输到Promise
    /// </summary>
    /// <param name="promise"></param>
    /// <param name="task"></param>
    /// <typeparam name="TResult"></typeparam>
    public static void SetPromise<TResult>(IPromise<TResult> promise, IFuture<TResult> task) {
        IPromise<TResult>.SetPromise(promise, task);
    }

    #endregion

    #region system

    public static void FlatSetPromise<TResult>(TaskCompletionSource<TResult> promise, Task<Task<TResult>> task) {
        if (task.IsCompleted) {
            if (task.IsCompletedSuccessfully) {
                SetPromise(promise, task.Result);
            } else if (task.IsFaulted) {
                promise.TrySetException(task.Exception!);
            } else {
                promise.TrySetCanceled();
            }
        } else {
            task.ContinueWith((t, obj) => FlatSetPromise((TaskCompletionSource<TResult>)obj, t), promise);
        }
    }

    public static void SetPromise<TResult>(TaskCompletionSource<TResult> promise, Task<TResult> task) {
        if (task.IsCompleted) {
            if (task.IsCompletedSuccessfully) {
                promise.TrySetResult(task.Result);
            } else if (task.IsFaulted) {
                promise.TrySetException(task.Exception!);
            } else {
                promise.TrySetCanceled();
            }
        } else {
            task.ContinueWith((t, obj) => SetPromise((TaskCompletionSource<TResult>)obj, t), promise);
        }
    }

    /** 用于忽略警告 */
    public static void Forget(this Task task) {
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
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
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
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    public static TaskAwaitable<T> GetAwaitable<T>(this Task<T> task, IExecutor executor, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return new TaskAwaitable<T>(task, executor, options);
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
        private readonly CancellationToken cancelToken;
        private readonly int options;

        public ActionWrapper2(Action action, CancellationToken cancelToken, int options) {
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

    private class ActionWrapper3 : ITask
    {
        private readonly Action action;
        private readonly ICancelToken cancelToken;
        private readonly int options;

        public ActionWrapper3(Action action, ICancelToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancelling) {
                return;
            }
            action();
        }
    }

    private class ActionWrapper4 : ITask
    {
        private readonly Action<IContext> action;
        private readonly IContext context;
        private readonly int options;

        public ActionWrapper4(Action<IContext> action, IContext context, int options) {
            this.action = action;
            this.context = context;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (context.CancelToken.IsCancelling) {
                return;
            }
            action(context);
        }
    }

    #endregion
}
}