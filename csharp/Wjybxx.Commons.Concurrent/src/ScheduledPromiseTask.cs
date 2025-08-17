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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using static Wjybxx.Commons.Concurrent.PromiseTask;
using static Wjybxx.Commons.Concurrent.TaskBuilder;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public static class ScheduledPromiseTask
{
    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<int> OfAction(Action action, ICancelToken? cancelToken, int options,
                                                     ValuePromise<int> promise,
                                                     TimeSpan delay) {
        return ScheduledPromiseTask<int>.Acquire(TYPE_ACTION, action, cancelToken, options, promise, delay);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> OfFunction<T>(Func<T> action, ICancelToken? cancelToken, int options,
                                                        ValuePromise<T> promise,
                                                        TimeSpan delay) {
        return ScheduledPromiseTask<T>.Acquire(TYPE_FUNC, action, cancelToken, options, promise, delay);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> OfBuilder<T>(in ScheduledTaskBuilder<T> builder,
                                                       ValuePromise<T> promise) {
        return ScheduledPromiseTask<T>.Acquire(in builder, promise);
    }

    #endregion
}

/// <summary>
/// 1.该类的数据是（部分）开放的，以支持不同的扩展。
/// 2.未继承<see cref="ValuePromise{T}"/>，各执行各的池化
/// 3.该对象不可返回给用户！否则可能导致内存泄漏，复用错误。
///
/// TODO 或可不继承<see cref="PromiseTask{T}"/>，而是统一装箱结果。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ScheduledPromiseTask<T> : PromiseTask<T>, IScheduledFutureTask, IIndexedElement
{
#nullable disable
    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id = -1;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long triggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private long period;

    /** 截止时间 -- 有效性见<see cref="PromiseTask.MASK_HAS_DEADLINE"/> */
    private long deadline;
    /** 剩余次数 -- 有效性见<see cref="PromiseTask.MASK_HAS_COUNTDOWN"/> */
    private int countdown;

    /** 用于避免具体类型依赖 */
    private ISchedulerHelper helper;
    /** 在队列中的下标 */
    private int qIndex = IIndexedElement.IndexNotFound;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private Registration cancelRegistration;
    /** 异步任务的结果 */
    private ValueFuture<T> asyncResult;
#nullable restore

    private ScheduledPromiseTask() {
    }

    /// <summary>
    /// 用于简单情况下的Init
    /// </summary>
    internal void Init(int taskType, object action, object? ctx, int options,
                       ValuePromise<T> promise, TimeSpan delay) {
        base.Init(taskType, action, ctx, options, promise);
        this.triggerTime = delay.Ticks; // 先记录为Tick数
        this.period = 0;
    }

    internal void Init(in ScheduledTaskBuilder<T> builder, ValuePromise<T> promise) {
        base.Init(builder.Type, builder.Task, builder.Context, builder.Options, promise);
        ScheduleType = builder.ScheduleType;
        // 时间戳先保存为ticks单位
        long timeUnit = builder.TimeUnit.Ticks;
        this.triggerTime = builder.InitialDelay * timeUnit;
        this.period = builder.Period * timeUnit;
        // 初始化周期任务数据
        if (builder.IsPeriodic) {
            if (period <= 0) throw new Exception("period: " + period);
            if (builder.HasTimeout) {
                ctl |= MASK_HAS_DEADLINE;
                this.deadline = builder.Timeout * timeUnit;
            }
            if (builder.HasCountLimit) {
                ctl |= MASK_HAS_COUNTDOWN;
                this.countdown = builder.CountLimit;
            }
        }
    }

    /// <summary>
    /// 事件循环在将任务插入到队列时调用该方法初始化任务
    /// </summary>
    /// <param name="helper">事件循环的helper</param>
    public void Inject(ISchedulerHelper helper) {
        this.helper = helper;
        TimeSpan timeUnit = new TimeSpan(1);
        this.triggerTime = helper.TriggerTime(triggerTime, timeUnit);
        if (IsPeriodic) {
            this.period = helper.TriggerPeriod(period, timeUnit);
        }
        // 这里第二次读取TickTime，Deadline可能大于预期值，问题不大
        if (HasDeadline) {
            this.deadline = helper.TriggerTime(deadline, timeUnit);
        }
    }

    #region internal

    public long Id {
        get => id;
        set => id = value;
    }

    public long TriggerTime {
        get => triggerTime;
        set => triggerTime = value;
    }

    /** 任务的调度类型 -- 应该在添加到队列之前设置 */
    private int ScheduleType {
        get => (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
        set => ctl |= (value << OFFSET_SCHEDULE_TYPE);
    }

    /// <summary>
    /// 任务的优先级，范围 [0, 31]
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Priority {
        get => TaskOptions.GetPriority(options);
        set => options = TaskOptions.SetPriority(options, value);
    }

    public bool IsTriggered => (ctl & MASK_TRIGGERED) != 0;

    public bool IsPeriodic => ScheduleType != 0;

    public int CollectionIndex(object collection) {
        return qIndex;
    }

    public void CollectionIndex(object collection, int index) {
        this.qIndex = index;
    }

    private bool HasDeadline => (ctl & MASK_HAS_DEADLINE) != 0;

    private bool HasCountdown => (ctl & MASK_HAS_COUNTDOWN) != 0;

    #endregion

    #region core

    protected override void PrepareToRecycle() {
        CloseRegistration();
        POOL.Release(this); // sealed class
    }

    protected override void Reset() {
        base.Reset();
        id = -1;
        triggerTime = 0;
        period = 0;
        deadline = 0;
        countdown = 0;
        helper = null;
        cancelRegistration = default;
        asyncResult = default;
    }

    public void Cancel(int code) {
        // 只支持在EventLoop线程主动取消，否则存在数据可见性问题
        if (helper == null || !helper.InEventLoop()) {
            throw new IllegalStateException();
        }
        TrySetCancelled(promise, GetCancelToken(), code);
        PrepareToRecycle();
    }

    /** 该方法在任务出队列的时候调用 */
    public override void Run() {
        if (helper == null) {
            throw new IllegalStateException("helper is uninitialized");
        }
        // 该方法只能执行一次
        if ((ctl & MASK_STARTED) != 0) {
            throw new IllegalStateException();
        }
        ctl |= MASK_STARTED;

        // 检测取消和关闭，避免不必要的启动和停止(监听器)
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelRequested || helper.IsShutdown) {
            TrySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
            PrepareToRecycle();
            return;
        }
        // 先监听取消信号
        RegisterCancellation();
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
        PrepareToRecycle();
        return false;
    }

    /** 返回false的情况下需要调用stop方法 */
    private bool Trigger0(long tickTime) {
        // 标记为已触发
        bool firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }
        // 先检测取消
        ValuePromise<T> promise = this.promise;
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelRequested) {
            TrySetCancelled(promise, cancelToken);
            return false;
        }
        // 一次性任务 -- 不能调用基类的Run
        int scheduleType = ScheduleType;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            if (!promise.Internal_TrySetComputing()) {
                return false;
            }
            try {
                T value = RunTask();
                promise.Internal_TrySetResult(value);
            }
            catch (Exception e) {
                promise.Internal_TrySetException(e);
            }
            return false;
        }
        // 周期性任务
        if (firstTrigger) {
            if (!promise.Internal_TrySetComputing()) {
                return false;
            }
        } else if (!promise.IsComputing) {
            return false;
        }
        try {
            if (TaskType == TYPE_ASYNC_TASK) {
                if (firstTrigger) {
                    Func<AsyncTaskContext, ValueFuture<T>> task = (Func<AsyncTaskContext, ValueFuture<T>>)this.task;
                    AsyncTaskContext context = new AsyncTaskContext(helper, ctx);
                    asyncResult = task(context);
                }
                if (asyncResult.IsCompleted) {
                    TaskResult<T> result = asyncResult.GetResult(SuppressedTypes.All);
                    promise.Internal_TrySetResult(result);
                    return false;
                }
            } else {
                RunTask();
            }
        }
        catch (Exception ex) {
            // 通过异常传递结果
            if (ex is TaskResultException resultException) {
                T? result = resultException.Cast<T>();
                promise.Internal_TrySetResult(result);
                return false;
            }
            ThreadUtil.RecoveryInterrupted(ex);
            if (!CanCaughtException(ex)) {
                promise.Internal_TrySetException(ex);
                return false;
            }
            FutureLogger.LogCause(ex, "periodic task caught exception");
        }
        // 任务执行后检测取消
        if (cancelToken.IsCancelRequested || !promise.IsComputing) {
            TrySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasDeadline && deadline <= tickTime) {
            promise.Internal_TrySetException(StacklessCancellationException.Timeout);
            return false;
        }
        // 检测次数限制
        if (HasCountdown && (--countdown < 1)) {
            promise.Internal_TrySetException(StacklessCancellationException.TriggerCountLimit);
            return false;
        }
        SetNextRunTime(tickTime, scheduleType);
        return true;
    }

    private bool CanCaughtException(Exception ex) {
        if (ScheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        return TaskOptions.IsEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void SetNextRunTime(long tickTime, int scheduleType) {
        long maxDelay = HasDeadline ? (deadline - tickTime) : long.MaxValue;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            triggerTime = triggerTime + Math.Min(period, maxDelay); // 逻辑时间
        } else {
            triggerTime = tickTime + Math.Min(period, maxDelay); // 真实时间
        }
    }

    /** 监听取消令牌中的取消信号 -- 理论上由helper来监听更好 */
    private void RegisterCancellation() {
        // C# 的future中无取消方法，因此只需要监听取消令牌
        // 注意：监听需要回调给Helper，参数为taskId -- 不能回调给自己，否则可能对象复用bug
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.CanBeCancelled) {
            cancelRegistration = cancelToken.ThenNotify(helper, id);
        }
    }

    /** 关闭取消令牌的监听 */
    private void CloseRegistration() {
        Registration registration = this.cancelRegistration;
        this.cancelRegistration = default;
        registration.Dispose();
    }

    #endregion

    #region factory

    private static readonly ConcurrentObjectPool<ScheduledPromiseTask<T>> POOL =
        new(() => new ScheduledPromiseTask<T>(), task => task.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolType.ScheduledPromiseTask));

    /// <summary>
    /// 申请一个PromiseTask对象，Task在进入完成状态后会自动回收。
    /// 注意：该对象不可返回给用户！该对象不可返回给用户！该对象不可返回给用户！
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="action">任务</param>
    /// <param name="ctx">任务关联上下文</param>
    /// <param name="options">任务调度选项</param>
    /// <param name="promise">关联的Promise</param>
    /// <param name="delay">延迟时间</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> Acquire(int taskType, object action, object? ctx, int options,
                                                  ValuePromise<T> promise,
                                                  TimeSpan delay) {
        ScheduledPromiseTask<T> promiseTask = POOL.Acquire();
        promiseTask.Init(taskType, action, ctx, options, promise, delay);
        return promiseTask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> Acquire(in ScheduledTaskBuilder<T> builder, ValuePromise<T> promise) {
        ScheduledPromiseTask<T> promiseTask = POOL.Acquire();
        promiseTask.Init(builder, promise);
        return promiseTask;
    }

    #endregion
}
}