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
    protected readonly ImmutableList<EventLoopModule> _moduleList;
    /** 高速缓存的模块列表 */
    protected readonly ImmutableList<EventLoopModule?> _indexedModuleList;
    /** 重写了update方法的模块 */
    protected readonly ImmutableList<EventLoopModule> _updateModuleList;
    /** 重写了lateUpdate方法的模块 */
    protected readonly ImmutableList<EventLoopModule> _lateUpdateModuleList;

    protected AbstractEventLoop(IEventLoopGroup? parent,
                                List<EventLoopModule> moduleList) {
        _parent = parent;
        _selfCollection = ImmutableList<IEventLoop>.CreateRange(new[] { this });
        _syncContext = new ExecutorSynchronizationContext(this);
        _scheduler = new ExecutorTaskScheduler(this);
        // 需要去重
        LinkedHashSet<EventLoopModule> copiedModuleList = new LinkedHashSet<EventLoopModule>(moduleList);
        _moduleList = copiedModuleList.ToImmutableList2();
        _indexedModuleList = ToIndexedArray(copiedModuleList).ToImmutableList2();
        // 需要update的模块缓存
        _updateModuleList = copiedModuleList
            .Where(e => e.Cid.IsPrivateScript)
            .Where(EventLoopModuleUtil.IsOverrideUpdate)
            .ToImmutableList2();
        _lateUpdateModuleList = copiedModuleList
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
        Execute(ExecutorCoreUtil.ToTask(action, options));
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
        Execute(PromiseTask.OfAction(action, null, options, promise));
        return promise.VoidFuture;
    }

    public virtual ValueFuture SubmitAction(Action action, ICancelToken cancelToken, int options = 0) {
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
        Execute(PromiseTask.OfFunction(action, null, options, promise));
        return promise.Future;
    }

    public virtual ValueFuture<T> SubmitFunc<T>(Func<T> action, ICancelToken cancelToken, int options = 0) {
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

    public virtual ValueFuture<T> Schedule<T>(in ScheduledTaskBuilder<T> builder) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise));
        return promise.Future;
    }

    public virtual ValueFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(ScheduledPromiseTask.OfAction(action, cancelToken, 0, promise, delay));
        return promise.VoidFuture;
    }

    public virtual ValueFuture<T> ScheduleFunc<T>(Func<T> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        ValuePromise<T> promise = ValuePromise<T>.Acquire(this);
        Execute(ScheduledPromiseTask.OfFunction(action, cancelToken, 0, promise, delay));
        return promise.Future;
    }

    public virtual ValueFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedDelay(delay.Ticks, period.Ticks, new TimeSpan(1));

        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise));
        return promise.VoidFuture;
    }

    public virtual ValueFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(action, cancelToken);
        builder.SetFixedRate(delay.Ticks, period.Ticks, new TimeSpan(1));

        ValuePromise<int> promise = ValuePromise<int>.Acquire(this);
        Execute(ScheduledPromiseTask.OfBuilder(in builder, promise));
        return promise.VoidFuture;
    }

    #endregion

    #region 组件模式

    public void AddComponent(IComponent comp) {
        throw new NotImplementedException();
    }

    public bool DelComponent(IComponent comp) {
        throw new NotImplementedException();
    }

    public bool ContainsComponent(IComponent comp) {
        int index = comp.Cid.Index;
        return index < _indexedModuleList.Count && _indexedModuleList[index] == comp;
    }

    public IList<IComponent> GetComponents() {
        // .net8 才支持泛型协变
        return new List<IComponent>(_moduleList);
    }

    public int GetComponents(List<IComponent> outList) {
        outList.AddRange(_moduleList);
        return _moduleList.Count;
    }

    public int CountComponent() {
        return _moduleList.Count;
    }

    #region 泛型

    public T GetComponent<T>(ComponentId<T> cid) where T : IComponent {
        IComponent? comp;
        if (cid.Index < _indexedModuleList.Count
            && (comp = _indexedModuleList[cid.Index]) != null
            && ReferenceEquals(comp.Cid, cid)) {
            return (T)comp;
        }
        return default;
    }

    public T GetLastComponent<T>(ComponentId<T> cid) where T : IComponent {
        return GetComponent(cid);
    }

    public List<T> GetComponents<T>(ComponentId<T> cid) where T : IComponent {
        T component = GetComponent(cid);
        if (component == null) {
            return new List<T>(0);
        }
        return new List<T>(1) { component };
    }

    public int GetComponents<T>(ComponentId<T> cid, List<T> outList) where T : IComponent {
        T component = GetComponent(cid);
        if (component == null) {
            return 0;
        }
        outList.Add(component);
        return 1;
    }

    public T DelComponent<T>(ComponentId<T> cid) where T : IComponent {
        throw new NotImplementedException();
    }

    public T DelLastComponent<T>(ComponentId<T> cid) where T : IComponent {
        throw new NotImplementedException();
    }

    public List<T> DelComponents<T>(ComponentId<T> cid) where T : IComponent {
        throw new NotImplementedException();
    }

    public int DelComponents<T>(ComponentId<T> cid, List<T> outList) where T : IComponent {
        throw new NotImplementedException();
    }

    #endregion

    #region 非泛型

    public int CountComponent(ComponentId cid) {
        return GetComponent(cid) == null ? 0 : 1;
    }

    public IComponent? GetComponent(ComponentId cid) {
        IComponent? comp;
        if (cid.Index < _indexedModuleList.Count
            && (comp = _indexedModuleList[cid.Index]) != null
            && ReferenceEquals(comp.Cid, cid)) {
            return comp;
        }
        return null;
    }

    public IComponent? GetLastComponent(ComponentId cid) {
        return GetComponent(cid);
    }

    public List<IComponent> GetComponents(ComponentId cid) {
        IComponent component = GetComponent(cid);
        if (component == null) {
            return new List<IComponent>(0);
        }
        return new List<IComponent>(1) { component };
    }

    public int GetComponents(ComponentId cid, List<IComponent> outList) {
        IComponent component = GetComponent(cid);
        if (component == null) {
            return 0;
        }
        outList.Add(component);
        return 1;
    }

    public IComponent DelComponent(ComponentId cid) {
        throw new NotImplementedException();
    }

    public IComponent DelLastComponent(ComponentId cid) {
        throw new NotImplementedException();
    }

    public List<IComponent> DelComponents(ComponentId cid) {
        throw new NotImplementedException();
    }

    public int DelComponents(ComponentId cid, List<IComponent> outList) {
        throw new NotImplementedException();
    }

    #endregion

    #endregion

    #region util

    /** 将组件散开为基于组件index的数组 -- 暂时禁止组件重复 */
    private static EventLoopModule[] ToIndexedArray(ICollection<EventLoopModule> moduleList) {
        if (moduleList.Count == 0) {
            return Array.Empty<EventLoopModule>();
        }
        int maxIndex = moduleList
            .Select(e => e.Cid.Index)
            .Max();

        EventLoopModule[] result = new EventLoopModule[maxIndex + 1];
        foreach (EventLoopModule module in moduleList) {
            EventLoopModule exist = result[module.Cid.Index];
            if (exist != null) {
                throw new IllegalStateException("module is duplicate, cid: " + module.Cid);
            }
            result[module.Cid.Index] = module;
        }
        return result;
    }

    #endregion
}
}