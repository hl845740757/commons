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
using System.Diagnostics;
using Wjybxx.Commons.Pool;

#pragma warning disable CS8603

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 提供给用户的工具类
/// </summary>
public static class ValueFutureTask
{
    public static ValueFuture<T> Call<T>(IExecutor executor, in TaskBuilder<T> builder) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(builder.Task, builder.Context, builder.Options, builder.Type);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask); // 可能会立即完成，因此需要提前保存future
        return promise;
    }

    #region func

    public static ValueFuture<T> Call<T>(IExecutor executor, Func<T> task, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, null, options, TaskBuilder.TYPE_FUNC);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture<T> Call<T>(IExecutor executor, Func<T> task, ICancelToken cancelToken, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, cancelToken, options, TaskBuilder.TYPE_FUNC);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture<T> Call<T>(IExecutor executor, Func<IContext, T> task, IContext ctx, int options = 0) {
        ValueFutureTask<T> futureTask = ValueFutureTask<T>.Create(task, ctx, options, TaskBuilder.TYPE_FUNC_CTX);
        ValueFuture<T> promise = futureTask.Future;
        executor.Execute(futureTask);
        return promise;
    }

    #endregion

    #region action

    public static ValueFuture Run(IExecutor executor, Action task, int options = 0) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, null, options, TaskBuilder.TYPE_ACTION);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture Run(IExecutor executor, Action task, ICancelToken cancelToken, int options) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, cancelToken, options, TaskBuilder.TYPE_ACTION);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    public static ValueFuture Run(IExecutor executor, Action<IContext> task, IContext ctx, int options) {
        ValueFutureTask<int> futureTask = ValueFutureTask<int>.Create(task, ctx, options, TaskBuilder.TYPE_ACTION_CTX);
        ValueFuture promise = futureTask.VoidFuture;
        executor.Execute(futureTask);
        return promise;
    }

    #endregion
}

/// <summary>
/// 用于封装用户的任务，并返回给用户<see cref="ValueFuture{T}"/>，
/// 可看做轻量级的<see cref="PromiseTask{T}"/>实现。
/// </summary>
/// <typeparam name="T"></typeparam>
internal class ValueFutureTask<T> : ValuePromise<T>, IFutureTask
{
    private static readonly ConcurrentObjectPool<ValueFutureTask<T>> POOL =
        new(() => new ValueFutureTask<T>(), task => task.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolConfig.TaskType.ValueFutureTask));

#nullable disable
    /** 用户的委托 */
    private object task;
    /** 任务的上下文 */
    private object ctx;
    /** 任务的调度选项 */
    private int options;
    /** 任务的控制标记 */
    private int ctl;
#nullable enable

    private ValueFutureTask() {
    }

    private void Init(object action, object? ctx, int options, int taskType) {
        if (ctx == null) {
            if (TaskBuilder.IsTaskAcceptContext(taskType)) {
                ctx = IContext.NONE;
            } else {
                ctx = ICancelToken.NONE;
            }
        }
        this.task = action ?? throw new ArgumentNullException(nameof(action));
        this.ctx = ctx;
        this.options = options;

        this.ctl = (options & TaskOptions.MASK_PRIORITY_AND_SCHEDULE_PHASE);
        this.ctl |= (taskType << PromiseTask.OFFSET_TASK_TYPE);
    }

    public static ValueFutureTask<T> Create(object action, object? ctx, int options, int taskType) {
        ValueFutureTask<T> futureTask = POOL.Acquire();
        futureTask.IncReentryId(); // 重用时也加1
        futureTask.Init(action, ctx, options, taskType);
        return futureTask;
    }

    public override void Reset() {
        base.Reset();
        task = null;
        ctx = null;
        options = 0;
        ctl = 0;
    }

    protected override void PrepareToRecycle() {
        POOL.Release(this);
    }

    #region future-task

    public int Options => options;

    /** 是否收到了取消信号 */
    public bool IsCancelling() {
        return IsCompleted || GetCancelToken().IsCancelling;
    }

    /** 设置为取消状态 */
    public void TrySetCancelled(int code = CancelCodes.REASON_SHUTDOWN) {
        ICancelToken cancelToken = GetCancelToken();
        int cancelCode = cancelToken.CancelCode;
        if (cancelCode == 0) {
            cancelCode = code;
        }
        Internal_TrySetCancelled(cancelCode);
    }

    /// <summary>
    /// 该方法可能被EventLoop调用，但我们空实现
    /// </summary>
    public void Clear() {
    }

    /** 任务的类型 */
    private int TaskType => (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;

    /** 获取取消令牌 */
    private ICancelToken GetCancelToken() {
        object ctx = this.ctx;
        if (ctx == ICancelToken.NONE || ctx == IContext.NONE) {
            return ICancelToken.NONE;
        }
        if (TaskBuilder.IsTaskAcceptContext(TaskType)) {
            IContext castCtx = (IContext)ctx;
            return castCtx.CancelToken;
        }
        return (ICancelToken)ctx;
    }

    /** 运行可直接得出结果的任务 */
    private T RunTask() {
        int type = (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;
        switch (type) {
            case TaskBuilder.TYPE_ACTION: {
                Action task = (Action)this.task;
                task();
                return default;
            }
            case TaskBuilder.TYPE_FUNC: {
                Func<T> task = (Func<T>)this.task;
                return task();
            }
            case TaskBuilder.TYPE_ACTION_CTX: {
                Action<IContext> task = (Action<IContext>)this.task;
                task((IContext)ctx);
                return default;
            }
            case TaskBuilder.TYPE_FUNC_CTX: {
                Func<IContext, T> task = (Func<IContext, T>)this.task;
                return task((IContext)ctx);
            }
            case TaskBuilder.TYPE_TASK: {
                ITask task = (ITask)this.task;
                task.Run();
                return default;
            }
            default: {
                throw new AssertionError("type: " + type);
            }
        }
    }

    public void Run() {
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelling) {
            Internal_TrySetCancelled(cancelToken.CancelCode);
            return;
        }
        if (Internal_TrySetComputing()) {
            try {
                T value = RunTask();
                Internal_TrySetResult(value);
            }
            catch (Exception e) {
                Internal_TrySetException(e);
            }
        }
    }

    #endregion
}
}