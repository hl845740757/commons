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
using System.Runtime.CompilerServices;
using System.Threading;
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
    public static ScheduledPromiseTask<int> OfEmpty(CancellationToken cancelToken, int options, ValuePromise<int> promise) {
        return ScheduledPromiseTask<int>.Acquire(TYPE_EMPTY, null!, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<int> OfAction(Action action, CancellationToken cancelToken, int options, ValuePromise<int> promise) {
        return ScheduledPromiseTask<int>.Acquire(TYPE_ACTION, action, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<int> OfAction(Action<object> action, object ctx, int options, ValuePromise<int> promise) {
        return ScheduledPromiseTask<int>.Acquire(TYPE_ACTION_CTX, action, ctx, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> OfFunction<T>(Func<T> action, CancellationToken cancelToken, int options, ValuePromise<T> promise) {
        return ScheduledPromiseTask<T>.Acquire(TYPE_FUNC, action, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> OfFunction<T>(Func<object, T> action, object ctx, int options, ValuePromise<T> promise) {
        return ScheduledPromiseTask<T>.Acquire(TYPE_FUNC_CTX, action, ctx, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScheduledPromiseTask<T> OfBuilder<T>(in ScheduledTaskBuilder<T> builder, ValuePromise<T> promise) {
        return ScheduledPromiseTask<T>.Acquire(builder.Type, builder.Task, builder.Context, builder.Options, promise);
    }

    #endregion
}

/// <summary>
/// 1.不论有多少处更新promise状态的逻辑，都必须在EventLoop下完成。
/// 2.由于存在多处修改<see cref="ValuePromise{T}"/>状态的情况，因此需要校验rid -- 但都在EventLoop线程更新Promise。
/// 3.因此Task不主动调用回收，而是由调度器确定没有持有者后再触发回收。
///
/// TODO 或可不继承<see cref="PromiseTask{T}"/>，而是统一装箱结果以优化对象池。
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

    /** 在队列中的下标 */
    private int qIndex = IIndexedElement.IndexNotFound;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private CancellationTokenRegistration registration;
    /** 用于避免具体类型依赖 */
    private ISchedulerHelper helper;
#nullable restore

    private ScheduledPromiseTask() {
    }

    protected override void Reset() {
        base.Reset();
        id = -1;
        triggerTime = 0;
        period = 0;
        deadline = 0;
        countdown = 0;

        CloseRegistration();
        qIndex = IIndexedElement.IndexNotFound;
        registration = default;
        helper = null;
    }

    #region 基础属性

    public ISchedulerHelper Helper {
        get => helper;
        set => helper = value;
    }
    public long Id {
        get => id;
        set => id = value;
    }

    public int ScheduleType {
        get => (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
        set => ctl = BitFlags.SetField(ctl, MASK_SCHEDULE_TYPE, OFFSET_SCHEDULE_TYPE, value);
    }

    public long TriggerTime {
        get => triggerTime;
        set => triggerTime = value;
    }

    public long Period {
        get => period;
        set => period = value;
    }

    public long Deadline {
        get => deadline;
        set {
            deadline = value;
            SetCtlBit(MASK_HAS_DEADLINE, true);
        }
    }
    public int Countdown {
        get => countdown;
        set {
            countdown = value;
            SetCtlBit(MASK_HAS_COUNTDOWN, true);
        }
    }

    public bool HasDeadline {
        get => (ctl & MASK_HAS_DEADLINE) != 0;
        set => SetCtlBit(MASK_HAS_DEADLINE, value);
    }

    public bool HasCountdown {
        get => (ctl & MASK_HAS_COUNTDOWN) != 0;
        set => SetCtlBit(MASK_HAS_COUNTDOWN, value);
    }

    public bool IsPeriodic => ScheduleType != 0;
    public bool IsTriggered => (ctl & MASK_TRIGGERED) != 0;

    public CancellationTokenRegistration Registration {
        get => registration;
        set => registration = value;
    }

    public int CollectionIndex(object collection) {
        return qIndex;
    }

    public void CollectionIndex(object collection, int index) {
        this.qIndex = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtlBit(int mask, bool enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    #endregion

    #region core

    public void Cancel(CancellationToken cts = default) {
        Debug.Assert(helper.InEventLoop());
        TrySetCancelled(cts);
    }

    /** 该方法仅在任务出队列的时候调用 */
    public override void Run() {
        if (helper == null) {
            throw new IllegalStateException("helper is uninitialized");
        }
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
        CloseRegistration();
        return false;
    }

    private bool Trigger0(long tickTime) {
        // 标记为已触发
        bool firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }
        // 由于存在多处更新Promise的逻辑，因此先检测Promise的有效性
        ValuePromise<T> promise = this.promise;
        if (promise.IsRecycledOrCompleted(promiseRid)) {
            return false;
        }
        // 先检测取消
        CancellationToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancellationRequested) {
            TrySetCancelled(cancelToken);
            return false;
        }
        // 一次性任务 -- 不能调用基类的Run
        int scheduleType = ScheduleType;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            if (!promise.TrySetComputing(promiseRid)) {
                return false;
            }
            try {
                T value = RunTask();
                TrySetResult(value);
            }
            catch (Exception e) {
                TrySetException(e);
            }
            return false;
        }
        // 周期性任务 -- 已检查Promise状态的情况下，TrySetComputing不应该失败
        if (firstTrigger) {
            if (!promise.TrySetComputing(promiseRid)) {
                return false;
            }
        }
        try {
            if (TaskType == TYPE_ASYNC_TASK) {
                if (firstTrigger) {
                    StartAsyncTask().Forget();
                }
            } else {
                RunTask();
            }
        }
        catch (Exception ex) {
            // 通过异常传递结果
            if (ex is TaskResultException resultException) {
                T? result = resultException.Cast<T>();
                TrySetResult(result);
                return false;
            }
            ThreadUtil.RecoveryInterrupted(ex);
            if (!CanCaughtException(ex)) {
                TrySetException(ex);
                return false;
            }
            FutureLogger.LogCause(ex, "periodic task caught exception");
        }
        // 再次检查Promise的有效性
        if (promise.IsRecycledOrCompleted(promiseRid)) {
            return false;
        }
        // 任务执行后检测取消
        if (cancelToken.IsCancellationRequested) {
            TrySetCancelled(cancelToken);
            return false;
        }
        // 未被取消的情况下检测超时
        if (HasDeadline && deadline <= tickTime) {
            TrySetCancelled(CancellationToken.None);
            return false;
        }
        // 检测次数限制
        if (HasCountdown && (--countdown < 1)) {
            TrySetCancelled(CancellationToken.None);
            return false;
        }
        SetNextRunTime(tickTime, scheduleType);
        return true;
    }

    /// <summary>
    /// 注意：我们用事件驱动代替心跳检测以后，如果当前事件循环是有界队列，任务在其它线程进入完成状态，提交回调到当前线程可能被阻塞
    /// </summary>
    private async ValueFuture StartAsyncTask() {
        Func<AsyncTaskContext, ValueFuture<T>> task = (Func<AsyncTaskContext, ValueFuture<T>>)this.task;
        ValueFuture<T> future = task(new AsyncTaskContext(helper, ctx));
        long taskId = this.id;
        TaskResult<T> taskResult = await future.GetAwaitable(helper.EventLoop, SuppressedTypes.All, TaskOptions.STAGE_TRY_INLINE);
        if (taskId != this.id) {
            return;
        }
        TrySetResult(taskResult);
    }

    private bool CanCaughtException(Exception _) {
        return ScheduleType != ScheduledTaskBuilder.SCHEDULE_ONCE
               && TaskOptions.IsEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void SetNextRunTime(long tickTime, int scheduleType) {
        long maxDelay = HasDeadline ? (deadline - tickTime) : long.MaxValue;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            triggerTime = triggerTime + Math.Min(period, maxDelay); // 逻辑时间
        } else {
            triggerTime = tickTime + Math.Min(period, maxDelay); // 真实时间
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CloseRegistration() {
        this.registration.Dispose();
        this.registration = default;
    }

    #endregion

    #region setResult

    private bool TrySetResult(TaskResult<T> result) {
        if (promise.IsRecycledOrCompleted(promiseRid)) {
            return false;
        }
        if (result.IsSucceeded) {
            return promise.TrySetResult(promiseRid, result.Result);
        } else if (result.IsCancelled) { // 避免不必要的异常堆栈恢复
            return promise.TrySetException(promiseRid, result.Exception!);
        } else {
            return promise.TrySetException(promiseRid, result.ExceptionDispatchInfo!);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TrySetResult(T value) {
        return !promise.IsRecycled(promiseRid) && promise.TrySetResult(promiseRid, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TrySetException(Exception ex) {
        return !promise.IsRecycled(promiseRid) && promise.TrySetException(promiseRid, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TrySetCancelled(CancellationToken cts) {
        return !promise.IsRecycled(promiseRid) && promise.TrySetCancelled(promiseRid, cts);
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
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal new static ScheduledPromiseTask<T> Acquire(int taskType, object action, object? ctx, int options, ValuePromise<T> promise) {
        ScheduledPromiseTask<T> promiseTask = POOL.Acquire();
        promiseTask.Init(taskType, action, ctx, options, promise);
        return promiseTask;
    }

    public void Release() {
        POOL.Release(this);
    }

    #endregion
}
}