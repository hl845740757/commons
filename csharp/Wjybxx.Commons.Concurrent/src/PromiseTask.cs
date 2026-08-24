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
using System.Runtime.ExceptionServices;
using System.Threading;
using Wjybxx.Commons.Pool;
using static Wjybxx.Commons.Concurrent.TaskBuilder;

#pragma warning disable CS8603

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 接口用于定义常量和工具方法
/// </summary>
public static class PromiseTask
{
    /** 任务类型的偏移量 */
    public const int OFFSET_TASK_TYPE = 8;
    /** 调度类型的偏移量 - 理论上3个Bit足够 */
    public const int OFFSET_SCHEDULE_TYPE = 12;

    // 低8位和TaskOptions保持一致，以方便继承数据
    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    public const int MASK_TASK_TYPE = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种，可支持复杂的调度 */
    public const int MASK_SCHEDULE_TYPE = 0xF000;

    /** 延时任务是否已触发过 */
    public const int MASK_TRIGGERED = 1 << 16;
    /** 延时任务有超时时间 -- 识别结构体的有效性 */
    public const int MASK_HAS_DEADLINE = 1 << 17;
    /** 延时任务有次数限制 */
    public const int MASK_HAS_COUNTDOWN = 1 << 18;
    /** 任务是否启动了 */
    public const int MASK_STARTED = 1 << 19;
    /** 任务是否已停止 */
    public const int MASK_STOPPED = 1 << 20;

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<int> OfTask(ValuePromise<int> promise, ITask task,
                                          CancellationToken cancelToken = default, int options = 0) {
        return PromiseTask<int>.Acquire(promise, TYPE_TASK, task, null, cancelToken, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<int> OfAction(ValuePromise<int> promise, Action action,
                                            CancellationToken cancelToken = default, int options = 0) {
        return PromiseTask<int>.Acquire(promise, TYPE_ACTION, action, null, cancelToken, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<int> OfAction(ValuePromise<int> promise, Action<object> action, object? state,
                                            CancellationToken cancelToken = default, int options = 0) {
        return PromiseTask<int>.Acquire(promise, TYPE_ACTION_STATE, action, state, cancelToken, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfFunction<T>(ValuePromise<T> promise, Func<T> action,
                                               CancellationToken cancelToken = default, int options = 0) {
        return PromiseTask<T>.Acquire(promise, TYPE_FUNC, action, null, cancelToken, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfFunction<T>(ValuePromise<T> promise, Func<object, T> action, object? state,
                                               CancellationToken cancelToken = default, int options = 0) {
        return PromiseTask<T>.Acquire(promise, TYPE_FUNC_STATE, action, state, cancelToken, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfBuilder<T>(ValuePromise<T> promise, in TaskBuilder<T> builder) {
        return PromiseTask<T>.Acquire(promise, builder.Type, builder.Task, builder.State,
            builder.CancelToken, builder.Options);
    }

    #endregion
}

/// <summary>
/// 1.该类的数据是（部分）开放的，以支持不同的扩展。
/// 2.未继承<see cref="ValuePromise{T}"/>，各执行各的池化
/// 3.该对象不可返回给用户！否则可能导致内存泄漏，复用错误。
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : IFutureTask
{
#nullable disable
    /** 用户的委托 */
    internal object task;
    /** 任务的上下文 */
    internal object state;
    /** 任务的取消令牌 */
    protected CancellationToken cancelToken;
    /** 任务的调度选项 */
    protected int options;

    /** 任务关联的promise -- 不会返回给用户 */
    protected ValuePromise<T> promise;
    /** promise关联的重入id -- 其实仅子类需要 */
    protected int promiseRid;
    /** 任务的控制标记 */
    protected int ctl;
#nullable restore

    protected PromiseTask() {
    }

    protected void Init(ValuePromise<T> promise, int taskType, object task, object? state,
                        CancellationToken cancelToken, int options) {
        if (task == null && taskType != 0) throw new ArgumentNullException(nameof(task));
        this.task = task;
        this.state = state;
        this.cancelToken = cancelToken;
        this.options = options;
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));
        this.promiseRid = promise.ReentryId;

        // this.ctl = (options & TaskOptions.MASK_CTL_RESERVED);
        this.ctl = (taskType << PromiseTask.OFFSET_TASK_TYPE);
    }

    protected virtual void Reset() {
        this.task = null;
        this.state = null;
        this.cancelToken = default;
        this.options = 0;
        this.promise = null;
        this.promiseRid = 0;
        this.ctl = 0;
    }

    #region Props

    public int Options => options;

    public int TaskType => (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;

    //** 任务是否启用了指定选项 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(int taskOption) {
        return TaskOptions.IsEnabled(options, taskOption);
    }

    #endregion

    /** 获取上下文中的取消令牌 */
    public CancellationToken CancelToken {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cancelToken;
    }

    /** 运行可直接得出结果的任务 */
    protected T RunTask() {
        int type = (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;
        switch (type) {
            case TYPE_EMPTY: {
                return default;
            }
            case TYPE_ACTION: {
                Action task = (Action)this.task;
                task();
                return default;
            }
            case TYPE_ACTION_STATE: {
                Action<object> task = (Action<object>)this.task;
                task(state);
                return default;
            }
            case TYPE_FUNC: {
                Func<T> task = (Func<T>)this.task;
                return task();
            }
            case TYPE_FUNC_STATE: {
                Func<object, T> task = (Func<object, T>)this.task;
                return task(state);
            }
            case TYPE_TASK: {
                ITask task = (ITask)this.task;
                task.Run();
                return default;
            }
            default: {
                throw new AssertionError("type: " + type);
            }
        }
    }

    /** 子类不要调用该方法，会导致自动回收 */
    public virtual void Run() {
        // 超类可以直接调用Internal方法，因为不会有其它地方更新Promise
        ValuePromise<T> promise = this.promise;
        CancellationToken cancelToken = CancelToken;
        if (cancelToken.IsCancellationRequested) {
            promise.Internal_TrySetCancelled(cancelToken);
            TryRelease();
            return;
        }
        if (!promise.Internal_TrySetComputing()) {
            TryRelease();
            return;
        }
        try {
            T value = RunTask();
            promise.Internal_TrySetResult(value);
        }
        catch (Exception e) {
            promise.Internal_TrySetException(e);
        }
        TryRelease();
    }

    private void TryRelease() {
        // 要求外部已不持有该对象引用
        if ((options & TaskOptions.MANUAL_RELEASE) == 0 && GetType() == typeof(PromiseTask<T>)) {
            POOL?.Release(this);
        }
    }

    #region factory

    private static readonly ConcurrentObjectPool<PromiseTask<T>>? POOL;

    static PromiseTask() {
        int poolSize = TaskPoolConfig.GetPoolSize<T>(TaskPoolType.PromiseTask);
        if (poolSize > 0) {
            POOL = new ConcurrentObjectPool<PromiseTask<T>>(() => new PromiseTask<T>(), e => e.Reset(), poolSize);
        }
    }

    /// <summary>
    /// 申请一个PromiseTask对象，Task在进入完成状态后会自动回收。
    /// 注意：该对象不可返回给用户！该对象不可返回给用户！该对象不可返回给用户！
    /// </summary>
    /// <param name="promise">关联的Promise</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="action">任务</param>
    /// <param name="state">任务参数</param>
    /// <param name="cancelToken">取消令牌+</param>
    /// <param name="options">任务调度选项</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static PromiseTask<T> Acquire(ValuePromise<T> promise, int taskType, object action, object? state,
                                           CancellationToken cancelToken, int options) {
        PromiseTask<T> promiseTask = POOL != null ? POOL.Acquire() : new PromiseTask<T>();
        promiseTask.Init(promise, taskType, action, state, cancelToken, options);
        return promiseTask;
    }

    /// <summary>
    /// 归还Promise
    /// </summary>
    /// <param name="promiseTask"></param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Release(PromiseTask<T> promiseTask) {
        if (promiseTask.GetType() != typeof(PromiseTask<T>)) {
            throw new ArgumentException(null, nameof(promiseTask));
        }
        POOL?.Release(promiseTask);
    }

    #endregion
}
}