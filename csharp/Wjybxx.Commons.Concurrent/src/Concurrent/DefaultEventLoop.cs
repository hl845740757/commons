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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Serilog;
using Wjybxx.Commons.Collections;

#pragma warning disable CS0169

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 默认的事件循环实现
/// </summary>
public class DefaultEventLoop : AbstractScheduledEventLoop
{
    private static readonly ILogger Logger = Log.Logger;

    /** 初始状态，未启动状态 */
    private const int ST_NOT_STARTED = 0;
    /** 启动中 */
    private const int ST_STARTING = 1;
    /** 运行状态 */
    private const int ST_RUNNING = 2;
    /** 正在关闭状态 */
    private const int ST_SHUTTING_DOWN = 3;
    /** 已关闭状态，正在进行最后的清理 */
    private const int ST_SHUTDOWN = 4;
    /** 终止状态 */
    private const int ST_TERMINATED = 5;

    private const int MIN_BATCH_SIZE = 64;
    private const int MAX_BATCH_SIZE = 65535;

    private long p1, p2, p3, p4, p5, p6, p7, p8;
    /** 当前帧时间戳 -- 变化频繁，访问频率高，缓存行填充 */
    private long _tickTime;
    private long p11, p12, p13, p14, p15, p16, p17, p18;

    /** 事件循环的状态 -- 变化频率不高，不缓存行填充 */
    private volatile int _state;
    /** 普通任务队列 */
    private readonly ConcurrentQueue<ITask> _taskQueue = new();
    /** 定时任务队列 */
    private readonly IndexedPriorityQueue<IScheduledFutureTask> _scheduledTaskQueue = new(new ScheduledTaskComparator());
    /** 定时任务id分配器 -- 线程本地变量，任务出队时分配，以保证先入队的任务id越小 */
    private long _idSequencer;

    /** 绑定的线程 */
    private readonly Thread _thread;
    /** 进入运行状态的promise */
    private readonly IPromise<int> _runningPromise;
    /** 进入终止状态的promise */
    private readonly IPromise<int> _terminationPromise;
    /** 只读future - 缓存字段 */
    private readonly IFuture _runningFuture;
    private readonly IFuture _terminationFuture;

    /** 等待任务时的自旋次数 */
    private readonly int _waitTaskSpinTries;
    /** 最多连续处理多少个事件（任务）就需要执行一次update */
    private readonly int _taskBatchSize;
    /** 任务的拒绝策略 */
    private readonly RejectedExecutionHandler _rejectedExecutionHandler;
    /** 事件循环的内部代理 */
    private readonly IEventLoopAgent<IAgentEvent> _agent;
    /** 事件循环的主模块 */
    private readonly IEventLoopModule? _mainModule;

    public DefaultEventLoop(EventLoopBuilder builder) : base(builder.Parent) {
        _state = ST_NOT_STARTED;
        _tickTime = ObjectUtil.SystemTicks();

        ThreadFactory threadFactory = ObjectUtil.RequireNonNull(builder.ThreadFactory, "ThreadFactory");
        _thread = threadFactory.NewThread(MainLoopEntry);
        _runningPromise = new Promise<int>(this);
        _terminationPromise = new Promise<int>(this);
        _runningFuture = _runningPromise.AsReadonly();
        _terminationFuture = _terminationPromise.AsReadonly();

        _waitTaskSpinTries = Math.Max(0, builder.WaitTaskSpinTries);
        _taskBatchSize = Math.Clamp(builder.BatchSize, MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        _rejectedExecutionHandler = builder.RejectedExecutionHandler ?? RejectedExecutionHandlers.ABORT;
        _agent = builder.Agent ?? EmptyAgent<IAgentEvent>.INST;
        _mainModule = builder.MainModule;

        _agent.Inject(this); // 注入自己
    }

    public override IEventLoopModule? MainModule => _mainModule;

    protected internal override long TickTime => Volatile.Read(ref _tickTime);

    // C#的另一个坑，override的时候不能增加set...
    private void SetTickTime(long tickTime) {
        Volatile.Write(ref _tickTime, tickTime);
    }

    #region 状态查询

    public override IFuture RunningFuture => _runningFuture;
    public override IFuture TerminationFuture => _terminationFuture;

    public override EventLoopState State => (EventLoopState)_state;
    public override bool IsRunning => _state == ST_RUNNING;
    public override bool IsShuttingDown => _state >= ST_SHUTTING_DOWN;
    public override bool IsShutdown => _state >= ST_SHUTDOWN;
    public override bool IsTerminated => _state == ST_TERMINATED;

    public override bool InEventLoop() {
        return Thread.CurrentThread == _thread;
    }

    public override bool InEventLoop(Thread thread) {
        return thread == _thread;
    }

    public override void Wakeup() {
        if (!InEventLoop() && _thread.IsAlive) {
            _thread.Interrupt();
            _agent.Wakeup();
        }
    }

    #endregion

    #region Execute

    public override void Execute(ITask task) {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (IsShuttingDown) {
            _rejectedExecutionHandler.Rejected(task, this);
            return;
        }
        _taskQueue.Enqueue(task);
        if (IsShuttingDown) {
            // C#端先不删除任务，这在EventLoop关闭时可能造成一定的内存泄漏，但对我们目前的工作影响较小，
            // 而要解决这个问题的成本较高，要么对Task进行二次封装（运行成本高），要么需要实现一个并发队列（开发成本高）
        } else if (_state == ST_NOT_STARTED) {
            EnsureThreadStarted();
        }
    }

    protected internal override void ReschedulePeriodic(IScheduledFutureTask scheduledTask, bool triggered) {
        Debug.Assert(InEventLoop());
        if (IsShuttingDown) {
            scheduledTask.CancelWithoutRemove();
            return;
        }
        _scheduledTaskQueue.Enqueue(scheduledTask);
    }

    protected internal override void RemoveScheduled(IScheduledFutureTask scheduledTask) {
        if (IsShuttingDown) {
            _scheduledTaskQueue.Remove(scheduledTask);
        } else {
            Execute(scheduledTask); // run方法会检测取消信号，避免额外封装；出队列时要判断一下id
        }
    }

    #endregion

    #region 线程主循环

    public override IFuture Start() {
        EnsureThreadStarted();
        return RunningFuture;
    }

    public override void Shutdown() {
        if (!_runningPromise.IsDone) { // 尚未启动成功就关闭
            _runningPromise.TrySetCancelled(CancelCodes.REASON_SHUTDOWN);
        }
        int expectedState = _state;
        for (;;) {
            // check
            if (expectedState >= ST_SHUTTING_DOWN) {
                return;
            }
            // cas
            int realState = Interlocked.CompareExchange(ref _state, ST_SHUTTING_DOWN, expectedState);
            if (realState == expectedState) {
                EnsureThreadTerminable(expectedState);
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    public override List<ITask> ShutdownNow() {
        Shutdown();
        AdvanceRunState(ST_SHUTDOWN);
        return new List<ITask>(0); // EventLoop的任务队列是线程私有的，外部不可访问
    }

    private void EnsureThreadStarted() {
        if (_state == ST_NOT_STARTED
            && Interlocked.CompareExchange(ref _state, ST_STARTING, ST_NOT_STARTED) == ST_NOT_STARTED) {
            _thread.UnsafeStart(); // 不捕获奇怪的ExecutionContext，我非常讨厌C#这个隐式上下文捕获
        }
    }

    /// <summary>
    /// 确保线程可关闭
    /// </summary>
    /// <param name="oldState">切换为关闭状态之前的状态</param>
    private void EnsureThreadTerminable(int oldState) {
        if (oldState == ST_NOT_STARTED) {
            _state = ST_TERMINATED;

            _runningPromise.TrySetCancelled(CancelCodes.REASON_SHUTDOWN);
            _terminationPromise.TrySetResult(0);
        } else {
            // 在C#的实现中，暂未实现复杂的生产者消费者协调策略，因此此处暂只需要唤醒线程
            // 唤醒线程
            Wakeup();
        }
    }

    /// <summary>
    /// 如果事件循环尚未到达指定状态，则更新为指定状态
    /// </summary>
    /// <param name="targetState">要到达的状态</param>
    private void AdvanceRunState(int targetState) {
        int expectedState = _state;
        for (;;) {
            // check
            if (expectedState >= targetState) {
                return;
            }
            // cas
            int realState = Interlocked.CompareExchange(ref _state, targetState, expectedState);
            if (realState >= targetState) {
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    /// <summary>
    /// 线程主循环入口
    /// </summary>
    private void MainLoopEntry() {
        // 设置同步上下文，使得EventLoop创建的Task的下游任务默认继续在EventLoop上执行 -- 可增加是否启用选项
        SynchronizationContext.SetSynchronizationContext(AsSyncContext());
        try {
            if (_runningPromise.IsDone) {
                goto loopEnd; // 退出
            }
            SetTickTime(ObjectUtil.SystemTicks());
            _agent.OnStart();

            AdvanceRunState(ST_RUNNING);
            _runningPromise.TrySetResult(0);

            if (_runningPromise.IsSucceeded) {
                MainLoopCore();
            }

            loopEnd:
            {
            }
        }
        catch (Exception e) {
            Logger.Error(e, "eventLoop exit due to exception");
            if (!_runningPromise.IsSucceeded) {
                AdvanceRunState(ST_SHUTDOWN); // 启动失败直接进入快速退出状态，丢弃所有提交的任务
            }
        }
        finally {
            if (_runningPromise.IsSucceeded) {
                AdvanceRunState(ST_SHUTTING_DOWN);
            } else {
                AdvanceRunState(ST_SHUTDOWN);
            }

            CleanTaskQueue();
            ThreadUtil.ClearInterrupt();

            try {
                _agent.OnShutdown();
            }
            catch (Exception e) {
                Logger.Warning(e, "agent.OnShutdown caught exception");
            }

            // 进入终止状态 -- 需清理同步上下文
            SynchronizationContext.SetSynchronizationContext(null);
            _state = ST_TERMINATED;
            _terminationPromise.TrySetResult(0);
        }
    }

    /// <summary>
    /// 线程主循环
    /// </summary>
    private void MainLoopCore() {
        ConcurrentQueue<ITask> taskQueue = _taskQueue;
        IEventLoopAgent<IAgentEvent> agent = _agent;
        int batchSize = _taskBatchSize;

        int count = 0;
        ITask task;
        while (!IsShuttingDown) {
            if (!WaitTask(taskQueue, out task)) {
                if (!IsShuttingDown) {
                    count = 0;
                    SetTickTime(ObjectUtil.SystemTicks());
                    ProcessScheduledQueue(false);
                    InvokeAgentUpdate();
                }
                continue;
            }
            // 在出队列时分配id，可保证先进入队列的任务的id越小，保证id的有序性 -- 否则我们需要实现自己的并发队列
            if (task is IScheduledFutureTask scheduledTask && scheduledTask.Id == 0) {
                scheduledTask.Id = _idSequencer++;
                scheduledTask.RegisterCancellation();
            }

            // 更新时间戳，更新时间后必须先执行定时任务队列
            if (count++ == 0) {
                SetTickTime(ObjectUtil.SystemTicks());
                ProcessScheduledQueue(false);
            }

            // 执行出队的任务
            try {
                if (task is IAgentEvent agentEvent) {
                    agent.OnEvent(agentEvent);
                } else {
                    task.Run();
                }
            }
            catch (Exception e) {
                if (e is ThreadInterruptedException && IsShuttingDown) {
                    break; // 响应关闭
                }
                Logger.Information(e, "execute task caught exception");
            }

            // 检测是否应该执行用户Update方法
            if (count >= batchSize && !IsShuttingDown) {
                count = 0;
                InvokeAgentUpdate();
            }
        }
    }

    private bool WaitTask(ConcurrentQueue<ITask> taskQueue, out ITask task) {
        // Dequeue的性能不是很好，不能频繁调用
        int waitCounter = _waitTaskSpinTries;
        while (!taskQueue.TryDequeue(out task)) {
            waitCounter--;
            if (waitCounter > 0) {
                Thread.Yield(); // yield
                continue;
            }
            // sleep的最小单位是毫秒，稍微有点大；可能被中断唤醒，以处理任务或关闭
            try {
                Thread.Sleep(1);
            }
            catch (ThreadInterruptedException) {
            }
            // 醒来之后再尝试一次
            return taskQueue.TryDequeue(out task);
        }
        return true;
    }

    private void InvokeAgentUpdate() {
        try {
            _agent.Update();
        }
        catch (Exception e) {
            Logger.Information(e, "agent.Update caught exception");
        }
    }

    private void ProcessScheduledQueue(bool shuttingDown) {
        long tickTime = _tickTime; // 线程内无需volatile读
        var taskQueue = _scheduledTaskQueue;

        IScheduledFutureTask queueTask;
        while (taskQueue.TryPeekHead(out queueTask)) {
            if (queueTask.Future.IsDone) {
                taskQueue.Dequeue(); // 未及时删除的任务
                continue;
            }
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (tickTime < queueTask.NextTriggerTime) {
                return;
            }
            // 响应关闭
            if (IsShutdown) {
                return;
            }
            taskQueue.Dequeue();
            if (shuttingDown) {
                // 关闭模式下，不再重复执行任务
                if (queueTask.IsTriggered || queueTask.Trigger(tickTime)) {
                    queueTask.CancelWithoutRemove();
                }
            } else {
                // 非关闭模式下，任务必须执行，否则可能导致时序错误
                if (queueTask.Trigger(tickTime)) {
                    taskQueue.Enqueue(queueTask);
                }
            }
        }
    }

    /// <summary>
    /// 清理任务队列
    /// </summary>
    private void CleanTaskQueue() {
        long startTime = ObjectUtil.SystemTicks();
        SetTickTime(startTime);
        ProcessScheduledQueue(true);

        ConcurrentQueue<ITask> taskQueue = _taskQueue;
        IEventLoopAgent<IAgentEvent> agent = _agent;

        long taskCount = 0;
        long discardCount = 0;
        ITask task;
        while (taskQueue.TryDequeue(out task)) {
            taskCount++;
            if (IsShutdown) { // 如果已进入shutdown阶段，则直接丢弃任务
                discardCount++;
                continue;
            }

            // 执行出队的任务
            try {
                if (task is IAgentEvent agentEvent) {
                    agent.OnEvent(agentEvent);
                } else {
                    task.Run();
                }
            }
            catch (Exception e) {
                Logger.Information(e, "execute task caught exception");
            }
        }
        long costTime = TimeSpan.FromTicks(ObjectUtil.SystemTicks() - startTime).Milliseconds;
        Logger.Information($"CleanTaskQueue succeeded, taskCount: {taskCount}, discardCount: {discardCount}, costTime: {costTime}ms");
    }

    #endregion
}