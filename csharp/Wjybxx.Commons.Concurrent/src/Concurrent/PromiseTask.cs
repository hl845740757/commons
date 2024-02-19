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
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : IFutureTask<T>
{
    /**
     * queueId的掩码 -- 8bit，最大255。
     * 1.放在低8位，减少运算，queueId的计算频率高于其它部分。
     * 2.大于{@link TaskOption}的中的64阶段。
     */
    protected const int maskQueueId = 0xFF;
    /** 任务类型的掩码 -- 4bit，可省去大量的instanceof测试 */
    protected const int maskTaskType = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种 */
    protected const int maskScheduleType = 0xF000;
    /** 是否已经声明任务的归属权 */
    protected const int maskClaimed = 1 << 16;
    /** 分时任务是否已启动 */
    protected const int maskStarted = 1 << 17;
    /** 分时任务是否已停止 */
    protected const int maskStopped = 1 << 18;

    protected const int offsetQueueId = 0;
    protected const int offsetTaskType = 8;
    protected const int offsetScheduleType = 12;
    protected const int maxQueueId = 255;

    /** 用户的委托 */
    private object task;
    /** 任务的调度选项 */
    protected readonly int options;
    /** 任务关联的promise - 用户可能在任务完成后继续访问，因此不能清理 */
    protected readonly IPromise<T> promise;
    /** 任务的控制标记 */
    private int ctl;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action">任务</param>
    /// <param name="options">任务的调度选项</param>
    /// <param name="promise"></param>
    public PromiseTask(object action, int options, IPromise<T> promise)
        : this(action, options, promise, TaskBuilder.TaskType(action)) {
    }

    public PromiseTask(ref TaskBuilder<T> builder, IPromise<T> promise)
        : this(builder.Task, builder.Options, promise, builder.Type) {
    }

    public PromiseTask(object action, int options, IPromise<T> promise, int taskType) {
        this.task = action ?? throw new ArgumentNullException(nameof(action));
        this.options = options;
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));
        this.ctl |= (taskType << offsetTaskType);
        // 注入promise
    }

    #region factory

    public static PromiseTask<T> OfAction(Action action, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, options, promise, TaskBuilder.TYPE_ACTION);
    }

    public static PromiseTask<T> OfAction(Action<IContext> action, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, options, promise, TaskBuilder.TYPE_ACTION_CTX);
    }

    public static PromiseTask<T> OfFunction(Func<T> action, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, options, promise, TaskBuilder.TYPE_FUNC);
    }

    public static PromiseTask<T> OfFunction(Func<IContext, T> action, int options, IPromise<T> promise) {
        return new PromiseTask<T>(action, options, promise, TaskBuilder.TYPE_FUNC_CTX);
    }

    public static PromiseTask<T> OfBuilder(ref TaskBuilder<T> builder, IPromise<T> promise) {
        return new PromiseTask<T>(ref builder, promise);
    }

    #endregion

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
    public bool isEnable(int taskOption) {
        return TaskOption.isEnabled(options, taskOption);
    }

    /** 获取绑定的任务 */
    public object getTask() {
        return task;
    }

    /** 获取任务所属的队列id */
    public int getQueueId() {
        return (ctl & maskQueueId);
    }

    /** @param queueId 队列id，范围 [0, 255] */
    public void setQueueId(int queueId) {
        if (queueId < 0 || queueId > maxQueueId) {
            throw new ArgumentException("queueId: " + maxQueueId);
        }
        ctl &= ~maskQueueId;
        ctl |= (queueId);
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public int getTaskType() {
        return (ctl & maskTaskType) >> offsetTaskType;
    }

    /** 获取任务的调度类型 */
    public int getScheduleType() {
        return (ctl & maskScheduleType) >> offsetScheduleType;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    public void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << offsetScheduleType);
    }

    /** 是否是循环任务 */
    public bool isPeriodic() {
        return getScheduleType() != 0;
    }

    /** 是否已经声明任务的归属权 */
    public bool isClaimed() {
        return (ctl & maskClaimed) != 0;
    }

    /** 将任务标记为已申领 */
    public void setClaimed() {
        ctl |= maskClaimed;
    }

    /** 分时任务是否启动 */
    public bool isStarted() {
        return (ctl & maskStarted) != 0;
    }

    /** 将分时任务标记为已启动 */
    public void setStarted() {
        ctl |= maskStarted;
    }

    #endregion

    protected void Clear() {
        task = null!;
    }

    /** 运行可直接得出结果的任务 */
    protected T RunTask() {
        int type = (ctl & maskTaskType) >> offsetTaskType;
        switch (type) {
            case TaskBuilder.TYPE_ACTION: {
                Action task = (Action)this.task;
                task();
                return default;
            }
            case TaskBuilder.TYPE_ACTION_CTX: {
                Action<IContext> task = (Action<IContext>)this.task;
                task(promise.Context);
                return default;
            }
            case TaskBuilder.TYPE_FUNC: {
                Func<T> task = (Func<T>)this.task;
                return task();
            }
            case TaskBuilder.TYPE_FUNC_CTX: {
                Func<IContext, T> task = (Func<IContext, T>)this.task;
                return task(promise.Context);
            }
            default: {
                throw new AssertionError("type: " + type);
            }
        }
    }

    public void Run() {
        IPromise<T> promise = this.promise;
        if (promise.Context.CancelToken.IsCancelling()) {
            TrySetCancelled(promise);
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

    protected static void TrySetCancelled(IPromise promise) {
        int cancelCode = promise.Context.CancelToken.CancelCode;
        Debug.Assert(cancelCode != 0);
        promise.TrySetCancelled(cancelCode);
    }

    protected static void TrySetCancelled(IPromise promise, int def) {
        int cancelCode = promise.Context.CancelToken.CancelCode;
        if (cancelCode == 0) cancelCode = def;
        promise.TrySetCancelled(cancelCode);
    }
}