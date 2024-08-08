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
using System.Threading;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 
/// </summary>
public abstract class AbstractUniScheduledExecutor : AbstractUniExecutor, IUniScheduledExecutor
{
    public IScheduledPromise<T> NewScheduledPromise<T>() => new UniScheduledPromise<T>();

    public IScheduledPromise NewScheduledPromise() => new UniScheduledPromise<int>();

    #region schedule

    public virtual IScheduledFuture<TResult> Schedule<TResult>(in ScheduledTaskBuilder<TResult> builder) {
        UniScheduledPromiseTask<TResult> promiseTask = UniScheduledPromiseTask.OfBuilder(in builder, NewScheduledPromise<TResult>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        long triggerTime = UniScheduledPromiseTask.TriggerTime(delay, TickTime);
        MiniContext context = MiniContext.OfCancelToken(cancelToken);

        UniScheduledPromiseTask<int> promiseTask = UniScheduledPromiseTask.OfAction(action, context, 0, NewScheduledPromise<int>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture ScheduleAction(Action<IContext> action, TimeSpan delay, IContext context) {
        long triggerTime = UniScheduledPromiseTask.TriggerTime(delay, TickTime);

        UniScheduledPromiseTask<int> promiseTask = UniScheduledPromiseTask.OfAction(action, context, 0, NewScheduledPromise<int>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        long triggerTime = UniScheduledPromiseTask.TriggerTime(delay, TickTime);
        MiniContext context = MiniContext.OfCancelToken(cancelToken);

        UniScheduledPromiseTask<TResult> promiseTask =
            UniScheduledPromiseTask.OfFunction(action, context, 0, NewScheduledPromise<TResult>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<IContext, TResult> action, TimeSpan delay, IContext context) {
        long triggerTime = UniScheduledPromiseTask.TriggerTime(delay, TickTime);

        UniScheduledPromiseTask<TResult> promiseTask =
            UniScheduledPromiseTask.OfFunction(action, context, 0, NewScheduledPromise<TResult>(), 0, triggerTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        MiniContext context = MiniContext.OfCancelToken(cancelToken);
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action);
        builder.SetFixedDelay(delay.Ticks, period.Ticks, new TimeSpan(1));
        builder.Context = context;

        UniScheduledPromiseTask<int> promiseTask = UniScheduledPromiseTask.OfBuilder(in builder, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IScheduledFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        MiniContext context = MiniContext.OfCancelToken(cancelToken);
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action);
        builder.SetFixedRate(delay.Ticks, period.Ticks, new TimeSpan(1));
        builder.Context = context;

        UniScheduledPromiseTask<int> promiseTask = UniScheduledPromiseTask.OfBuilder(in builder, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    #endregion

    #region execute

    public override void Execute(Action action, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfAction(action, null, options, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
    }

    public override void Execute(Action<IContext> action, IContext context, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfAction(action, context, options, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
    }

    public override void Execute(Action action, CancellationToken cancelToken, int options = 0) {
        ITask task = Executors.BoxAction(action, cancelToken, options);
        var promiseTask = UniScheduledPromiseTask.OfTask(task, null, options, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
    }

    #endregion

    #region submit

    public override IFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        var promiseTask = UniScheduledPromiseTask.OfBuilder(in builder, NewScheduledPromise<T>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IFuture SubmitAction(Action action, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfAction(action, null, options, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfAction(action, context, options, NewScheduledPromise<int>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfFunction(action, null, options, NewScheduledPromise<T>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public override IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0) {
        var promiseTask = UniScheduledPromiseTask.OfFunction(action, context, options, NewScheduledPromise<T>(), 0, TickTime);
        Execute(promiseTask);
        return promiseTask.Future;
    }

    #endregion

    #region internal

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

    #endregion
}
}