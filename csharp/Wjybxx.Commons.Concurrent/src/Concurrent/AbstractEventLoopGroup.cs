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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
public abstract class AbstractEventLoopGroup : IEventLoopGroup
{
    private readonly SynchronizationContext _syncContext;
    private readonly ExecutorTaskScheduler _scheduler;

    protected AbstractEventLoopGroup() {
        _syncContext = new ExecutorSynchronizationContext(this);
        _scheduler = new ExecutorTaskScheduler(this);
    }

    public SynchronizationContext AsSyncContext() => _syncContext;

    public TaskScheduler AsScheduler() => _scheduler;

    public abstract IEventLoop Select();

    #region 生命周期

    public abstract void Shutdown();

    public abstract List<ITask> ShutdownNow();

    public abstract bool IsShuttingDown { get; }

    public abstract bool IsShutdown { get; }

    public abstract bool IsTerminated { get; }

    public abstract IFuture TerminationFuture { get; }

    #endregion

    #region submit

    public virtual void Execute(ITask task) {
        Select().Execute(task);
    }

    public virtual IPromise<T> NewPromise<T>() => new Promise<T>(this);

    public virtual IPromise NewPromise() => new Promise<int>(this);

    public virtual IFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        return Select().Submit(in builder);
    }

    public virtual IFuture SubmitAction(Action action, int options = 0) {
        return Select().SubmitAction(action, options);
    }

    public virtual IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0) {
        return Select().SubmitAction(action, context, options);
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        return Select().SubmitFunc(action, options);
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0) {
        return Select().SubmitFunc(action, context, options);
    }

    #endregion

    #region schedule

    public virtual IScheduledPromise<T> NewScheduledPromise<T>() => new ScheduledPromise<T>(this);

    public virtual IScheduledPromise NewScheduledPromise() => new ScheduledPromise<object>(this);

    public virtual IScheduledFuture<TResult> Schedule<TResult>(in ScheduledTaskBuilder<TResult> builder) {
        return Select().Schedule(in builder);
    }

    public virtual IScheduledFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        return Select().ScheduleAction(action, delay, cancelToken);
    }

    public virtual IScheduledFuture ScheduleAction(Action<IContext> action, TimeSpan delay, IContext context) {
        return Select().ScheduleAction(action, delay, context);
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        return Select().ScheduleFunc(action, delay, cancelToken);
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<IContext, TResult> action, TimeSpan delay, IContext context) {
        return Select().ScheduleFunc(action, delay, context);
    }

    public virtual IScheduledFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        return Select().ScheduleWithFixedDelay(action, delay, period, cancelToken);
    }

    public virtual IScheduledFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        return Select().ScheduleAtFixedRate(action, delay, period, cancelToken);
    }

    #endregion

    #region 迭代

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public abstract IEnumerator<IEventLoop> GetEnumerator();

    #endregion
}
}