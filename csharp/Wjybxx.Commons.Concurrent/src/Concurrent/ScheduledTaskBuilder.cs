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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 使用接口定义常量
/// </summary>
public interface ScheduledTaskBuilder : TaskBuilder
{
    /** 执行一次 */
    public const byte SCHEDULE_ONCE = 0;
    /** 固定延迟 -- 两次执行的间隔大于等于给定的延迟 */
    public const byte SCHEDULE_FIXED_DELAY = 1;
    /** 固定频率 -- 执行次数 */
    public const byte SCHEDULE_FIXED_RATE = 2;
    /** 动态延迟 -- 每次执行后计算下一次的延迟 */
    public const byte SCHEDULE_DYNAMIC_DELAY = 3;

    /** 适用于禁止初始延迟小于0的情况 */
    public static void ValidateInitialDelay(long initialDelay) {
        if (initialDelay < 0) {
            throw new ArgumentException($"initialDelay: {initialDelay} (expected: >= 0)");
        }
    }

    public static void ValidatePeriod(long period) {
        if (period == 0) {
            throw new ArgumentException("period: 0 (expected: != 0)");
        }
    }

    #region factory

    public new static ScheduledTaskBuilder<int> NewAction(Action task, ICancelToken? cancelToken = null) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewAction(task, cancelToken);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
    }

    public new static ScheduledTaskBuilder<int> NewAction(Action<IContext> task, IContext ctx) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewAction(task, ctx);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
    }

    public new static ScheduledTaskBuilder<T> NewFunc<T>(Func<T> task, ICancelToken? cancelToken = null) {
        TaskBuilder<T> taskBuilder = TaskBuilder.NewFunc(task, cancelToken);
        return new ScheduledTaskBuilder<T>(ref taskBuilder);
    }

    public new static ScheduledTaskBuilder<T> NewFunc<T>(Func<IContext, T> task, IContext ctx) {
        TaskBuilder<T> taskBuilder = TaskBuilder.NewFunc(task, ctx);
        return new ScheduledTaskBuilder<T>(ref taskBuilder);
    }

    public new static ScheduledTaskBuilder<T> NewTimeSharing<T>(TimeSharingTask<T> func, IContext? context = null) {
        TaskBuilder<T> taskBuilder = TaskBuilder.NewTimeSharing(func, context);
        return new ScheduledTaskBuilder<T>(ref taskBuilder);
    }

    public new static ScheduledTaskBuilder<int> NewTask(ITask task) {
        TaskBuilder<int> taskBuilder = TaskBuilder.NewTask(task);
        return new ScheduledTaskBuilder<int>(ref taskBuilder);
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
    /** 时间单位 -- 默认毫秒 */
    private TimeSpan timeunit;
    /** 执行次数限制 */
    private int countLimit;

    internal ScheduledTaskBuilder(ref TaskBuilder<T> core) {
        _core = core;
        scheduleType = 0;
        initialDelay = 0;
        period = 0;
        timeout = -1;
        timeunit = TimeSpan.FromMilliseconds(1);
        countLimit = -1;
    }

    #region 代理

    public int Type => _core.Type;

    public object Task => _core.Task;

    public IContext? Context {
        get => _core.Context;
        set => _core.Context = value;
    }

    public int Options {
        get => _core.Options;
        set => _core.Options = value;
    }

    /// <summary>
    /// 启用特定任务选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Enable(int taskOption) {
        _core.Enable(taskOption);
    }

    /// <summary>
    /// 关闭特定任务选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Disable(int taskOption) {
        _core.Disable(taskOption);
    }

    /// <summary>
    /// 设置任务的调度阶段
    /// </summary>
    public int SchedulePhase {
        get => _core.SchedulePhase;
        set => _core.SchedulePhase = value;
    }

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => _core.Priority;
        set => _core.Priority = value;
    }

    #endregion

    #region schedule

    public byte ScheduleType => scheduleType;

    public long InitialDelay => initialDelay;

    public long Period => period;

    public TimeSpan Timeunit {
        get => timeunit;
        set {
            if (value.Ticks <= 0) {
                throw new ArgumentException("invalid timeunit");
            }
            timeunit = value;
        }
    }

    /** 是否是周期性任务 */
    public bool IsPeriodic => scheduleType != 0;

    public bool IsOnlyOnce => scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE;

    /// <summary>
    /// 设置任务为单次执行
    /// </summary>
    /// <param name="delay">触发延迟</param>
    public void SetOnlyOnce(long delay) {
        this.scheduleType = ScheduledTaskBuilder.SCHEDULE_ONCE;
        this.initialDelay = delay;
        this.period = default;
    }

    /// <summary>
    /// 设置任务为单次执行
    /// </summary>
    /// <param name="delay">触发延迟</param>
    /// <param name="timeunit">时间单位</param>
    public void SetOnlyOnce(long delay, TimeSpan timeunit) {
        SetOnlyOnce(delay);
        Timeunit = timeunit;
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
        Timeunit = timeunit;
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
        Timeunit = timeunit;
    }

    /// <summary>
    /// 是否设置了超时时间
    /// </summary>
    public bool HasTimeout => timeout >= 0;

    /// <summary>
    /// 1. -1表示无限制，大于等于0表示有限制
    /// 2. 默认只在执行任务后检查是否超时，以确保至少会执行一次
    /// 3. 超时是一个不准确的调度，不保证超时后能立即结束
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public long Timeout {
        get => timeout;
        set {
            if (value < -1) {
                throw new ArgumentException("invalid timeout: " + timeout);
            }
            timeout = value;
        }
    }

    /// <summary>
    /// 通过预估执行次数限制超时时间
    /// 该方法对于fixedRate类型的任务有帮助
    /// </summary>
    /// <param name="count"></param>
    public void SetTimeoutByCount(int count) {
        if (count < 1) {
            throw new ArithmeticException("invalid count: " + count);
        }
        if (count == 1) {
            this.timeout = Math.Max(0, initialDelay);
        } else {
            this.timeout = Math.Max(0, initialDelay + (count - 1) * Period);
        }
    }

    /// <summary>
    /// 设置任务的执行次数限制
    /// 1. -1表示无限制，大于0表示有限制，0非法
    /// </summary>
    public int CountLimit {
        get => countLimit;
        set {
            if (value <= 0 && value != -1) {
                throw new ArgumentException("invalid countLimit: " + countLimit);
            }
            countLimit = value;
        }
    }

    #endregion
}
}