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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

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

    public virtual IPromise<T> NewPromise<T>(IContext? ctx = null) => new Promise<T>(this, ctx);

    public virtual IPromise NewPromise(IContext? ctx = null) => new Promise<byte>(this, ctx);

    public virtual IFuture<T> Submit<T>(ref TaskBuilder<T> builder) {
        return Select().Submit(ref builder);
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

    public virtual IScheduledPromise<T> NewScheduledPromise<T>(IContext? context = null) => new ScheduledPromise<T>(this, context);

    public virtual IScheduledPromise NewScheduledPromise(IContext? context = null) => new ScheduledPromise<object>(this, context);

    public virtual IScheduledFuture<TResult> Schedule<TResult>(ref ScheduledTaskBuilder<TResult> builder) {
        return Select().Schedule(ref builder);
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