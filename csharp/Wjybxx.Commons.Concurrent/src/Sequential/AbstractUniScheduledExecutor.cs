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

    public IScheduledPromise<int> NewScheduledPromise() => new UniScheduledPromise<int>();

    protected abstract IScheduledHelper Helper { get; }

    #region schedule

    public virtual IScheduledFuture<TResult> Schedule<TResult>(in ScheduledTaskBuilder<TResult> builder) {
        IScheduledPromise<TResult> promise = NewScheduledPromise<TResult>();
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise, Helper));
        return promise;
    }

    public virtual IScheduledFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfAction(action, cancelToken, 0, promise, Helper, Helper.TriggerTime(1, delay)));
        return promise;
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        IScheduledPromise<TResult> promise = NewScheduledPromise<TResult>();
        Execute(ScheduledPromiseTask.OfFunction(action, cancelToken, 0, promise, Helper, Helper.TriggerTime(1, delay)));
        return promise;
    }

    public virtual IScheduledFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedDelay(delay.Ticks, period.Ticks, new TimeSpan(1));

        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise, Helper));
        return promise;
    }

    public virtual IScheduledFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedRate(delay.Ticks, period.Ticks, new TimeSpan(1));

        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise, Helper));
        return promise;
    }

    #endregion

    #region execute

    public override void Execute(Action action, int options = 0) {
        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfAction(action, null, options, promise, Helper, Helper.TickTime));
    }

    #endregion

    #region submit

    public override IFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        IScheduledPromise<T> promise = NewScheduledPromise<T>();
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise, Helper));
        return promise;
    }

    public override IFuture SubmitAction(Action action, int options = 0) {
        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfAction(action, null, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    public override IFuture SubmitAction(Action action, ICancelToken cancelToken, int options = 0) {
        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfAction(action, cancelToken, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    public override IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0) {
        IScheduledPromise<int> promise = NewScheduledPromise();
        Execute(ScheduledPromiseTask.OfAction(action, context, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    public override IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        IScheduledPromise<T> promise = NewScheduledPromise<T>();
        Execute(ScheduledPromiseTask.OfFunction(action, null, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    public override IFuture<T> SubmitFunc<T>(Func<T> action, ICancelToken cancelToken, int options = 0) {
        IScheduledPromise<T> promise = NewScheduledPromise<T>();
        Execute(ScheduledPromiseTask.OfFunction(action, cancelToken, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    public override IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0) {
        IScheduledPromise<T> promise = NewScheduledPromise<T>();
        Execute(ScheduledPromiseTask.OfFunction(action, context, options, promise, Helper, Helper.TickTime));
        return promise;
    }

    #endregion
}
}