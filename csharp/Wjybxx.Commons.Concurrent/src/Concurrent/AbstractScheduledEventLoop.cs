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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

public abstract class AbstractScheduledEventLoop : AbstractEventLoop
{
    protected AbstractScheduledEventLoop(IEventLoopGroup? parent) : base(parent) {
    }

    public override IScheduledFuture<TResult> Schedule<TResult>(ref ScheduledTaskBuilder<TResult> builder) {
        ScheduledPromiseTask<TResult> promiseTask = ScheduledPromiseTask.OfBuilder(ref builder, NewScheduledPromise<TResult>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        long triggerTime = ScheduledPromiseTask.TriggerTime(delay, TickTime);
        MiniContext context = MiniContext.OfCancelToken(cancelToken);

        ScheduledPromiseTask<object> promiseTask = ScheduledPromiseTask.OfAction(action, context, 0, NewScheduledPromise<object>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture ScheduleAction(Action<IContext> action, TimeSpan delay, IContext context) {
        long triggerTime = ScheduledPromiseTask.TriggerTime(delay, TickTime);

        ScheduledPromiseTask<object> promiseTask = ScheduledPromiseTask.OfAction(action, context, 0, NewScheduledPromise<object>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        long triggerTime = ScheduledPromiseTask.TriggerTime(delay, TickTime);
        MiniContext context = MiniContext.OfCancelToken(cancelToken);

        ScheduledPromiseTask<TResult> promiseTask = ScheduledPromiseTask.OfFunction(action, context, 0, NewScheduledPromise<TResult>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<IContext, TResult> action, TimeSpan delay, IContext context) {
        long triggerTime = ScheduledPromiseTask.TriggerTime(delay, TickTime);

        ScheduledPromiseTask<TResult> promiseTask = ScheduledPromiseTask.OfFunction(action, context, 0, NewScheduledPromise<TResult>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        MiniContext context = MiniContext.OfCancelToken(cancelToken);
        ScheduledTaskBuilder<object> builder = ScheduledTaskBuilder.NewAction(action);
        builder.SetFixedDelay(delay.Ticks, period.Ticks, new TimeSpan(1));
        builder.Context = context;

        ScheduledPromiseTask<object> promiseTask = ScheduledPromiseTask.OfBuilder(ref builder, NewScheduledPromise<object>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IScheduledFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        MiniContext context = MiniContext.OfCancelToken(cancelToken);
        ScheduledTaskBuilder<object> builder = ScheduledTaskBuilder.NewAction(action);
        builder.SetFixedRate(delay.Ticks, period.Ticks, new TimeSpan(1));
        builder.Context = context;

        ScheduledPromiseTask<object> promiseTask = ScheduledPromiseTask.OfBuilder(ref builder, NewScheduledPromise<object>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    /// <summary>
    /// 当前事件循环的时间tick数
    /// </summary>
    protected internal abstract long TickTime { get; }

    /// <summary>
    /// 请求将当前任务重新压入队列
    /// 1.一定从当前线程调用
    /// 2.如果无法继续调度任务，则取消任务
    /// </summary>
    /// <param name="scheduledTask"></param>
    /// <param name="triggered">是否是执行之后再次压入队列</param>
    protected internal abstract void ReSchedulePeriodic(IScheduledFutureTask scheduledTask, bool triggered);

    /// <summary>
    /// 请求删除给定的任务
    /// 1.可能从其它线程调用，需考虑线程安全问题
    /// </summary>
    /// <param name="scheduledTask"></param>
    protected internal abstract void RemoveScheduled(IScheduledFutureTask scheduledTask);
}