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
    public static PromiseTask<int> OfTask(ITask task, ICancelToken? cancelToken, int options,
                                          ValuePromise<int> promise) {
        return PromiseTask<int>.Acquire(TYPE_TASK, task, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<int> OfAction(Action action, ICancelToken? cancelToken, int options,
                                            ValuePromise<int> promise) {
        return PromiseTask<int>.Acquire(TYPE_ACTION, action, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<int> OfAction(Action<object> action, object? ctx, int options,
                                            ValuePromise<int> promise) {
        return PromiseTask<int>.Acquire(TYPE_ACTION_CTX, action, ctx, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfFunction<T>(Func<T> action, ICancelToken? cancelToken, int options,
                                               ValuePromise<T> promise) {
        return PromiseTask<T>.Acquire(TYPE_FUNC, action, cancelToken, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfFunction<T>(Func<object, T> action, object? ctx, int options,
                                               ValuePromise<T> promise) {
        return PromiseTask<T>.Acquire(TYPE_FUNC_CTX, action, ctx, options, promise);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PromiseTask<T> OfBuilder<T>(in TaskBuilder<T> builder, ValuePromise<T> promise) {
        return PromiseTask<T>.Acquire(builder.Type, builder.Task, builder.Context, builder.Options, promise);
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
    internal object ctx;
    /** 任务的调度选项 */
    protected int options;
    /** 任务关联的promise -- 不会返回给用户 */
    protected ValuePromise<T> promise;
    /** 任务的控制标记 */
    protected int ctl;
#nullable restore

    protected PromiseTask() {
    }

    protected void Init(int taskType, object action, object? ctx, int options, ValuePromise<T> promise) {
        this.task = action ?? throw new ArgumentNullException(nameof(action));
        this.ctx = ctx;
        this.options = options;
        this.promise = promise ?? throw new ArgumentNullException(nameof(promise));

        // this.ctl = (options & TaskOptions.MASK_CTL_RESERVED);
        this.ctl |= (taskType << PromiseTask.OFFSET_TASK_TYPE);
    }

    protected virtual void Reset() {
        this.task = null;
        this.ctx = null;
        this.options = 0;
        this.promise = null;
        this.ctl = 0;
    }

    /** 准备回收 */
    protected virtual void PrepareToRecycle() {
        if (GetType() == typeof(PromiseTask<T>)) {
            POOL.Release(this);
        }
    }

    #region Props

    public int Options => options;

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public int TaskType => (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;

    /** 任务是否启用了指定选项 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(int taskOption) {
        return TaskOptions.IsEnabled(options, taskOption);
    }

    #endregion

    /** 获取上下文中的取消令牌 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ICancelToken GetCancelToken() {
        return ExecutorUtil.GetCancelToken(ctx, options);
    }

    /** 运行可直接得出结果的任务 */
    protected T RunTask() {
        int type = (ctl & PromiseTask.MASK_TASK_TYPE) >> PromiseTask.OFFSET_TASK_TYPE;
        switch (type) {
            case TYPE_ACTION: {
                Action task = (Action)this.task;
                task();
                return default;
            }
            case TYPE_FUNC: {
                Func<T> task = (Func<T>)this.task;
                return task();
            }
            case TYPE_ACTION_CTX: {
                Action<object> task = (Action<object>)this.task;
                task(ctx);
                return default;
            }
            case TYPE_FUNC_CTX: {
                Func<object, T> task = (Func<object, T>)this.task;
                return task(ctx);
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
        ValuePromise<T> promise = this.promise;
        ICancelToken cancelToken = GetCancelToken();
        if (cancelToken.IsCancelRequested) {
            TrySetCancelled(promise, cancelToken);
            return;
        }
        if (!promise.Internal_TrySetComputing()) {
            return;
        }
        try {
            T value = RunTask();
            promise.Internal_TrySetResult(value);
        }
        catch (Exception e) {
            promise.Internal_TrySetException(e);
        }
        PrepareToRecycle();
    }

    #region util

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool TrySetCancelled(ValuePromise<T> promise, ICancelToken cancelToken) {
        int cancelCode = cancelToken.CancelCode;
        Debug.Assert(cancelCode != 0);
        return promise.Internal_TrySetCancelled(cancelCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool TrySetCancelled(ValuePromise<T> promise, ICancelToken cancelToken, int def) {
        int cancelCode = cancelToken.CancelCode;
        if (cancelCode == 0) cancelCode = def;
        return promise.Internal_TrySetCancelled(cancelCode);
    }

    #endregion

    #region factory

    private static readonly ConcurrentObjectPool<PromiseTask<T>> POOL =
        new(() => new PromiseTask<T>(), e => e.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolType.PromiseTask));

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
    public static PromiseTask<T> Acquire(int taskType, object action, object? ctx, int options,
                                         ValuePromise<T> promise) {
        PromiseTask<T> promiseTask = POOL.Acquire();
        promiseTask.Init(taskType, action, ctx, options, promise);
        return promiseTask;
    }

    #endregion
}
}