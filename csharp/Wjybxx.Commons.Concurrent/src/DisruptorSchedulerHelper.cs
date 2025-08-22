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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于实现全局Id分配
///
/// 注：
/// 1.改用该id方案后，先进入到Disruptor队列的任务可能有更大的Id —— 先提交任务的线程，在这里不一定先手。
/// 2.改用该id方案后，可以自由创建定时任务 —— 不再需要竞争Disruptor队列。
/// </summary>
internal static class DisruptorSchedulerHelper
{
    private static long _nextId;

    internal static long NextId() {
        return Interlocked.Increment(ref _nextId);
    }
}

public class DisruptorSchedulerHelper<T> : ISchedulerHelper where T : IAgentEvent
{
    private readonly IDisruptorEventLoop<T> _eventLoop;
    private readonly IndexedPriorityQueue<IScheduledFutureTask> _taskQueue;
    private readonly Action<ICancelToken, object> _onCancelRequested;

    public DisruptorSchedulerHelper(IDisruptorEventLoop<T> eventLoop) {
        _eventLoop = eventLoop ?? throw new ArgumentNullException(nameof(eventLoop));
        _taskQueue = new IndexedPriorityQueue<IScheduledFutureTask>(new ScheduledTaskComparator(), 64);
        _onCancelRequested = OnCancelRequested;
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
        // 检测正常定时任务
        IScheduledFutureTask futureTask;
        while (taskQueue.TryPeekHead(out futureTask) && !eventLoop.IsShutdown) {
            if (tickTime < futureTask.TriggerTime) {
                break;
            }
            taskQueue.Dequeue();

            bool enqueued = false;
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (futureTask.IsTriggered || futureTask.Trigger(tickTime)) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                }
            } else if (futureTask.Trigger(tickTime)) {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务
                if (eventLoop.IsShuttingDown) {
                    futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.Enqueue(futureTask);
                    enqueued = true;
                }
            }
            if (!enqueued) {
                futureTask.Release();
            }
        }
    }

    public void DoSchedule(IScheduledFutureTask futureTask) {
        Debug.Assert(_eventLoop.InEventLoop() && futureTask.Id >= 0);
        // 检测取消和关闭，避免不必要的启动和停止
        ICancelToken cancelToken = futureTask.GetCancelToken();
        if (_eventLoop.IsShutdown || cancelToken.IsCancelRequested) {
            int cancelCode = _eventLoop.IsShutdown ? CancelCodes.REASON_SHUTDOWN : cancelToken.CancelCode;
            futureTask.Cancel(cancelCode);
            futureTask.Release();
            return;
        }
        if (cancelToken.CanBeCancelled && NeedListenCancelToken(futureTask)) {
            futureTask.CancelRegistration = cancelToken.ThenAccept(_onCancelRequested, new Canceller(futureTask, futureTask.Id));
        }

        long tickTime = _eventLoop.TickTime;
        if (tickTime < futureTask.TriggerTime) {
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

    private bool NeedListenCancelToken(IScheduledFutureTask futureTask) {
        // 执行间隔短的任务就不主动注册监听器，以避免不必要的开销，暂定5S
        return (futureTask.Options & TaskOptions.LISTEN_CANCEL_TOKEN) != 0
               || futureTask.TriggerTime - _eventLoop.TickTime > 5 * TimeSpan.TicksPerSecond
               || futureTask.Period > 5 * TimeSpan.TicksPerSecond;
    }

    private void OnCancelRequested(ICancelToken cancelToken, object ctx) {
        Canceller canceller = (Canceller)ctx;
        if (_eventLoop.InEventLoop()) {
            Cancel(canceller.futureTask, canceller.taskId, cancelToken.CancelCode);
        } else {
            // 如果在其它线程，尝试发布一个删除任务（能收到取消信号，通常证明Task还未结束）
            long sequence = _eventLoop.TryNextSequence(1);
            if (sequence < 0) {
                return; // TODO 可通过GlobalEventLoop不断重试
            }
            ref T evt = ref _eventLoop.GetEventRef(sequence);
            evt.Type = DisruptorEventLoop<T>.TYPE_REMOVE_SCHEDULE;
            evt.Obj1 = canceller.futureTask;
            evt.LongVal1 = canceller.taskId;
            evt.LongVal2 = cancelToken.CancelCode;
            _eventLoop.Publish(sequence);
        }
    }

    public void Cancel(IScheduledFutureTask futureTask, long taskId, int cancelCode) {
        if (futureTask.Id != taskId) {
            return;
        }
        // 如果不在调度队列，应当正在执行Trigger方法，在执行完用户回调后会检测到取消信号
        if (_taskQueue.Remove(futureTask)) {
            futureTask.Cancel(cancelCode);
            futureTask.Release();
        }
    }

    public ValueFuture Sleep(TimeSpan timeSpan, ICancelToken? cancelToken) {
        if (!_eventLoop.InEventLoop()) {
            throw new GuardedOperationException();
        }
        if (timeSpan.Ticks == 0) { // 强制跳过当前帧
            timeSpan = new TimeSpan(1);
        }
        ValuePromise<int> promise = ValuePromise<int>.Acquire(_eventLoop);
        ScheduledPromiseTask<int> task = ScheduledPromiseTask.OfEmpty(cancelToken, 0, promise);
        ISchedulerHelper helper = this;
        task.Helper = helper;
        task.Id = NextId();
        task.TriggerTime = helper.TriggerTime(1, timeSpan);
        DoSchedule(task);
        return promise.VoidFuture;
    }

    /// <summary>
    /// 清理任务队列
    /// </summary>
    public void ClearTaskQueue() {
        // 需要归还到池
        IScheduledFutureTask futureTask;
        while (_taskQueue.TryDequeue(out futureTask)) {
            futureTask.Cancel(CancelCodes.REASON_SHUTDOWN);
            futureTask.Release();
        }
    }

    #endregion

    #region simple

    public IEventLoop EventLoop => _eventLoop;

    public bool InEventLoop() => _eventLoop.InEventLoop();

    public long TickTime => _eventLoop.TickTime;

    public long Normalize(long worldTime, TimeSpan timeUnit) {
        return worldTime * timeUnit.Ticks;
    }

    public long Denormalize(long localTime, TimeSpan timeUnit) {
        return localTime / timeUnit.Ticks;
    }

    public long NextId() {
        return DisruptorSchedulerHelper.NextId();
    }

    #endregion

    private class Canceller
    {
        public readonly IScheduledFutureTask futureTask;
        public readonly long taskId; // 校验是否已回收，只能在EventLoop线程校验

        public Canceller(IScheduledFutureTask futureTask, long taskId) {
            this.futureTask = futureTask;
            this.taskId = taskId;
        }
    }
}
}