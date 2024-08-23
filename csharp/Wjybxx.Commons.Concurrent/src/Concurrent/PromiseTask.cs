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
#pragma warning disable CS8603

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public interface PromiseTask
{
    /** 优先级的掩码 - 4bit，求值频率较高，放在低位 */
    public const int MASK_PRIORITY = 0x0F;
    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    public const int MASK_TASK_TYPE = 0xF0;
    /** 调度类型的掩码 -- 4bit，最大16种，可支持复杂的调度 */
    public const int MASK_SCHEDULE_TYPE = 0x0F00;

    /** 延时任务是否已触发过 */
    public const int MASK_TRIGGERED = 1 << 16;
    /** 延时任务有超时时间 -- 识别结构体的有效性 */
    public const int MASK_HAS_DEADLINE = 1 << 17;
    /** 延时任务有次数限制 */
    public const int MASK_HAS_COUNTDOWN = 1 << 18;

    public const int OFFSET_PRIORITY = 0;
    /** 任务类型的偏移量 */
    public const int OFFSET_TASK_TYPE = 4;
    /** 调度类型的偏移量 */
    public const int OFFSET_SCHEDULE_TYPE = 8;
    /** 最大优先级 */
    public const int MAX_PRIORITY = MASK_PRIORITY;

    #region factory

    public static PromiseTask<int> OfTask(ITask task, ICancelToken? cancelToken, int options, IPromise<int> promise) {
        return new PromiseTask<int>(task, cancelToken, options, promise, TaskBuilder.TYPE_TASK);
    }

    public static PromiseTask<int> OfAction(Action action, ICancelToken? cancelToken, int options, IPromise<int> promise) {
        return new PromiseTask<int>(action, cancelToken, options, promise, TaskBuilder.TYPE_ACTION);
    }

    public static PromiseTask<int> OfAction(Action<IContext> action, IContext? ctx, int options, IPromise<int> promise) {
        return new PromiseTask<int>(action, ctx, options, promise, TaskBuilder.TYPE_ACTION_CTX);
    }

    public static PromiseTask<T> OfFunction<T>(Func<T> action, ICancelToken? cancelToken, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, cancelToken, options, promise, TaskBuilder.TYPE_FUNC);
    }

    public static PromiseTask<T> OfFunction<T>(Func<IContext, T> action, IContext? ctx, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, ctx, options, promise, TaskBuilder.TYPE_FUNC_CTX);
    }

    public static PromiseTask<T> OfBuilder<T>(in TaskBuilder<T> builder, IPromise<T> promise) {
        return new PromiseTask<T>(in builder, promise);
    }

    #endregion
}

/// <summary>
/// ps：
/// 1.该类的数据是（部分）开放的，以支持不同的扩展。
/// 2.周期性任务通常不适合池化，因为生存周期较长，反而是Submit创建的PromiseTask适合缓存。
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : IFutureTask
{
#nullable disable
    /** 用户的委托 */
    private object task;
    /** 任务的上下文 */
    private object ctx;
    /** 任务的调度选项 */
    protected int options;
    /** 任务关联的promise */
    protected IPromise<T> promise;
    /** 任务的控制标记 */
    protected int ctl;
#nullable enable

    public PromiseTask(in TaskBuilder<T> builder, IPromise<T> promise)
        : this(builder.Task, builder.Context, builder.Options, promise, builder.Type) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">任务</param>
    /// <param name="ctx">任务的上下文</param>
    /// <param name="options">任务的调度选项</param>
    /// <param name="promise"></param>
    /// <param name="taskType">任务类型</param>
    protected internal PromiseTask(object action, object? ctx, int options, IPromise<T> promise, int taskType) {
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
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));
        this.ctl |= (taskType << PromiseTask.OFFSET_TASK_TYPE);
    }

    #region Props

    /** 任务的调度选项 */
    public int Options => options;

    /** 是否收到了取消信号 */
    public bool IsCancelling() {
        return promise.IsCompleted || GetCancelToken().IsCancelling;
    }

    /** 设置为取消状态 */
    public void TrySetCancelled(int code = CancelCodes.REASON_SHUTDOWN) {
        TrySetCancelled(promise, GetCancelToken(), code);
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public int TaskType => (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;

    /** 任务是否启用了指定选项 */
    public bool IsEnabled(int taskOption) {
        return TaskOptions.IsEnabled(options, taskOption);
    }

    /** 获取ctl中的某个bit */
    protected bool GetCtlBit(int mask) {
        return (ctl & mask) != 0;
    }

    /** 设置ctl中的某个bit */
    protected void SetCtlBit(int mask, bool value) {
        if (value) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    #endregion

    /** 注意：如果task和promise之间是双向绑定的，需要解除绑定 */
    public virtual void Clear() {
        task = null;
        ctx = null;
        promise = null;
        options = 0;
        ctl = 0;
    }

    protected ICancelToken GetCancelToken() {
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

    /** 运行分时任务 */
    protected bool RunTimeSharing(bool firstStep, out T result) {
        TimeSharingTask<T> task = (TimeSharingTask<T>)this.task;
        return task((IContext)ctx, firstStep, out result);
    }

    /** 运行可直接得出结果的任务 */
    protected T RunTask() {
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

    public virtual void Run() {
        IPromise<T> promise = this.promise;
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelling) {
            TrySetCancelled(promise, cancelToken);
            return;
        }
        if (promise.TrySetComputing()) {
            try {
                if (TaskType == TaskBuilder.TYPE_TIMESHARING) {
                    if (RunTimeSharing(true, out T result)) {
                        promise.TrySetResult(result);
                    } else {
                        promise.TrySetException(StacklessTimeoutException.INST);
                    }
                } else {
                    T value = RunTask();
                    promise.TrySetResult(value);
                }
            }
            catch (Exception e) {
                promise.TrySetException(e);
            }
        }
    }

    #region util

    protected static bool TrySetCancelled(IPromise promise, ICancelToken cancelToken) {
        int cancelCode = cancelToken.CancelCode;
        Debug.Assert(cancelCode != 0);
        return promise.TrySetCancelled(cancelCode);
    }

    protected static bool TrySetCancelled(IPromise promise, ICancelToken cancelToken, int def) {
        int cancelCode = cancelToken.CancelCode;
        if (cancelCode == 0) cancelCode = def;
        return promise.TrySetCancelled(cancelCode);
    }

    #endregion
}
}