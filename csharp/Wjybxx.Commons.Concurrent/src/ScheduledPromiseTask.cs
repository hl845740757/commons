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
using static Wjybxx.Commons.Concurrent.PromiseTask;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public interface ScheduledPromiseTask
{
    #region factory

    public static ScheduledPromiseTask<int> OfTask(ITask task, ICancelToken? cancelToken, int options,
                                                   IScheduledPromise<int> promise, ISchedulerHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(task, cancelToken, options, promise, TaskBuilder.TYPE_TASK,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<int> OfAction(Action action, ICancelToken? cancelToken, int options,
                                                     IScheduledPromise<int> promise, ISchedulerHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(action, cancelToken, options, promise, TaskBuilder.TYPE_ACTION,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<int> OfAction(Action<object> action, object? ctx, int options,
                                                     IScheduledPromise<int> promise, ISchedulerHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<int>(action, ctx, options, promise, TaskBuilder.TYPE_ACTION_CTX,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfFunction<T>(Func<T> action, ICancelToken? cancelToken, int options,
                                                        IScheduledPromise<T> promise, ISchedulerHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<T>(action, cancelToken, options, promise, TaskBuilder.TYPE_FUNC,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfFunction<T>(Func<object, T> action, object? ctx, int options,
                                                        IScheduledPromise<T> promise, ISchedulerHelper helper, long nextTriggerTime) {
        return new ScheduledPromiseTask<T>(action, ctx, options, promise, TaskBuilder.TYPE_FUNC_CTX,
            helper, nextTriggerTime);
    }

    public static ScheduledPromiseTask<T> OfBuilder<T>(in TaskBuilder<T> builder, IScheduledPromise<T> promise, ISchedulerHelper helper) {
        return new ScheduledPromiseTask<T>(builder.Task, builder.Context, builder.Options, promise, builder.Type,
            helper, helper.TickTime);
    }

    public static ScheduledPromiseTask<T> OfBuilder<T>(in ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise, ISchedulerHelper helper) {
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
    private ISchedulerHelper helper;
    /** 在队列中的下标 */
    private int queueIndex = IIndexedElement.IndexNotFound;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private Registration cancelRegistration;
#nullable enable

    internal ScheduledPromiseTask(in ScheduledTaskBuilder<T> builder, IScheduledPromise<T> promise,
                                  ISchedulerHelper helper, long nextTriggerTime, long period)
        : base(builder.Task, builder.Context, builder.Options, promise, builder.Type) {
        this.helper = helper;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        ScheduleType = builder.ScheduleType;
    }

    /** 用于简单情况下的对象创建 */
    internal ScheduledPromiseTask(object action, object? context, int options, IScheduledPromise<T> promise, int taskType,
                                  ISchedulerHelper helper, long nextTriggerTime)
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

    /** 任务的调度类型 -- 应该在添加到队列之前设置 */
    private int ScheduleType {
        get => (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
        set => ctl |= (value << OFFSET_SCHEDULE_TYPE);
    }

    /// <summary>
    /// 任务的优先级，范围 [0, 15]
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Priority {
        get => ctl & TaskOptions.MASK_PRIORITY;
        set => ctl = TaskOptions.SetPriority(ctl, value);
    }

    /** 任务是否已调度过，通常用于降低优先级 */
    public bool IsTriggered => (ctl & MASK_TRIGGERED) != 0;

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
        id = -1;
        nextTriggerTime = 0;
        period = 0;
        helper = null;
    }

    private bool HasTimeout => (ctl & MASK_HAS_DEADLINE) != 0;

    internal void EnableTimeout(long deadline) {
        ctl |= MASK_HAS_DEADLINE;
        this.deadline = deadline;
    }

    private bool HasCountLimit => (ctl & MASK_HAS_COUNTDOWN) != 0;

    internal void EnableCountLimit(int countdown) {
        ctl |= MASK_HAS_COUNTDOWN;
        this.countdown = countdown;
    }

    #endregion

    #region core

    private void Start() {
        if ((ctl & MASK_STARTED) == 0) {
            ctl |= MASK_STARTED;
            RegisterCancellation();
        }
    }

    private void Stop() {
        if ((ctl & MASK_STARTED) != 0 && (ctl & MASK_STOPPED) == 0) {
            ctl |= MASK_STOPPED;
            Clear();
        }
    }

    public override void Cancel(int code) {
        base.Cancel(code);
        if (helper.InEventLoop()) {
            Stop();
        } // else尚未启动
    }

    /** 该方法在任务出队列的时候调用 */
    public override void Run() {
        // 该方法只能执行一次
        if ((ctl & MASK_STARTED) != 0) {
            throw new IllegalStateException();
        }
        // 检测取消和关闭，避免不必要的启动和停止(监听器) -- 取消可能来自EventLoop，所以要测试promise
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelRequested || promise.IsCompleted || helper.IsShutdown) {
            TrySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
            return;
        }
        Start();
        helper.DoSchedule(this);
    }

    /**
     * 外部确定性触发，不需要回调的方式重新压入队列
     *
     * @return 如果需要再压入队列则返回true
     */
    public bool Trigger(long tickTime) {
        if (Trigger0(tickTime)) {
            return true;
        }
        Stop();
        return false;
    }

    /** 返回false的情况下需要调用stop方法 */
    private bool Trigger0(long tickTime) {
        // 标记为已触发
        bool firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }

        int scheduleType = ScheduleType;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            base.Run();
            return false;
        }

        IPromise<T> promise = this.promise;
        ICancelToken cancelToken = GetCancelToken();
        // 为兼容，还要检测来自future的取消，即isComputing...
        if (cancelToken.IsCancelRequested) {
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
            promise.TrySetException(StacklessCancellationException.Timeout);
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
        if (cancelToken.IsCancelRequested || !promise.IsComputing) {
            TrySetCancelled(promise, cancelToken);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasTimeout && deadline <= tickTime) {
            promise.TrySetException(StacklessCancellationException.Timeout);
            return false;
        }
        // 检测次数限制
        if (HasCountLimit && (--countdown < 1)) {
            promise.TrySetException(StacklessCancellationException.TriggerCountLimit);
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
    private void RegisterCancellation() {
        // C# 的future中无取消方法，因此只需要监听取消令牌
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.CanBeCancelled) {
            cancelRegistration = cancelToken.ThenNotify(this);
        }
    }

    /** 关闭取消令牌的监听 */
    private void CloseRegistration() {
        Registration registration = this.cancelRegistration;
        this.cancelRegistration = default;
        registration.Dispose();
    }

    [Obsolete("该方法为中转方法，EventLoop不应该调用")]
    public void OnCancelRequested(ICancelToken cancelToken) {
        // 由EventLoop处理多线程下的可见性问题
        ISchedulerHelper helper = this.helper;
        if (helper == null) {
            return; // cleared
        }
        helper.OnCancelRequested(this, cancelToken.CancelCode);
    }

    #endregion
}
}