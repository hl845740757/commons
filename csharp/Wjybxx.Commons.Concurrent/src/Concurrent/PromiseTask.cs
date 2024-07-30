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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public interface PromiseTask
{
    /** 优先级的掩码 - 4bit，求值频率较高，放在低位 */
    protected const int MASK_PRIORITY = 0x0F;
    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    protected const int MASK_TASK_TYPE = 0xF0;
    /** 调度类型的掩码 -- 4bit，最大16种，可支持复杂的调度 */
    protected const int MASK_SCHEDULE_TYPE = 0x0F00;

    /** 是否已经声明任务的归属权 */
    protected const int MASK_CLAIMED = 1 << 16;
    /** 分时任务是否已启动 */
    protected const int MASK_STARTED = 1 << 17;
    /** 分时任务是否已停止 */
    protected const int MASK_STOPPED = 1 << 18;
    /** 延时任务有超时时间 -- 识别结构体的有效性 */
    protected const int MASK_TIMEOUT = 1 << 20;
    /** 延时任务是否已触发过 */
    protected const int MASK_TRIGGERED = 1 << 21;

    protected const int OFFSET_PRIORITY = 0;
    /** 任务类型的偏移量 */
    protected const int OFFSET_TASK_TYPE = 4;
    /** 调度类型的偏移量 */
    protected const int OFFSET_SCHEDULE_TYPE = 8;
    /** 最大优先级 */
    protected const int MAX_PRIORITY = MASK_PRIORITY;

    #region factory

    public static PromiseTask<T> OfTask<T>(ITask task, IContext? context, int options, IPromise<T> promise) {
        return new PromiseTask<T>(task, context, options, promise, TaskBuilder.TYPE_TASK);
    }

    public static PromiseTask<T> OfAction<T>(Action action, IContext? context, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_ACTION);
    }

    public static PromiseTask<T> OfAction<T>(Action<IContext> action, IContext? context, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_ACTION_CTX);
    }

    public static PromiseTask<T> OfFunction<T>(Func<T> action, IContext? context, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_FUNC);
    }

    public static PromiseTask<T> OfFunction<T>(Func<IContext, T> action, IContext? context, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_FUNC_CTX);
    }

    public static PromiseTask<T> OfBuilder<T>(in TaskBuilder<T> builder, IPromise<T> promise) {
        return new PromiseTask<T>(in builder, promise);
    }

    #endregion
}

/// <summary>
/// 注意：我们统一使用int代替void，避免额外的实现
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : IFutureTask<T>, PromiseTask
{
    /** 用户的委托 */
    private object task;
    /** 任务的上下文 */
    protected IContext context;
    /** 任务的调度选项 */
    protected readonly int options;
    /** 任务关联的promise - 用户可能在任务完成后继续访问，因此不能清理 */
    protected readonly IPromise<T> promise;
    /** 任务的控制标记 */
    protected int ctl;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">任务</param>
    /// <param name="context">任务的上下文</param>
    /// <param name="options">任务的调度选项</param>
    /// <param name="promise"></param>
    public PromiseTask(object action, IContext context, int options, IPromise<T> promise)
        : this(action, context, options, promise, TaskBuilder.TaskType(action)) {
    }

    public PromiseTask(in TaskBuilder<T> builder, IPromise<T> promise)
        : this(builder.Task, builder.Context, builder.Options, promise, builder.Type) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">任务</param>
    /// <param name="context">任务的上下文</param>
    /// <param name="options">任务的调度选项</param>
    /// <param name="promise"></param>
    /// <param name="taskType">任务类型</param>
    public PromiseTask(object action, IContext? context, int options, IPromise<T> promise, int taskType) {
        this.task = action ?? throw new ArgumentNullException(nameof(action));
        this.context = context ?? IContext.NONE;
        this.options = options;
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));
        this.ctl |= (taskType << PromiseTask.OFFSET_TASK_TYPE);
        // 分时任务注入promise
    }

    #region Props

    /// <summary>
    /// 任务的调度选项
    /// </summary>
    public int Options => options;

    /// <summary>
    /// 获取任务关联的Promise
    /// 允许子类修改返回值类型。
    /// </summary>
    public virtual IPromise<T> Future => promise;

    /** 任务是否启用了指定选项 */
    public bool IsEnabled(int taskOption) {
        return TaskOption.IsEnabled(options, taskOption);
    }

    /** 获取绑定的任务 */
    public object Task => task;

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public int TaskType => (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;

    /** 分时任务是否启动 */
    public bool IsStarted() {
        return (ctl & PromiseTask.MASK_STARTED) != 0;
    }

    /** 将分时任务标记为已启动 */
    public void SetStarted() {
        ctl |= PromiseTask.MASK_STARTED;
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

    protected virtual void Clear() {
        task = null!;
        context = null!;
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
                task(context);
                return default;
            }
            case TaskBuilder.TYPE_FUNC_CTX: {
                Func<IContext, T> task = (Func<IContext, T>)this.task;
                return task(context);
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
        IContext context = this.context;
        if (context.CancelToken.IsCancelling) {
            TrySetCancelled(promise, context);
            Clear();
            return;
        }
        if (promise.TrySetComputing()) {
            try {
                T value = RunTask();
                promise.TrySetResult(value);
            }
            catch (Exception e) {
                promise.TrySetException(e);
            }
        }
        Clear();
    }

    protected static void TrySetCancelled(IPromise promise, IContext context) {
        int cancelCode = context.CancelToken.CancelCode;
        Debug.Assert(cancelCode != 0);
        promise.TrySetCancelled(cancelCode);
    }

    protected static void TrySetCancelled(IPromise promise, IContext context, int def) {
        int cancelCode = context.CancelToken.CancelCode;
        if (cancelCode == 0) cancelCode = def;
        promise.TrySetCancelled(cancelCode);
    }
}