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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Sequential;

/// <summary>
/// 接口用于定义常量和工具方法
///
/// 与<see cref="ScheduledPromiseTask{T}"/>的区别？
/// 1. 时间单位不同，ticks => millis；
/// 2. 依赖的Executor不同。
/// </summary>
public interface UniScheduledPromiseTask
{
    #region factory

    public static UniScheduledPromiseTask<T> OfTask<T>(ITask task, IContext? context, int options, IScheduledPromise<T> promise,
                                                       long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<T>(task, context, options, promise, TaskBuilder.TYPE_TASK,
            id, nextTriggerTime);
    }

    public static UniScheduledPromiseTask<T> OfAction<T>(Action action, IContext? context, int options, IScheduledPromise<T> promise,
                                                         long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_ACTION,
            id, nextTriggerTime);
    }

    public static UniScheduledPromiseTask<T> OfAction<T>(Action<IContext> action, IContext? context, int options, IScheduledPromise<T> promise,
                                                         long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_ACTION_CTX,
            id, nextTriggerTime);
    }

    public static UniScheduledPromiseTask<T> OfFunction<T>(Func<T> action, IContext? context, int options, IScheduledPromise<T> promise,
                                                           long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_FUNC,
            id, nextTriggerTime);
    }

    public static UniScheduledPromiseTask<T> OfFunction<T>(Func<IContext, T> action, IContext? context, int options, IScheduledPromise<T> promise,
                                                           long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_FUNC_CTX,
            id, nextTriggerTime);
    }

    public static UniScheduledPromiseTask<T> OfBuilder<T>(ref TaskBuilder<T> builder, IScheduledPromise<T> promise,
                                                          long id, long tickTime) {
        return new UniScheduledPromiseTask<T>(builder.Task, builder.Context, builder.Options, promise, builder.Type,
            id, tickTime);
    }

    public static UniScheduledPromiseTask<T> OfBuilder<T>(ref ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise,
                                                          long id, long tickTime) {
        long timeUnit = Math.Max(1, builder.Timeunit.Ticks);

        // 并发库中不支持插队，初始延迟强制转0 -- tick转毫秒
        long initialDelay = Math.Max(0, builder.InitialDelay * timeUnit) / TimeSpan.TicksPerMillisecond;
        long period = Math.Max(1, (builder.Period * timeUnit) / TimeSpan.TicksPerMillisecond);
        long triggerTime = tickTime + initialDelay;

        TimeoutContext? timeoutContext;
        if (builder.IsPeriodic && builder.Timeout != -1) {
            long timeout = builder.Timeout * timeUnit / TimeSpan.TicksPerMillisecond;
            timeoutContext = new TimeoutContext(timeout, tickTime);
        } else {
            timeoutContext = null;
        }
        return new UniScheduledPromiseTask<T>(ref builder, promise, id, triggerTime, period, timeoutContext);
    }

    #endregion

    /** 计算任务的触发时间 -- 毫秒 */
    public static long TriggerTime(TimeSpan delay, long tickTime) {
        return Math.Max(0, (long)delay.TotalMilliseconds + tickTime);
    }
}

public class UniScheduledPromiseTask<T> : PromiseTask<T>, IScheduledFutureTask<T>,
    IIndexedElement, UniScheduledPromiseTask, ICancelTokenListener
{
    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long nextTriggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private long period;
    /** 超时信息 - 有效性见<see cref="PromiseTask.MASK_TIMEOUT"/> */
    private TimeoutContext timeoutContext;

    /** 在队列中的下标 */
    private int queueIndex = IIndexedElement.IndexNotFound;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private IRegistration? cancelRegistration;

    internal UniScheduledPromiseTask(ref ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise,
                                     long id, long nextTriggerTime, long period, TimeoutContext? timeoutContext)
        : base(builder.Task, builder.Context, builder.Options, promise, builder.Type) {
        this.id = id;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        // c# 超时信息特殊处理
        if (timeoutContext.HasValue) {
            this.timeoutContext = timeoutContext.Value;
            HasTimeout = true;
        } else {
            this.timeoutContext = default;
        }
        ScheduleType = builder.ScheduleType;
        // 双向绑定
        promise.SetTask(this);
    }

    /** 用于简单情况下的对象创建 */
    internal UniScheduledPromiseTask(object action, IContext? context, int options, IScheduledPromise<T> promise, int taskType,
                                     long id, long nextTriggerTime)
        : base(action, context, options, promise, taskType) {
        this.id = id;
        this.nextTriggerTime = nextTriggerTime;
        this.period = 0;
        // 双向绑定
        promise.SetTask(this);
    }

    #region internal

    public long Id {
        get => id;
        set => id = value;
    }

    public long NextTriggerTime {
        get => nextTriggerTime;
        set => nextTriggerTime = value;
    }

    /** 任务的调度类型 -- 应该在添加到队列之前设置 */
    public int ScheduleType {
        get => (ctl & PromiseTask.MASK_SCHEDULE_TYPE) >> PromiseTask.OFFSET_SCHEDULE_TYPE;
        set => ctl |= (value << PromiseTask.OFFSET_SCHEDULE_TYPE);
    }

    /// <summary>
    /// 任务的优先级，范围 [0, 255]
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Priority {
        get => (ctl & PromiseTask.MASK_PRIORITY);
        set {
            if (value < 0 || value > PromiseTask.MAX_PRIORITY) {
                throw new ArgumentException("priority: " + PromiseTask.MAX_PRIORITY);
            }
            ctl &= ~PromiseTask.MASK_PRIORITY;
            ctl |= (value);
        }
    }

    /** 是否已经声明任务的归属权 */
    public bool IsClaimed => (ctl & PromiseTask.MASK_CLAIMED) != 0;

    /** 将任务标记为已申领 */
    public void SetClaimed() {
        ctl |= PromiseTask.MASK_CLAIMED;
    }

    private void SetTriggered() => ctl |= PromiseTask.MASK_TRIGGERED;

    public bool IsTriggered => (ctl & PromiseTask.MASK_TRIGGERED) != 0;

    public override IScheduledPromise<T> Future => (IScheduledPromise<T>)promise;

    public bool IsPeriodic => ScheduleType != 0;

    public int CollectionIndex(object collection) {
        return queueIndex;
    }

    public void CollectionIndex(object collection, int index) {
        this.queueIndex = index;
    }

    protected override void Clear() {
        base.Clear();
        CloseRegistration();
    }

    private bool HasTimeout {
        get => (ctl & PromiseTask.MASK_TIMEOUT) != 0;
        set => SetCtlBit(PromiseTask.MASK_TIMEOUT, value);
    }

    #endregion

    #region core

    private AbstractUniScheduledExecutor EventLoop => (AbstractUniScheduledExecutor)promise.Executor!;

    /** 该方法在任务出队列的时候调用 */
    public override void Run() {
        AbstractUniScheduledExecutor eventLoop = EventLoop;
        IPromise<T> promise = this.promise;
        IContext context = this.context;
        if (promise.IsDone || context.CancelToken.IsCancelling) {
            CancelWithoutRemove(CancelCodes.REASON_DEFAULT);
            return;
        }
        long tickTime = eventLoop.TickTime;
        if (tickTime < nextTriggerTime) { // 显式测试一次，适应多种EventLoop
            eventLoop.ReSchedulePeriodic(this, false);
            return;
        }
        if (Trigger(tickTime)) {
            eventLoop.ReSchedulePeriodic(this, true);
        }
    }

    public bool Trigger(long tickTime) {
        // 标记为已触发
        SetTriggered();

        int scheduleType = ScheduleType;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            base.Run();
            return false;
        }

        IPromise<T> promise = this.promise;
        IContext context = this.context;
        if (context.CancelToken.IsCancelling) {
            TrySetCancelled(promise, context);
            Clear();
            return false;
        }
        if (!IsClaimed) {
            if (!promise.TrySetComputing()) {
                Clear();
                return false;
            }
            SetClaimed();
        } else if (!promise.IsComputing) {
            Clear();
            return false;
        }
        // 结构体，避免拷贝...
        ref TimeoutContext timeoutContext = ref this.timeoutContext;
        try {
            if (HasTimeout) {
                timeoutContext.BeforeCall(tickTime, nextTriggerTime, scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE);
                if (TaskOption.IsEnabled(options, TaskOption.TIMEOUT_BEFORE_RUN) && timeoutContext.IsTimeout()) {
                    promise.TrySetException(StacklessTimeoutException.INST);
                    Clear();
                    return false;
                }
            }

            // 普通周期性任务没有结果
            RunTask();

            // 任务执行后检测取消
            if (context.CancelToken.IsCancelling || !promise.IsComputing) {
                TrySetCancelled(promise, context);
                Clear();
                return false;
            }
            // 未被取消的情况下检测超时
            if (HasTimeout && timeoutContext.IsTimeout()) {
                promise.TrySetException(StacklessTimeoutException.INST);
                Clear();
                return false;
            }

            SetNextRunTime(tickTime, ref timeoutContext, scheduleType);
            return true;
        }
        catch (Exception ex) {
            ThreadUtil.RecoveryInterrupted(ex);
            if (CanCaughtException(ex)) {
                FutureLogger.LogCause(ex, "periodic task caught exception");
                SetNextRunTime(tickTime, ref timeoutContext, scheduleType);
                return true;
            }
            promise.TrySetException(ex);
            Clear();
            return false;
        }
    }

    private bool CanCaughtException(Exception ex) {
        if (ScheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        return TaskOption.IsEnabled(options, TaskOption.CAUGHT_EXCEPTION);
    }

    private void SetNextRunTime(long tickTime, ref TimeoutContext timeoutContext, int scheduleType) {
        long maxDelay = HasTimeout ? timeoutContext.timeLeft : long.MaxValue;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            nextTriggerTime = nextTriggerTime + Math.Min(maxDelay, period); // 逻辑时间
        } else {
            nextTriggerTime = tickTime + Math.Min(maxDelay, period); // 真实时间
        }

        throw new NotImplementedException();
    }

    /** 该接口只能在EventLoop内调用 -- 且当前任务已弹出队列 */
    public void CancelWithoutRemove(int code = CancelCodes.REASON_SHUTDOWN) {
        TrySetCancelled(promise, context, code);
        Clear();
    }

    /** 监听取消令牌中的取消信号 */
    public void RegisterCancellation() {
        // C# 的future中无取消方法，因此只需要监听取消令牌
        ICancelToken cancelToken = context.CancelToken;
        if (cancelToken == ICancelToken.NONE) {
            return;
        }
        if (cancelRegistration == null && cancelToken.CanBeCancelled) {
            cancelRegistration = cancelToken.ThenNotify(this);
        }
    }

    public void OnCancelRequested(ICancelToken cancelToken) {
        // 用户通过令牌发起取消
        if (promise.TrySetCancelled(cancelToken.CancelCode) && !cancelToken.IsWithoutRemove) {
            EventLoop.RemoveScheduled(this);
        }
    }

    private void CloseRegistration() {
        IRegistration registration = this.cancelRegistration;
        if (registration != null) {
            this.cancelRegistration = null;
            registration.Dispose();
        }
    }

    #endregion
}