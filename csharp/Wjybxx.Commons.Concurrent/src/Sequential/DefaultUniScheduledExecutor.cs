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
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Time;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 时序管理同<see cref="DisruptorEventLoop{T}"/>：
/// 
/// </summary>
public class DefaultUniScheduledExecutor : AbstractUniScheduledExecutor
{
    private readonly ITimeProvider timeProvider;
    private readonly Queue<object> taskQueue;
    private readonly IndexedPriorityQueue<IScheduledFutureTask> scheduledTaskQueue = new(new ScheduledTaskComparator());
    private readonly ScheduledHelper scheduledHelper;
    private readonly UniPromise<int> terminationPromise;

    private EventLoopState state = EventLoopState.Unstarted;
    /** 为任务分配唯一id，确保先入先出 */
    private long sequencer = 0;
    /** 当前帧的时间戳，缓存下来以避免在tick的过程中产生变化 */
    private long tickTime;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeProvider">计时器</param>
    /// <param name="initCapacity">初始队列大</param>
    public DefaultUniScheduledExecutor(ITimeProvider timeProvider, int initCapacity = 16) {
        this.timeProvider = timeProvider;
        this.taskQueue = new Queue<object>(initCapacity);
        this.terminationPromise = new UniPromise<int>(this);
        this.scheduledHelper = new ScheduledHelper(this);
        this.tickTime = timeProvider.Current;
    }

    public override void Update() {
        // 需要缓存下来，一来用于计算下次调度时间，二来避免优先级错乱
        tickTime = timeProvider.Current;
        ProcessScheduledQueue(tickTime, IsShuttingDown);

        Queue<object> taskQueue = this.taskQueue;
        if (taskQueue.Count == 0) {
            return;
        }

        object task;
        while (taskQueue.TryDequeue(out task)) {
            try {
                if (task is Action action) {
                    action();
                } else {
                    ITask castTask = (ITask)task;
                    castTask.Run();
                }
            }
            catch (Exception ex) {
                LogCause(ex);
            }
        }

        // 为何要再执行一次？任务队列中可能包含定时任务，我们需要进行补帧
        ProcessScheduledQueue(tickTime, IsShuttingDown);
    }

    private void ProcessScheduledQueue(long tickTime, bool shuttingDownMode) {
        IndexedPriorityQueue<IScheduledFutureTask> taskQueue = scheduledTaskQueue;
        IScheduledFutureTask queueTask;
        while (taskQueue.TryPeekHead(out queueTask)) {
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (tickTime < queueTask.NextTriggerTime) {
                return;
            }

            taskQueue.Dequeue();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (queueTask.IsTriggered || queueTask.Trigger(tickTime)) {
                    queueTask.TrySetCancelled();
                    scheduledHelper.OnCompleted(queueTask);
                }
            } else {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 需等同Reschedule
                if (queueTask.Trigger(tickTime)) {
                    if (IsShuttingDown) {
                        queueTask.TrySetCancelled();
                        scheduledHelper.OnCompleted(queueTask);
                    } else {
                        taskQueue.Enqueue(queueTask);
                        continue;
                    }
                } else {
                    scheduledHelper.OnCompleted(queueTask);
                }
            }
        }
    }

    public override bool NeedMoreUpdate() {
        if (scheduledTaskQueue.TryPeekHead(out IScheduledFutureTask queueTask)) {
            return queueTask.NextTriggerTime <= tickTime;
        }
        return false;
    }

    public override void Execute(ITask task) {
        if (IsShuttingDown) {
            // 暂时直接取消
            if (task is IFutureTask futureTask) {
                futureTask.TrySetCancelled();
            }
            return;
        }
        taskQueue.Enqueue(task);
        if (task is IScheduledFutureTask scheduledFutureTask) {
            scheduledFutureTask.Id = ++sequencer;
            scheduledFutureTask.RegisterCancellation();
        }
    }

    public override void Execute(Action action, int options = 0) {
        // 该executor不支持options
        if (IsShutdown) {
            // 暂时直接取消
            return;
        }
        taskQueue.Enqueue(action);
    }

    #region lifecycle

    public override void Shutdown() {
        if (state < EventLoopState.ShuttingDown) {
            state = EventLoopState.ShuttingDown;
        }
    }

    public override List<ITask> ShutdownNow() {
        List<ITask> result = new List<ITask>(scheduledTaskQueue);
        scheduledTaskQueue.ClearIgnoringIndexes();
        state = EventLoopState.Terminated;
        terminationPromise.TrySetResult(0);
        return result;
    }

    public override IFuture TerminationFuture => terminationPromise.AsReadonly();
    public override bool IsShuttingDown => state >= EventLoopState.ShuttingDown;
    public override bool IsShutdown => state >= EventLoopState.Shutdown;
    public override bool IsTerminated => state == EventLoopState.Terminated;

    #endregion

    #region internal

    protected override IScheduledHelper Helper => scheduledHelper;

    private class ScheduledHelper : IScheduledHelper
    {
        private DefaultUniScheduledExecutor _executor;

        public ScheduledHelper(DefaultUniScheduledExecutor executor) {
            _executor = executor;
        }

        public long TickTime => _executor.tickTime;

        public long Normalize(long worldTime, TimeSpan timeUnit) {
            long ticks = worldTime * timeUnit.Ticks;
            return ticks / TimeSpan.TicksPerMillisecond; // 默认转毫秒单位
        }

        public long Denormalize(long localTime, TimeSpan timeUnit) {
            long ticks = localTime * TimeSpan.TicksPerMillisecond;
            return ticks / timeUnit.Ticks;
        }

        public void Reschedule(IScheduledFutureTask futureTask) {
            if (_executor.IsShuttingDown) {
                futureTask.TrySetCancelled();
                OnCompleted(futureTask);
            } else {
                _executor.scheduledTaskQueue.Enqueue(futureTask);
            }
        }

        public void OnCompleted(IScheduledFutureTask futureTask) {
            futureTask.Clear();
        }

        public void OnCancelRequested(IScheduledFutureTask futureTask, int cancelCode) {
            _executor.scheduledTaskQueue.Remove(futureTask);
        }
    }

    #endregion
}
}