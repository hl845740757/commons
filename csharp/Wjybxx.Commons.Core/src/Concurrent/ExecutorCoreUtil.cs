#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该类用于解决行为树的依赖问题
///
/// 主要与取消令牌相关
/// </summary>
public static class ExecutorCoreUtil
{
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
    /// <param name="cts">要等待的取消令牌</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancelTokenAwaitable GetAwaitable(this ICancelToken cts, IExecutor? executor, int options = 0) {
        return new CancelTokenAwaitable(cts, executor, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancelTokenAwaiter GetAwaiter(this ICancelToken cts) {
        return new CancelTokenAwaiter(cts, null, 0);
    }

    /// <summary>
    /// 获取ctx中的取消令牌
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ICancelToken GetCancelToken(object? ctx, int options) {
        if (ctx == null || TaskOptions.IsEnabled(options, TaskOptions.STAGE_UNCANCELLABLE_CTX)) {
            return ICancelToken.NONE;
        }
        if (ctx is ICancelToken cts) {
            return cts;
        }
        if (ctx is IContext ctx2) {
            return ctx2.CancelToken;
        }
        return ICancelToken.NONE;
    }

    /// <summary>
    /// 查询ctx中的取消令牌是否收到了取消信号
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static bool IsCancelRequested(object? ctx, int options) {
        if (ctx == null || TaskOptions.IsEnabled(options, TaskOptions.STAGE_UNCANCELLABLE_CTX)) {
            return false;
        }
        if (ctx is ICancelToken cts) {
            return cts.IsCancelRequested;
        }
        if (ctx is IContext ctx2) {
            return ctx2.CancelToken.IsCancelRequested;
        }
        return false;
    }

    /// <summary>
    /// 判断是否可以不提交任务，而是立即执行
    /// </summary>
    /// <param name="e"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInlinable(IExecutor? e, int options) {
        if (e == null) return true;
        return TaskOptions.IsEnabled(options, TaskOptions.STAGE_TRY_INLINE)
               && e is ISingleThreadExecutor eventLoop
               && eventLoop.InEventLoop();
    }

    #region box

    public static ITask ToTask(Action action, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper1(action, options);
    }

    public static ITask ToTask(Action action, ICancelToken? cancelToken, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (cancelToken == null) throw new ArgumentNullException(nameof(cancelToken));
        return new ActionWrapper2(action, cancelToken, options);
    }

    public static ITask ToTask(Action<object> action, object? ctx, int options = 0) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper3(action, ctx, options);
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

        public override string ToString() {
            return $"{nameof(action)}: {action}, {nameof(options)}: {options}";
        }
    }

    private class ActionWrapper2 : ITask
    {
        private readonly Action action;
        private readonly ICancelToken? cancelToken;
        private readonly int options;

        public ActionWrapper2(Action action, ICancelToken? cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken != null && cancelToken.IsCancelRequested) {
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
        private readonly object? ctx;
        private readonly int options;

        public ActionWrapper3(Action<object> action, object? ctx, int options) {
            this.action = action;
            this.ctx = ctx;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (IsCancelRequested(ctx, options)) {
                return;
            }
            action(ctx);
        }

        public override string ToString() {
            return $"{nameof(action)}: {action}, {nameof(options)}: {options}";
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

        public override string ToString() {
            return $"{nameof(action)}: {action}, {nameof(options)}: {options}";
        }
    }

    #endregion
}
}