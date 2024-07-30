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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.Sequential
{
public abstract class AbstractUniExecutor : IUniExecutorService
{
    private readonly SynchronizationContext _syncContext;
    private readonly TaskScheduler _scheduler;

    protected AbstractUniExecutor() {
        _syncContext = new ExecutorSynchronizationContext(this);
        _scheduler = new ExecutorTaskScheduler(this);
    }

    public SynchronizationContext AsSyncContext() => _syncContext;

    public TaskScheduler AsScheduler() => _scheduler;

    #region lifecycle

    public abstract void Shutdown();

    public abstract List<ITask> ShutdownNow();

    public abstract IFuture TerminationFuture { get; }

    public abstract bool IsShuttingDown { get; }

    public abstract bool IsShutdown { get; }

    public abstract bool IsTerminated { get; }

    public abstract void Update();

    public abstract bool NeedMoreUpdate();

    #endregion

    #region Execute

    public abstract void Execute(ITask task);

    public virtual void Execute(Action action, int options = 0) {
        Execute(Executors.BoxAction(action, options));
    }

    public virtual void Execute(Action<IContext> action, in IContext context, int options = 0) {
        Execute(Executors.BoxAction(action, context, options));
    }

    public virtual void Execute(Action action, CancellationToken cancelToken, int options = 0) {
        Execute(Executors.BoxAction(action, cancelToken, options));
    }

    #endregion

    #region Submit

    public virtual IPromise<T> NewPromise<T>() => new Promise<T>(this);

    public virtual IPromise NewPromise() => new Promise<int>(this);

    public virtual IFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        PromiseTask<T> promiseTask = PromiseTask.OfBuilder(in builder, NewPromise<T>());
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        PromiseTask<T> promiseTask = PromiseTask.OfFunction(action, null, options, NewPromise<T>());
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0) {
        PromiseTask<T> promiseTask = PromiseTask.OfFunction(action, context, options, NewPromise<T>());
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IFuture SubmitAction(Action action, int options = 0) {
        PromiseTask<int> promiseTask = PromiseTask.OfAction(action, null, options, NewPromise<int>());
        Execute(promiseTask);
        return promiseTask.Future;
    }

    public virtual IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0) {
        PromiseTask<int> promiseTask = PromiseTask.OfAction(action, context, options, NewPromise<int>());
        Execute(promiseTask);
        return promiseTask.Future;
    }

    #endregion
}
}