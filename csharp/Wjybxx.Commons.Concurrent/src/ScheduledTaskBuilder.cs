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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 使用接口定义常量
/// </summary>
public static class ScheduledTaskBuilder
{
    /** 执行一次 */
    public const byte SCHEDULE_ONCE = 0;
    /** 固定延迟 -- 两次执行的间隔大于等于给定的延迟 */
    public const byte SCHEDULE_FIXED_DELAY = 1;
    /** 固定频率 -- 执行次数 */
    public const byte SCHEDULE_FIXED_RATE = 2;

    #region factory

    public static ScheduledTaskBuilder<int> NewAction(Action task, CancellationToken cancelToken = default) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewAction(task, cancelToken);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
    }

    public static ScheduledTaskBuilder<int> NewAction(Action<object> task, object? state, CancellationToken cancelToken = default) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewAction(task, state, cancelToken);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
    }

    public static ScheduledTaskBuilder<T> NewFunc<T>(Func<T> task, CancellationToken cancelToken = default) {
        TaskBuilder<T> taskBuilder = TaskBuilder.NewFunc(task, cancelToken);
        return new ScheduledTaskBuilder<T>(ref taskBuilder);
    }

    public static ScheduledTaskBuilder<T> NewFunc<T>(Func<object, T> task, object? state, CancellationToken cancelToken = default) {
        TaskBuilder<T> taskBuilder = TaskBuilder.NewFunc(task, state, cancelToken);
        return new ScheduledTaskBuilder<T>(ref taskBuilder);
    }

    public static ScheduledTaskBuilder<int> NewTask(ITask task, CancellationToken cancelToken = default) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewTask(task, cancelToken);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
    }

    #endregion

    #region 校验

    /** 适用于禁止初始延迟小于0的情况 */
    public static void ValidateInitialDelay(long initialDelay) {
        if (initialDelay < 0) throw new ArgumentException($"initialDelay: {initialDelay} (expected: >= 0)");
    }

    public static void ValidatePeriod(long period) {
        if (period <= 0) throw new ArgumentException($"period: {period}, (expected: > 0)");
    }

    #endregion
}

/// <summary>
/// 定时任务构建器
/// </summary>
/// <typeparam name="T">结果类型，无结果时可使用int，无开销</typeparam>
public struct ScheduledTaskBuilder<T>
{
    /** 不能为readonly否则调用方法会产生拷贝 */
    private TaskBuilder<T> _core;

    private byte scheduleType;
    private long initialDelay;
    private long period;
    private long timeout;
    private int _countLimit;
    private TimeSpan _timeUnit;

    internal ScheduledTaskBuilder(ref TaskBuilder<T> core) {
        _core = core;
        scheduleType = 0;
        initialDelay = 0;
        period = 0;
        timeout = 0;
        _countLimit = 0;
        _timeUnit = TimeSpan.FromMilliseconds(1);
    }

    #region 代理

    /// <summary>
    /// 任务类型
    /// </summary>
    public int Type => _core.Type;
    /// <summary>
    /// 任务对象
    /// </summary>
    public object Task => _core.Task;
    /// <summary>
    /// 任务参数
    /// </summary>
    public object? State {
        get => _core.State;
        set => _core.State = value;
    }
    /// <summary>
    /// 任务取消令牌
    /// </summary>
    public CancellationToken CancelToken {
        get => _core.CancelToken;
        set => _core.CancelToken = value;
    }
    /// <summary>
    /// 任务调度选项
    /// </summary>
    public int Options {
        get => _core.Options;
        set => _core.Options = value;
    }

    /// <summary>
    /// 是否启用了某选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(int optionMask) {
        return _core.IsEnabled(optionMask);
    }

    /// <summary>
    /// 启用特定任务选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enable(int taskOption) {
        _core.Enable(taskOption);
    }

    /// <summary>
    /// 关闭特定任务选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disable(int taskOption) {
        _core.Disable(taskOption);
    }

    /// <summary>
    /// 启用或禁用选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetEnable(int optionMask, bool enable) {
        _core.SetEnable(optionMask, enable);
    }

    #endregion

    #region schedule

    /// <summary>
    /// 时间单位（默认毫秒）
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public TimeSpan TimeUnit {
        get => _timeUnit;
        set {
            if (value.Ticks < 1) {
                throw new ArgumentException("invalid timeunit");
            }
            _timeUnit = value;
        }
    }

    public byte ScheduleType => scheduleType;
    public long InitialDelay => initialDelay;
    public long Period => period;

    /// <summary>
    /// 是否是周期性任务
    /// </summary>
    public bool IsPeriodic => scheduleType != 0;
    /// <summary>
    /// 是否是一次性任务
    /// </summary>
    public bool IsOnlyOnce => scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE;

    /// <summary>
    /// 设置任务为单次执行
    /// </summary>
    /// <param name="delay">触发延迟</param>
    public void SetOnlyOnce(long delay) {
        this.scheduleType = ScheduledTaskBuilder.SCHEDULE_ONCE;
        this.initialDelay = delay;
        this.period = 0;
    }

    /// <summary>
    /// 设置任务为单次执行
    /// </summary>
    /// <param name="delay">触发延迟</param>
    /// <param name="timeunit">时间单位</param>
    public void SetOnlyOnce(long delay, TimeSpan timeunit) {
        SetOnlyOnce(delay);
        TimeUnit = timeunit;
    }

    public bool IsFixedDelay => scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_DELAY;

    /// <summary>
    /// 设置任务为固定延迟执行
    /// </summary>
    /// <param name="initialDelay">首次延迟</param>
    /// <param name="period">循环周期</param>
    public void SetFixedDelay(long initialDelay, long period) {
        ScheduledTaskBuilder.ValidatePeriod(period);
        this.scheduleType = ScheduledTaskBuilder.SCHEDULE_FIXED_DELAY;
        this.initialDelay = initialDelay;
        this.period = period;
    }

    /// <summary>
    /// 设置任务为固定延迟执行
    /// </summary>
    /// <param name="initialDelay"></param>
    /// <param name="period"></param>
    /// <param name="timeunit">时间单位</param>
    public void SetFixedDelay(long initialDelay, long period, TimeSpan timeunit) {
        SetFixedDelay(initialDelay, period);
        TimeUnit = timeunit;
    }

    public bool IsFixedRate => scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE;

    /// <summary>
    /// 设置任务为固定频率执行（会补帧）
    /// </summary>
    /// <param name="initialDelay">首次延迟</param>
    /// <param name="period">循环周期</param>
    public void SetFixedRate(long initialDelay, long period) {
        ScheduledTaskBuilder.ValidateInitialDelay(initialDelay);
        ScheduledTaskBuilder.ValidatePeriod(period);
        this.scheduleType = ScheduledTaskBuilder.SCHEDULE_FIXED_RATE;
        this.initialDelay = initialDelay;
        this.period = period;
    }

    /// <summary>
    /// 设置任务为固定频率执行（会补帧）
    /// </summary>
    /// <param name="initialDelay">首次延迟</param>
    /// <param name="period">循环周期</param>
    /// <param name="timeunit">时间单位</param>
    public void SetFixedRate(long initialDelay, long period, TimeSpan timeunit) {
        SetFixedRate(initialDelay, period);
        TimeUnit = timeunit;
    }

    /// <summary>
    /// 是否设置了超时时间
    /// </summary>
    public bool HasTimeout => _core.IsEnabled(TaskOptions.HAS_TIMEOUT);

    /// <summary>
    /// 1. 默认只在执行任务后检查是否超时，以确保至少会执行一次
    /// 2. 达到截止时间后任务将被取消，任何的主动退出都使用取消
    ///
    /// PS：使用取消异常是为了避免捕获堆栈，Future只对取消异常进行了优化。
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public long Timeout {
        get => timeout;
        set {
            timeout = value;
            _core.SetEnable(TaskOptions.HAS_TIMEOUT, value > 0);
        }
    }

    /// <summary>
    /// 通过预估执行次数限制超时时间
    /// 该方法对于fixedRate类型的任务有帮助
    /// </summary>
    /// <param name="count"></param>
    public void SetTimeoutByCount(int count) {
        if (count < 1) {
            throw new ArgumentException("invalid count: " + count);
        }
        this.Timeout = count == 1
            ? Math.Max(0, initialDelay)
            : Math.Max(0, initialDelay) + (count - 1) * Period;
    }

    /// <summary>
    /// 是否包含执行次数限制
    /// </summary>
    public bool HasCountLimit => _core.IsEnabled(TaskOptions.HAS_COUNT_LIMIT);

    /// <summary>
    /// 设置任务的执行次数限制
    ///
    /// 注：
    /// 1.到达执行上限后任务将被取消，任何的主动退出都使用取消。
    /// 2.使用取消异常是为了避免捕获堆栈，Future只对取消异常进行了优化。
    /// </summary>
    public int CountLimit {
        get => _countLimit;
        set {
            _countLimit = value;
            _core.SetEnable(TaskOptions.HAS_COUNT_LIMIT, value > 0);
        }
    }

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => TaskOptions.GetPriority(_core.Options);
        set {
            _core.Options = TaskOptions.SetPriority(_core.Options, value);
            Enable(TaskOptions.HAS_PRIORITY);
        }
    }

    #endregion
}
}