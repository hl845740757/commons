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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public interface ScheduledPromiseTask
{
    #region factory

    public static ScheduledPromiseTask<int> OfTask(ITask task, ICancelToken? cancelToken, int options,
                                                   IScheduledPromise<int> promise, IScheduledHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(task, cancelToken, options, promise, TaskBuilder.TYPE_TASK,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<int> OfAction(Action action, ICancelToken? cancelToken, int options,
                                                     IScheduledPromise<int> promise, IScheduledHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(action, cancelToken, options, promise, TaskBuilder.TYPE_ACTION,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<int> OfAction(Action<IContext> action, IContext? context, int options,
                                                     IScheduledPromise<int> promise, IScheduledHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(action, context, options, promise, TaskBuilder.TYPE_ACTION_CTX,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfFunction<T>(Func<T> action, ICancelToken? cancelToken, int options,
                                                        IScheduledPromise<T> promise, IScheduledHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<T>(action, cancelToken, options, promise, TaskBuilder.TYPE_FUNC,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfFunction<T>(Func<IContext, T> action, IContext? context, int options,
                                                        IScheduledPromise<T> promise, IScheduledHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<T>(action, context, options, promise, TaskBuilder.TYPE_FUNC_CTX,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfBuilder<T>(in TaskBuilder<T> builder, IScheduledPromise<T> promise, IScheduledHelper helper) {
        return new ScheduledPromiseTask<T>(builder.Task, builder.Context, builder.Options, promise, builder.Type,
            helper, helper.TickTime);
    }

    public static ScheduledPromiseTask<T> OfBuilder<T>(in ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise, IScheduledHelper helper) {
        long triggerTime = helper.TriggerTime(builder.InitialDelay, builder.Timeunit);
        long period = builder.IsPeriodic
            ? helper.TriggerPeriod(builder.Period, builder.Timeunit)
            : 0;

        ScheduledPromiseTask<T> promiseTask = new ScheduledPromiseTask<T>(in builder, promise, helper, triggerTime, period);
        if (builder.IsPeriodic) {
            if (builder.Timeout != -1) {
                promiseTask.EnableTimeout(helper.TriggerTime(builder.Timeout, builder.Timeunit));
            }
            if (builder.CountLimit != -1) {
                promiseTask.EnableCountLimit(builder.CountLimit);
            }
        }
        return promiseTask;
    }

    #endregion
}

public class ScheduledPromiseTask<T> : PromiseTask<T>,
    IScheduledFutureTask, IIndexedElement, ICancelTokenListener
{
#nullable disable
    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id = -1;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long nextTriggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private long period;

    /** 截止时间 -- 有效性见<see cref="PromiseTask.MASK_HAS_DEADLINE"/> */
    private long deadline;
    /** 剩余次数 -- 有效性见<see cref="PromiseTask.MASK_HAS_COUNTDOWN"/> */
    private int countdown;

    /** 用于避免具体类型依赖 */
    private IScheduledHelper helper;
    /** 在队列中的下标 */
    private int queueIndex = IIndexedElement.IndexNotFound;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private IRegistration cancelRegistration;
#nullable enable

    internal ScheduledPromiseTask(in ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise,
                                  IScheduledHelper helper, long nextTriggerTime, long period)
        : base(builder.Task, builder.Context, builder.Options, promise, builder.Type) {
        this.helper = helper;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        ScheduleType = builder.ScheduleType;
    }

    /** 用于简单情况下的对象创建 */
    internal ScheduledPromiseTask(object action, object? context, int options, IScheduledPromise<T> promise, int taskType,
                                  IScheduledHelper helper, long nextTriggerTime)
        : base(action, context, options, promise, taskType) {
        this.helper = helper;
        this.nextTriggerTime = nextTriggerTime;
        this.period = 0;
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

    /** 任务是否已调度过，通常用于降低优先级 */
    public bool IsTriggered => (ctl & PromiseTask.MASK_TRIGGERED) != 0;

    /** 任务的调度类型 -- 应该在添加到队列之前设置 */
    private int ScheduleType {
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

    public bool IsPeriodic => ScheduleType != 0;

    public int CollectionIndex(object collection) {
        return queueIndex;
    }

    public void CollectionIndex(object collection, int index) {
        this.queueIndex = index;
    }

    public override void Clear() {
        base.Clear();
        CloseRegistration();
        id = -1;
        nextTriggerTime = 0;
        period = 0;
        helper = null;
    }

    private bool HasTimeout => (ctl & PromiseTask.MASK_HAS_DEADLINE) != 0;

    internal void EnableTimeout(long deadline) {
        ctl |= PromiseTask.MASK_HAS_DEADLINE;
        this.deadline = deadline;
    }

    private bool HasCountLimit => (ctl & PromiseTask.MASK_HAS_COUNTDOWN) != 0;

    internal void EnableCountLimit(int countdown) {
        ctl |= PromiseTask.MASK_HAS_COUNTDOWN;
        this.countdown = countdown;
    }

    #endregion

    #region core

    /** 该方法在任务出队列的时候调用 */
    public override void Run() {
        long tickTime = helper.TickTime;
        // 显式测试一次时间，适应多种EventLoop
        if (tickTime < nextTriggerTime) {
            // 未达触发时间时，显式测试一次取消
            ICancelToken cancelToken = GetCancelToken();
            if (cancelToken.IsCancelling || promise.IsCompleted) {
                TrySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
                helper.OnCompleted(this);
            } else {
                helper.Reschedule(this);
            }
            return;
        }
        if (Trigger(tickTime)) {
            helper.Reschedule(this);
        } else {
            helper.OnCompleted(this);
        }
    }

    public bool Trigger(long tickTime) {
        // 标记为已触发
        bool firstTrigger = (ctl & PromiseTask.MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= PromiseTask.MASK_TRIGGERED;
        }

        int scheduleType = ScheduleType;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            base.Run();
            return false;
        }

        IPromise<T> promise = this.promise;
        ICancelToken cancelToken = GetCancelToken();
        // 为兼容，还要检测来自future的取消，即isComputing...
        if (cancelToken.IsCancelling) {
            TrySetCancelled(promise, cancelToken);
            return false;
        }
        if (firstTrigger) {
            if (!promise.TrySetComputing()) {
                return false;
            }
        } else if (!promise.IsComputing) {
            return false;
        }

        if (TaskOptions.IsEnabled(options, TaskOptions.TIMEOUT_BEFORE_RUN)
            && HasTimeout && deadline <= tickTime) {
            promise.TrySetException(StacklessTimeoutException.INST);
            return false;
        }
        try {
            if (TaskType == TaskBuilder.TYPE_TIMESHARING) {
                // 周期性任务，只有分时任务可以有结果
                if (RunTimeSharing(firstTrigger, out T result)) {
                    promise.TrySetResult(result);
                    return false;
                }
            } else {
                RunTask();
            }
        }
        catch (Exception ex) {
            ThreadUtil.RecoveryInterrupted(ex);
            if (!CanCaughtException(ex)) {
                promise.TrySetException(ex);
                return false;
            }
            FutureLogger.LogCause(ex, "periodic task caught exception");
        }
        // 任务执行后检测取消
        if (cancelToken.IsCancelling || !promise.IsComputing) {
            TrySetCancelled(promise, cancelToken);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasTimeout && deadline <= tickTime) {
            promise.TrySetException(StacklessTimeoutException.INST);
            return false;
        }
        // 检测次数限制
        if (HasCountLimit && (--countdown < 1)) {
            promise.TrySetException(StacklessTimeoutException.INST_COUNT_LIMIT);
            return false;
        }
        SetNextRunTime(tickTime, scheduleType);
        return true;
    }

    private bool CanCaughtException(Exception ex) {
        if (ScheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        if (TaskType == TaskBuilder.TYPE_TIMESHARING) {
            return false;
        }
        return TaskOptions.IsEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void SetNextRunTime(long tickTime, int scheduleType) {
        long maxDelay = HasTimeout ? (deadline - tickTime) : long.MaxValue;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            nextTriggerTime = nextTriggerTime + Math.Clamp(period, 1, maxDelay); // 逻辑时间
        } else {
            nextTriggerTime = tickTime + Math.Clamp(period, 1, maxDelay); // 真实时间
        }
    }

    /** 监听取消令牌中的取消信号 */
    public void RegisterCancellation() {
        // C# 的future中无取消方法，因此只需要监听取消令牌
        ICancelToken cancelToken = GetCancelToken();
        if (cancelRegistration == null && cancelToken.CanBeCancelled) {
            cancelRegistration = cancelToken.ThenNotify(this);
        }
    }

    [Obsolete("该方法为中转方法，EventLoop不应该调用")]
    public void OnCancelRequested(ICancelToken cancelToken) {
        // 用户通过令牌发起取消
        helper.OnCancelRequested(this, cancelToken.CancelCode);
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
}