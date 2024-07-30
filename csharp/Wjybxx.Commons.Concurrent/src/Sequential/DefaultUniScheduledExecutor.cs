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

using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Time;

namespace Wjybxx.Commons.Sequential
{
public class DefaultUniScheduledExecutor : AbstractUniScheduledExecutor
{
    private readonly ITimeProvider timeProvider;
    private readonly IndexedPriorityQueue<IScheduledFutureTask> taskQueue = new(new ScheduledTaskComparator());
    private readonly UniPromise<int> terminationPromise;
    private readonly IFuture terminationFuture;
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
        this.terminationPromise = new UniPromise<int>(this);
        this.terminationFuture = terminationPromise.AsReadonly();
        this.tickTime = timeProvider.Current;
    }

    public override void Update() {
        // 需要缓存下来，一来用于计算下次调度时间，二来避免优先级错乱
        long curTime = timeProvider.Current;
        tickTime = curTime;

        // 记录最后一个任务id，避免执行本次tick期间添加的任务
        long barrierTaskId = sequencer;
        IndexedPriorityQueue<IScheduledFutureTask> taskQueue = this.taskQueue;
        IScheduledFutureTask queueTask;
        while (taskQueue.TryPeekHead(out queueTask)) {
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (curTime < queueTask.NextTriggerTime) {
                return;
            }
            // 本次tick期间新增的任务，不立即执行，避免死循环或占用过多cpu
            if (queueTask.Id > barrierTaskId) {
                return;
            }

            taskQueue.Dequeue();
            if (queueTask.Trigger(tickTime)) {
                if (IsShuttingDown) { // 已请求关闭
                    queueTask.CancelWithoutRemove();
                } else {
                    taskQueue.Enqueue(queueTask);
                }
            }
        }
    }

    public override bool NeedMoreUpdate() {
        if (taskQueue.TryPeekHead(out IScheduledFutureTask queueTask)) {
            return queueTask.NextTriggerTime <= tickTime;
        }
        return false;
    }

    public override void Execute(ITask task) {
        if (IsShuttingDown) {
            // 暂时直接取消
            if (task is IFutureTask futureTask) {
                futureTask.Future.SetCancelled(CancelCodes.REASON_SHUTDOWN);
            }
            return;
        }
        if (task is IScheduledFutureTask scheduledFutureTask) {
            scheduledFutureTask.Id = ++sequencer;
            if (DelayExecute(scheduledFutureTask)) {
                scheduledFutureTask.RegisterCancellation();
            }
        } else {
            var promiseTask = UniScheduledPromiseTask.OfTask(task, null, task.Options, NewScheduledPromise<int>(), ++sequencer, tickTime);
            if (DelayExecute(promiseTask)) {
                promiseTask.RegisterCancellation();
            }
        }
    }

    private bool DelayExecute(IScheduledFutureTask futureTask) {
        if (IsShuttingDown) {
            // 默认直接取消，暂不添加拒绝处理器
            futureTask.CancelWithoutRemove();
            return false;
        } else {
            taskQueue.Enqueue(futureTask);
            return true;
        }
    }

    #region lifecycle

    public override void Shutdown() {
        if (state < EventLoopState.ShuttingDown) {
            state = EventLoopState.ShuttingDown;
        }
    }

    public override List<ITask> ShutdownNow() {
        List<ITask> result = new List<ITask>(taskQueue);
        taskQueue.ClearIgnoringIndexes();
        state = EventLoopState.Terminated;
        terminationPromise.TrySetResult(0);
        return result;
    }

    public override IFuture TerminationFuture => terminationFuture;
    public override bool IsShuttingDown => state >= EventLoopState.ShuttingDown;
    public override bool IsShutdown => state >= EventLoopState.Shutdown;
    public override bool IsTerminated => state == EventLoopState.Terminated;

    #endregion

    #region internal

    protected internal override long TickTime => tickTime;

    protected internal override void ReSchedulePeriodic(IScheduledFutureTask scheduledTask, bool triggered) {
        DelayExecute(scheduledTask);
    }

    protected internal override void RemoveScheduled(IScheduledFutureTask scheduledTask) {
        taskQueue.Remove(scheduledTask);
    }

    #endregion
}
}