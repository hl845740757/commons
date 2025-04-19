#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
public class DisruptorSchedulerHelper<T> : ISchedulerHelper where T : IAgentEvent
{
    private readonly IDisruptorEventLoop<T> _eventLoop;
    private readonly IndexedPriorityQueue<IScheduledFutureTask> _taskQueue;

    public DisruptorSchedulerHelper(IDisruptorEventLoop<T> eventLoop) {
        _eventLoop = eventLoop ?? throw new ArgumentNullException(nameof(eventLoop));
        _taskQueue = new IndexedPriorityQueue<IScheduledFutureTask>(new ScheduledTaskComparator(), 64);
    }

    #region core

    /// <summary>
    /// 处理周期性任务，传入的限制只有在遇见低优先级任务的时候才生效，因此限制为0则表示遇见低优先级任务立即结束
    /// (为避免时序错误，处理周期性任务期间不响应关闭，不容易安全实现)
    /// </summary>
    /// <param name="tickTime">当前时间</param>
    /// <param name="shuttingDownMode">是否是退出模式</param>
    public void Update(long tickTime, bool shuttingDownMode) {
        IndexedPriorityQueue<IScheduledFutureTask> taskQueue = this._taskQueue;
        IDisruptorEventLoop<T> eventLoop = this._eventLoop;

        IScheduledFutureTask futureTask;
        while (taskQueue.TryPeekHead(out futureTask) && !eventLoop.IsShutdown) {
            if (tickTime < futureTask.NextTriggerTime) {
                return;
            }
            taskQueue.Dequeue();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (futureTask.IsTriggered || futureTask.Trigger(tickTime)) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                }
            } else if (futureTask.Trigger(tickTime)) {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 和下面相同
                if (eventLoop.IsShuttingDown) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.Enqueue(futureTask);
                }
            }
        }
    }

    public void DoSchedule(IScheduledFutureTask futureTask) {
        Debug.Assert(_eventLoop.InEventLoop() && futureTask.Id >= 0);
        long tickTime = _eventLoop.TickTime;
        if (tickTime < futureTask.NextTriggerTime) {
            _taskQueue.Enqueue(futureTask);
            return;
        }
        // 和上面Update逻辑相同
        if (futureTask.Trigger(tickTime)) {
            if (_eventLoop.IsShuttingDown) {
                futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
            } else {
                _taskQueue.Enqueue(futureTask);
            }
        }
    }

    public void OnCancelRequested(ICancelToken cancelToken, object ctx) {
        int cancelCode = cancelToken.CancelCode;
        long taskId = (long)ctx;
        if (_eventLoop.InEventLoop()) {
            // 如果不在调度队列，应当正在执行Trigger方法，在执行完用户回调后会检测到取消信号
            IScheduledFutureTask task = RemoveTask(taskId);
            if (task != null) {
                task.Cancel(cancelCode);
            }
        } else {
            // 如果在其它线程，尝试发布一个删除任务（能收到取消信号，通常证明Task还未结束）
            long sequence = _eventLoop.TryNextSequence(1);
            if (sequence < 0) {
                return; // TODO 可通过GlobalEventLoop不断重试
            }
            ref T evt = ref _eventLoop.GetEventRef(sequence);
            evt.Type = DisruptorEventLoop<T>.TYPE_REMOVE_SCHEDULE;
            evt.LongVal1 = taskId;
            _eventLoop.Publish(sequence);
        }
    }

    /// <summary>
    /// 删除指定id的任务
    /// </summary>
    /// <param name="taskId"></param>
    public IScheduledFutureTask? RemoveTask(long taskId) {
        // 暂时迭代处理
        foreach (IScheduledFutureTask task in _taskQueue) {
            if (task.Id == taskId) {
                _taskQueue.Remove(task);
                return task;
            }
        }
        return null;
    }

    /// <summary>
    /// 清理任务队列
    /// </summary>
    public void ClearTaskQueue() {
        // 需要归还到池
        IScheduledFutureTask futureTask;
        while (_taskQueue.TryDequeue(out futureTask)) {
            futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
        }
    }

    #endregion

    #region simple

    public long TickTime => _eventLoop.TickTime;

    public bool IsShutdown => _eventLoop.IsShutdown;

    public bool InEventLoop() => _eventLoop.InEventLoop();

    public long Normalize(long worldTime, TimeSpan timeUnit) {
        return worldTime * timeUnit.Ticks;
    }

    public long Denormalize(long localTime, TimeSpan timeUnit) {
        return localTime / timeUnit.Ticks;
    }

    #endregion
}
}