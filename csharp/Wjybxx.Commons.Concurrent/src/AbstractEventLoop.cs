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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 事件循环的模板实现
/// (Unity下不继承MonoBehaviour，因为该类不支持空构造函数)
/// </summary>
public abstract class AbstractEventLoop : IEventLoop
{
    private readonly IEventLoopGroup? _parent;
    private readonly IList<IEventLoop> _selfCollection;
    private readonly SynchronizationContext _syncContext;
    private readonly TaskScheduler _scheduler;

    /** 所有的模块 -- 不可变List，保留为添加顺序 */
    protected readonly ImmutableList<EventLoopModule> moduleList;
    /** 高速缓存的模块列表 */
    protected readonly ImmutableList<EventLoopModule?> indexedModuleList;
    /** 重写了earlyUpdate方法的模块 */
    protected readonly ImmutableList<EventLoopModule> earlyUpdateModuleList;
    /** 重写了update方法的模块 */
    protected readonly ImmutableList<EventLoopModule> updateModuleList;
    /** 重写了lateUpdate方法的模块 */
    protected readonly ImmutableList<EventLoopModule> lateUpdateModuleList;

    protected AbstractEventLoop(IEventLoopGroup? parent,
                                List<EventLoopModule> moduleList) {
        _parent = parent;
        _selfCollection = ImmutableList<IEventLoop>.CreateRange(new[] { this });
        _syncContext = new ExecutorSynchronizationContext(this);
        _scheduler = new ExecutorTaskScheduler(this);
        // 需要去重
        LinkedHashSet<EventLoopModule> copiedModuleList = new LinkedHashSet<EventLoopModule>(moduleList);
        this.moduleList = copiedModuleList.ToImmutableList2();
        this.indexedModuleList = ToIndexedArray(copiedModuleList).ToImmutableList2();
        // 需要update的模块缓存
        this.earlyUpdateModuleList = copiedModuleList
            .Where(e => e.Cid.IsPrivateScript)
            .Where(EventLoopModuleUtil.IsOverrideEarlyUpdate)
            .ToImmutableList2();
        this.updateModuleList = copiedModuleList
            .Where(e => e.Cid.IsPrivateScript)
            .Where(EventLoopModuleUtil.IsOverrideUpdate)
            .ToImmutableList2();
        this.lateUpdateModuleList = copiedModuleList
            .Where(e => e.Cid.IsPrivateScript)
            .Where(EventLoopModuleUtil.IsOverrideLateUpdate)
            .ToImmutableList2();
    }

    public SynchronizationContext AsSyncContext() => _syncContext;

    public TaskScheduler AsScheduler() => _scheduler;

    // 允许子类转换类型
    public virtual IEventLoopGroup? Parent => _parent;

    // 允许子类转换类型
    public virtual IEventLoop Select() => this;

    // 允许子类转换类型
    public virtual IEventLoop Select(int key) => this;

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

    public abstract long TickTime { get; }

    public abstract IFuture RunningFuture { get; }

    public abstract IFuture TerminationFuture { get; }

    public abstract EventLoopState State { get; }

    public virtual bool IsRunning => State == EventLoopState.Running;

    public virtual bool IsShuttingDown => State >= EventLoopState.ShuttingDown;

    public virtual bool IsShutdown => State >= EventLoopState.Shutdown;

    public virtual bool IsTerminated => State >= EventLoopState.Terminated;

    public virtual bool AwaitTermination(TimeSpan timeout) {
        return TerminationFuture.Await(timeout);
    }

    #endregion

    #region Execute

    public abstract void Execute(ITask task);

    public virtual void Execute(Action action, int options = 0) {
        Execute(ExecutorUtil.ToTask(action, options));
    }

    #endregion

    #region Submit

    public virtual IPromise<T> NewPromise<T>() => new Promise<T>(this);

    public virtual IPromise<int> NewPromise() => new Promise<int>(this);

    public virtual ValueFuture<T> Submit<T>(in TaskBuilder<T> builder) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(PromiseTask.OfBuilder(in builder, promise));
        return promise.Future;
    }

    public virtual ValueFuture SubmitAction(Action action, int options = 0) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(PromiseTask.OfAction(action, default, options, promise));
        return promise.VoidFuture;
    }

    public virtual ValueFuture SubmitAction(Action action, CancellationToken cancelToken, int options = 0) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(PromiseTask.OfAction(action, cancelToken, options, promise));
        return promise.VoidFuture;
    }

    public virtual ValueFuture SubmitAction(Action<object> action, object ctx, int options = 0) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(PromiseTask.OfAction(action, ctx, options, promise));
        return promise.VoidFuture;
    }

    public virtual ValueFuture<T> SubmitFunc<T>(Func<T> action, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(PromiseTask.OfFunction(action, default, options, promise));
        return promise.Future;
    }

    public virtual ValueFuture<T> SubmitFunc<T>(Func<T> action, CancellationToken cancelToken, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(PromiseTask.OfFunction(action, cancelToken, options, promise));
        return promise.Future;
    }

    public virtual ValueFuture<T> SubmitFunc<T>(Func<object, T> action, object ctx, int options = 0) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(PromiseTask.OfFunction(action, ctx, options, promise));
        return promise.Future;
    }

    #endregion

    #region Schedule

    protected abstract ISchedulerHelper SchedulerHelper { get; }

    private void InitTriggerTime(IScheduledFutureTask promiseTask, TimeSpan delay) {
        ISchedulerHelper helper = SchedulerHelper;
        promiseTask.Helper = helper;
        promiseTask.Id = helper.NextId();
        promiseTask.TriggerTime = helper.TriggerTime(delay);
    }

    public virtual ValueFuture<T> Schedule<T>(in ScheduledTaskBuilder<T> builder) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        ScheduledPromiseTask<T> promiseTask = ScheduledPromiseTask.OfBuilder(in builder, promise);
        //
        ISchedulerHelper helper = SchedulerHelper;
        promiseTask.Helper = helper;
        promiseTask.Id = helper.NextId();
        promiseTask.TriggerTime = helper.TriggerTime(builder.InitialDelay, builder.TimeUnit);
        //
        promiseTask.ScheduleType = builder.ScheduleType;
        if (builder.IsPeriodic) {
            promiseTask.Period = helper.TriggerPeriod(builder.Period, builder.TimeUnit);
        }
        if (builder.HasTimeout) {
            promiseTask.Deadline = helper.TriggerTime(builder.Timeout, builder.TimeUnit);
        }
        if (builder.HasCountLimit) {
            promiseTask.Countdown = builder.CountLimit;
        }
        Execute(promiseTask); // 可能会立即完成
        return promise.Future;
    }

    public virtual ValueFuture ScheduleAction(Action action, TimeSpan delay, CancellationToken cancelToken = default) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        ScheduledPromiseTask<int> promiseTask = ScheduledPromiseTask.OfAction(action, cancelToken, 0, promise);
        InitTriggerTime(promiseTask, delay);

        Execute(promiseTask);
        return promise.VoidFuture;
    }

    public ValueFuture ScheduleAction(Action<object> action, object ctx, TimeSpan delay) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        ScheduledPromiseTask<int> promiseTask = ScheduledPromiseTask.OfAction(action, ctx, 0, promise);
        InitTriggerTime(promiseTask, delay);

        Execute(promiseTask);
        return promise.VoidFuture;
    }

    public virtual ValueFuture<T> ScheduleFunc<T>(Func<T> action, TimeSpan delay, CancellationToken cancelToken = default) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        ScheduledPromiseTask<T> promiseTask = ScheduledPromiseTask.OfFunction(action, cancelToken, 0, promise);
        InitTriggerTime(promiseTask, delay);

        Execute(promiseTask);
        return promise.Future;
    }

    public ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object ctx, TimeSpan delay) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        ScheduledPromiseTask<T> promiseTask = ScheduledPromiseTask.OfFunction(action, ctx, 0, promise);
        InitTriggerTime(promiseTask, delay);

        Execute(promiseTask);
        return promise.Future;
    }

    public virtual ValueFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, CancellationToken cancelToken = default) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedDelay(delay.Ticks, period.Ticks, new TimeSpan(1));
        return Schedule(in builder).Box(false);
    }

    public virtual ValueFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, CancellationToken cancelToken = default) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedRate(delay.Ticks, period.Ticks, new TimeSpan(1));
        return Schedule(in builder).Box(false);
    }

    #endregion

    #region 组件模式

    // ReSharper disable PossibleUnintendedReferenceComparison

    public List<IEventLoopModule> GetComponents() {
        return new List<IEventLoopModule>(moduleList);
    }

    public void GetComponents(List<IEventLoopModule> outList) {
        outList.AddRange(moduleList);
    }

    public int ComponentCount => moduleList.Count;

    public T? GetComponent<T>(ComponentId<T> cid) where T : class {
        IEventLoopModule? comp;
        if (cid.cacheIndex < indexedModuleList.Count
            && (comp = indexedModuleList[cid.cacheIndex]) != null
            && comp.Cid == cid) {
            return (T)comp;
        }
        return null;
    }

    public IEventLoopModule? GetComponent(ComponentId cid) {
        IEventLoopModule? comp;
        if (cid.cacheIndex < indexedModuleList.Count
            && (comp = indexedModuleList[cid.cacheIndex]) != null
            && comp.Cid == cid) {
            return comp;
        }
        return null;
    }

    public bool ContainsComponent(IEventLoopModule comp) {
        int index = comp.Cid.cacheIndex;
        return index < indexedModuleList.Count && indexedModuleList[index] == comp;
    }

    #endregion

    #region util

    /** 将组件散开为基于组件index的数组 -- 暂时禁止组件重复 */
    private static EventLoopModule[] ToIndexedArray(ICollection<EventLoopModule> moduleList) {
        if (moduleList.Count == 0) {
            return Array.Empty<EventLoopModule>();
        }
        int maxIndex = moduleList
            .Select(e => e.Cid.cacheIndex)
            .Max();

        EventLoopModule[] result = new EventLoopModule[maxIndex + 1];
        foreach (EventLoopModule module in moduleList) {
            EventLoopModule exist = result[module.Cid.cacheIndex];
            if (exist != null) {
                throw new IllegalStateException("module is duplicate, cid: " + module.Cid);
            }
            result[module.Cid.cacheIndex] = module;
        }
        return result;
    }

    #endregion
}
}