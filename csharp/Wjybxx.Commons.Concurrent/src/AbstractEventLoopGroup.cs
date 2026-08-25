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

    public abstract IEventLoop Select(int key);

    #region 生命周期

    public abstract void Shutdown();

    public abstract List<ITask> ShutdownNow();

    public abstract bool IsShuttingDown { get; }

    public abstract bool IsShutdown { get; }

    public abstract bool IsTerminated { get; }

    public abstract IFuture<int> TerminationFuture { get; }

    #endregion

    #region submit

    public virtual void Execute(ITask task) {
        Select().Execute(task);
    }

    public virtual void Execute(Action action, int options = 0) {
        Select().Execute(action, options);
    }

    public virtual IPromise<T> NewPromise<T>() => new Promise<T>(this);

    public virtual IPromise<int> NewPromise() => new Promise<int>(this);

    public virtual ValueFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        return Select().Submit(in builder);
    }

    public virtual ValueFuture SubmitAction(Action action, int options = 0, CancellationToken cancelToken = default) {
        return Select().SubmitAction(action, options, cancelToken);
    }

    public virtual ValueFuture SubmitAction(Action<object> action, object? state, int options = 0, CancellationToken cancelToken = default) {
        return Select().SubmitAction(action, state, options, cancelToken);
    }


    public virtual ValueFuture<T> SubmitFunc<T>(Func<T> action, int options = 0, CancellationToken cancelToken = default) {
        return Select().SubmitFunc(action, options, cancelToken);
    }

    public virtual ValueFuture<T> SubmitFunc<T>(Func<object, T> action, object? state, int options = 0, CancellationToken cancelToken = default) {
        return Select().SubmitFunc(action, state, options, cancelToken);
    }

    #endregion

    #region schedule

    public virtual ValueFuture<TResult> Schedule<TResult>(in ScheduledTaskBuilder<TResult> builder) {
        return Select().Schedule(in builder);
    }

    public virtual ValueFuture ScheduleAction(Action action, TimeSpan delay, CancellationToken cancelToken = default) {
        return Select().ScheduleAction(action, delay, cancelToken);
    }

    public ValueFuture ScheduleAction(Action<object> action, object? state, TimeSpan delay, CancellationToken cancelToken = default) {
        return Select().ScheduleAction(action, state, delay, cancelToken);
    }

    public virtual ValueFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, CancellationToken cancelToken = default) {
        return Select().ScheduleFunc(action, delay, cancelToken);
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object? state, TimeSpan delay, CancellationToken cancelToken = default) {
        return Select().ScheduleFunc(action, state, delay, cancelToken);
    }

    public virtual ValueFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, CancellationToken cancelToken = default) {
        return Select().ScheduleWithFixedDelay(action, delay, period, cancelToken);
    }

    public virtual ValueFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, CancellationToken cancelToken = default) {
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