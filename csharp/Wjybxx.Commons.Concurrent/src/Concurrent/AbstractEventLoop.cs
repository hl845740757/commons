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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
public abstract class AbstractEventLoop : IEventLoop
{
    private readonly IEventLoopGroup? _parent;
    private readonly IList<IEventLoop> _selfCollection;
    private readonly SynchronizationContext _syncContext;
    private readonly TaskScheduler _scheduler;

    protected AbstractEventLoop(IEventLoopGroup? parent) {
        _parent = parent;
        _selfCollection = ImmutableList<IEventLoop>.CreateRange(new[] { this });

        _syncContext = new ExecutorSynchronizationContext(this);
        _scheduler = new ExecutorTaskScheduler(this);
    }

    public SynchronizationContext AsSyncContext() => _syncContext;

    public TaskScheduler AsScheduler() => _scheduler;

    // 允许子类转换类型
    public virtual IEventLoopGroup? Parent => _parent;

    // 允许子类转换类型
    public virtual IEventLoop Select() => this;

    // 允许子类转换类型
    public virtual IEventLoop Select(int key) => this;

    public abstract IEventLoopModule? MainModule { get; }

    public int ChildCount => 1;

    IEnumerator IEnumerable.GetEnumerator() {
        return _selfCollection.GetEnumerator();
    }

    public IEnumerator<IEventLoop> GetEnumerator() {
        return _selfCollection.GetEnumerator();
    }

    #region 生命周期

    public abstract IFuture Start();

    public abstract void Shutdown();

    public abstract List<ITask> ShutdownNow();

    public abstract bool InEventLoop();

    public abstract bool InEventLoop(Thread thread);

    public abstract void Wakeup();

    public abstract IFuture RunningFuture { get; }

    public abstract IFuture TerminationFuture { get; }

    public abstract EventLoopState State { get; }

    public virtual bool IsRunning => State == EventLoopState.Running;

    public virtual bool IsShuttingDown => State >= EventLoopState.ShuttingDown;

    public virtual bool IsShutdown => State >= EventLoopState.Shutdown;

    public virtual bool IsTerminated => State >= EventLoopState.Terminated;

    public bool AwaitTermination(TimeSpan timeout) {
        return TerminationFuture.Await(timeout);
    }

    public void EnsureInEventLoop() {
        if (!InEventLoop()) {
            throw new GuardedOperationException();
        }
    }

    public void EnsureInEventLoop(string method) {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (!InEventLoop()) {
            throw new GuardedOperationException("Calling " + method + " must in the EventLoop");
        }
    }

    /** 如果当前在事件循环异常则抛出异常 */
    public void ThrowIfInEventLoop(string method) {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (InEventLoop()) {
            throw new BlockingOperationException("Calling " + method + " from within the EventLoop is not allowed");
        }
    }

    #endregion

    #region Execute

    public abstract void Execute(ITask task);

    public virtual void Execute(Action action, int options = 0) {
        Execute(Executors.ToTask(action, options));
    }

    #endregion

    #region Submit

    public virtual IPromise<T> NewPromise<T>() => new Promise<T>(this);

    public virtual IPromise<int> NewPromise() => new Promise<int>(this);

    public virtual IFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        IPromise<T> promise = NewPromise<T>();
        Execute(PromiseTask.OfBuilder(in builder, promise));
        return promise;
    }

    public virtual IFuture SubmitAction(Action action, int options = 0) {
        IPromise<int> promise = NewPromise();
        Execute(PromiseTask.OfAction(action, null, options, promise));
        return promise;
    }

    public virtual IFuture SubmitAction(Action action, ICancelToken cancelToken, int options = 0) {
        IPromise<int> promise = NewPromise();
        Execute(PromiseTask.OfAction(action, cancelToken, options, promise));
        return promise;
    }

    public virtual IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0) {
        IPromise<int> promise = NewPromise();
        Execute(PromiseTask.OfAction(action, context, options, promise));
        return promise;
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        IPromise<T> promise = NewPromise<T>();
        Execute(PromiseTask.OfFunction(action, null, options, promise));
        return promise;
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<T> action, ICancelToken cancelToken, int options = 0) {
        IPromise<T> promise = NewPromise<T>();
        Execute(PromiseTask.OfFunction(action, cancelToken, options, promise));
        return promise;
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0) {
        IPromise<T> promise = NewPromise<T>();
        Execute(PromiseTask.OfFunction(action, context, options, promise));
        return promise;
    }

    #endregion

    #region Schedule

    // 默认不支持定时任务

    public virtual IScheduledPromise<T> NewScheduledPromise<T>() => new ScheduledPromise<T>(this);

    public virtual IScheduledPromise<int> NewScheduledPromise() => new ScheduledPromise<int>(this);

    public virtual IScheduledFuture<TResult> Schedule<TResult>(in ScheduledTaskBuilder<TResult> builder) {
        throw new NotImplementedException();
    }

    public virtual IScheduledFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        throw new NotImplementedException();
    }

    public virtual IScheduledFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        throw new NotImplementedException();
    }

    public virtual IScheduledFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        throw new NotImplementedException();
    }

    public virtual IScheduledFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        throw new NotImplementedException();
    }

    #endregion
}
}