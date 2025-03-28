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
internal class ScheduledHelper<T> : ISchedulerHelper where T : IAgentEvent
{
    private readonly DisruptorEventLoop<T> _eventLoop;
    private readonly IndexedPriorityQueue<IScheduledFutureTask> _taskQueue;

    public ScheduledHelper(DisruptorEventLoop<T> eventLoop) {
        _eventLoop = eventLoop;
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
        DisruptorEventLoop<T> eventLoop = this._eventLoop;

        IScheduledFutureTask futureTask;
        while (taskQueue.TryPeekHead(out futureTask)) {
            if (tickTime < futureTask.NextTriggerTime) {
                return;
            }

            taskQueue.Dequeue();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (futureTask.IsTriggered || futureTask.Trigger(tickTime)) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                }
            } else {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 和下面相同
                if (futureTask.Trigger(tickTime)) {
                    if (eventLoop.IsShuttingDown) {
                        futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                    } else {
                        taskQueue.Enqueue(futureTask);
                        continue;
                    }
                }
            }
            // 响应关闭
            if (eventLoop.IsShutdown) {
                return;
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

    public void OnCancelRequested(IScheduledFutureTask futureTask, int cancelCode) {
        if (_eventLoop.InEventLoop()) {
            // 如果不再调度队列，两种情况：
            // 1.还在RingBuffer队列，出队列时会检测到promise被取消
            // 2.正在执行Trigger方法，在执行完用户回调后会检测到promise被取消
            int index = futureTask.CollectionIndex((_taskQueue));
            if (index >= 0) {
                _taskQueue.Remove(futureTask);
            }
            // 同线程时立即进入取消状态，避免时序错误
            futureTask.Cancel(cancelCode);
        } else {
            // 如果在其它线程，尝试发布一个删除任务，需要小心可见性问题
            long taskId = futureTask.Id;
            if (taskId < 0) {
                return;
            }
            long sequence = _eventLoop.NextSequence(1);
            if (sequence < 0) {
                return;
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
    public void RemoveTask(long taskId) {
        // 暂时迭代处理
        foreach (IScheduledFutureTask task in _taskQueue) {
            if (task.Id == taskId) {
                _taskQueue.Remove(task);
                return;
            }
        }
    }

    /// <summary>
    /// 清理任务队列
    /// </summary>
    public void ClearIgnoringIndexes() {
        _taskQueue.ClearIgnoringIndexes();
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