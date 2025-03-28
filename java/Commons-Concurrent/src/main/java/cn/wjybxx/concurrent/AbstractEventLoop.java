/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.concurrent;

import cn.wjybxx.base.fx.ComponentId;
import cn.wjybxx.base.fx.IComponent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/7
 */
@SuppressWarnings({"NullableProblems", "RedundantMethodOverride"})
public abstract class AbstractEventLoop implements IEventLoop {

    protected static final Logger logger = LoggerFactory.getLogger(AbstractEventLoop.class);

    protected final IEventLoopGroup parent;
    protected final Collection<IEventLoop> selfCollection = Collections.singleton(this);
    private final ExecutorServiceAdapter adapter;

    /** 所有的模块 -- 不可变List，保留为添加顺序 */
    protected final List<EventLoopModule> moduleList;
    /** 高速缓存的模块列表 */
    protected final List<EventLoopModule> indexedModuleList;
    /** 重写了update方法的模块 */
    protected final List<EventLoopModule> updateModuleList;
    /** 重写了lateUpdate方法的模块 */
    protected final List<EventLoopModule> lateUpdateModuleList;

    protected AbstractEventLoop(@Nullable IEventLoopGroup parent,
                                @Nonnull List<EventLoopModule> moduleList) {
        this.parent = parent;
        this.adapter = new ExecutorServiceAdapter(this);
        // 需要去重 - 兼容性
        final LinkedHashSet<EventLoopModule> copiedModuleList = new LinkedHashSet<>(moduleList);
        this.moduleList = List.copyOf(copiedModuleList);
        this.indexedModuleList = List.of(EventLoopUtils.toIndexedArray(copiedModuleList));
        // 需要Update的模块缓存
        this.updateModuleList = copiedModuleList.stream()
                .filter(e -> e.getCid().isPrivateScript())
                .filter(EventLoopUtils::isOverrideUpdate)
                .toList();
        this.lateUpdateModuleList = copiedModuleList.stream()
                .filter(e -> e.getCid().isPrivateScript())
                .filter(EventLoopUtils::isOverrideLateUpdate)
                .toList();
    }

    @Override
    public abstract void execute(Runnable command);

    @Override
    public abstract void execute(Runnable command, int options);

    // region IEventLoop

    @Nullable
    @Override
    public IEventLoopGroup parent() {
        return parent;
    }

    @Nonnull
    @Override
    public IEventLoop select() {
        return this;
    }

    @Nonnull
    @Override
    public IEventLoop select(int key) {
        return this;
    }

    @Override
    public int childCount() {
        return 1;
    }

    @Override
    public final void ensureInEventLoop() {
        if (!inEventLoop()) {
            throw new GuardedOperationException();
        }
    }

    @Override
    public final void ensureInEventLoop(String method) {
        Objects.requireNonNull(method);
        if (!inEventLoop()) {
            throw new GuardedOperationException("Calling " + method + " must in the EventLoop");
        }
    }

    public final void throwIfInEventLoop(String method) {
        if (inEventLoop()) {
            throw new BlockingOperationException("Calling " + method + " from within the EventLoop is not allowed");
        }
    }

    // endregion

    // region submit

    @Override
    public <T> IPromise<T> newPromise() {
        return new Promise<>(this);
    }

    @Override
    public <T> IFuture<T> submit(@Nonnull TaskBuilder<T> builder) {
        IPromise<T> promise = newPromise();
        execute(PromiseTask.ofBuilder(builder, promise));
        return promise;
    }

    @Override
    public <T> IFuture<T> submitFunc(Callable<? extends T> task, int options) {
        IPromise<T> promise = newPromise();
        execute(PromiseTask.ofFunction(task, null, options, promise));
        return promise;
    }

    @Override
    public <T> IFuture<T> submitFunc(Callable<? extends T> task, ICancelToken cancelToken, int options) {
        IPromise<T> promise = newPromise();
        execute(PromiseTask.ofFunction(task, cancelToken, options, promise));
        return promise;
    }

    @Override
    public <T> IFuture<T> submitFunc(Function<Object, ? extends T> task, Object ctx, int options) {
        IPromise<T> promise = newPromise();
        execute(PromiseTask.ofFunction(task, ctx, options, promise));
        return promise;
    }

    /** 该方法可能和{@link ExecutorService#submit(Runnable, Object)}冲突，因此我们要带后缀 */
    @Override
    public IFuture<?> submitAction(Runnable task, int options) {
        IPromise<Object> promise = newPromise();
        execute(PromiseTask.ofAction(task, null, options, promise));
        return promise;
    }

    @Override
    public IFuture<?> submitAction(Runnable task, ICancelToken cancelToken, int options) {
        IPromise<Object> promise = newPromise();
        execute(PromiseTask.ofAction(task, cancelToken, options, promise));
        return promise;
    }

    @Override
    public IFuture<?> submitAction(Consumer<Object> task, Object ctx, int options) {
        IPromise<Object> promise = newPromise();
        execute(PromiseTask.ofAction(task, ctx, options, promise));
        return promise;
    }

    @Override
    public final <T> IFuture<T> submitFunc(Callable<? extends T> task) {
        return submitFunc(task, 0);
    }

    @Override
    public final IFuture<?> submitAction(Runnable task) {
        return submitAction(task, 0);
    }

    @SuppressWarnings("deprecation")
    @Nonnull
    @Override
    public final <T> IFuture<T> submit(Callable<T> task) {
        return submitFunc(task, 0);
    }

    @SuppressWarnings("deprecation")
    @Override
    public final IFuture<?> submit(Runnable task) {
        return submitAction(task, 0);
    }

    // endregion

    // region schedule

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise() {
        return new ScheduledPromise<>(this);
    }

    protected abstract ISchedulerHelper helper();

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduledTaskBuilder<V> builder) {
        IScheduledPromise<V> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public <V> IScheduledFuture<V> scheduleFunc(Callable<V> task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        IScheduledPromise<V> promise = newScheduledPromise();
        ISchedulerHelper helper = helper();

        execute(ScheduledPromiseTask.ofFunction(task, cancelToken, 0, promise, helper, helper.triggerTime(delay, unit)));
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleAction(Runnable task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        IScheduledPromise<Object> promise = newScheduledPromise();
        ISchedulerHelper helper = helper();

        ScheduledPromiseTask<?> promiseTask = ScheduledPromiseTask.ofAction(task, cancelToken, 0, promise, helper, helper.triggerTime(delay, unit));
        execute(promiseTask);
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit, ICancelToken cancelToken) {
        ScheduledTaskBuilder<Object> builder = ScheduledTaskBuilder.newAction(task, cancelToken)
                .setFixedDelay(initialDelay, delay, unit);

        IScheduledPromise<Object> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit, ICancelToken cancelToken) {
        ScheduledTaskBuilder<Object> builder = ScheduledTaskBuilder.newAction(task, cancelToken)
                .setFixedRate(initialDelay, period, unit);

        IScheduledPromise<Object> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public final IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        return scheduleAction(task, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final <V> IScheduledFuture<V> schedule(Callable<V> task, long delay, TimeUnit unit) {
        return scheduleFunc(task, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        return scheduleWithFixedDelay(task, initialDelay, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        return scheduleAtFixedRate(task, initialDelay, period, unit, ICancelToken.NONE);
    }
    // endregion

    // region invoke

    @Nonnull
    @Override
    public <T> T invokeAny(Collection<? extends Callable<T>> tasks) throws InterruptedException, ExecutionException {
        throwIfInEventLoop("invokeAny");
        return adapter.invokeAny(tasks);
    }

    @Override
    public <T> T invokeAny(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        throwIfInEventLoop("invokeAny");
        return adapter.invokeAny(tasks, timeout, unit);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks) throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return adapter.invokeAll(tasks);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return adapter.invokeAll(tasks, timeout, unit);
    }

    // endregion

    // region iteration

    @Nonnull
    @Override
    public final Iterator<IEventLoop> iterator() {
        return selfCollection.iterator();
    }

    @Override
    public final void forEach(Consumer<? super IEventLoop> action) {
        selfCollection.forEach(action);
    }

    @Override
    public final Spliterator<IEventLoop> spliterator() {
        return selfCollection.spliterator();
    }

    // endregion

    protected static void logCause(Throwable t) {
        if (t instanceof VirtualMachineError) {
            logger.error("A task raised an exception.", t);
        } else {
            logger.warn("A task raised an exception.", t);
        }
    }

    // region 组件模式

    @Override
    public final void addComponent(IComponent comp) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final boolean delComponent(IComponent comp) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final boolean containsComponent(IComponent comp) {
        int index = comp.getCid().index;
        return index < indexedModuleList.size() && indexedModuleList.get(index) == comp;
    }

    @Override
    public final List<? extends IComponent> getComponents() {
        return moduleList;
    }

    @Override
    public final int getComponents(List<IComponent> outList) {
        outList.addAll(moduleList);
        return moduleList.size();
    }

    @Override
    public final int countComponent() {
        return moduleList.size();
    }

    @SuppressWarnings("unchecked")
    @Override
    public final <T extends IComponent> T getComponent(ComponentId<T> cid) {
        IComponent comp;
        if (cid.index < indexedModuleList.size()
                && (comp = indexedModuleList.get(cid.index)) != null
                && comp.getCid() == cid) {
            return (T) comp;
        }
        return null;
    }

    @Override
    public final <T extends IComponent> T getLastComponent(ComponentId<T> cid) {
        return getComponent(cid);
    }

    @Override
    public final <T extends IComponent> List<T> getComponents(ComponentId<T> cid) {
        T component = getComponent(cid);
        if (component == null) {
            return new ArrayList<>();
        }
        ArrayList<T> result = new ArrayList<>(1);
        result.add(component);
        return result;
    }

    @Override
    public final <T extends IComponent> int getComponents(ComponentId<T> cid, List<? super T> outList) {
        T component = getComponent(cid);
        if (component == null) {
            return 0;
        }
        outList.add(component);
        return 1;
    }

    @Override
    public final <T extends IComponent> T delComponent(ComponentId<T> cid) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final <T extends IComponent> T delLastComponent(ComponentId<T> cid) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final <T extends IComponent> List<T> delComponents(ComponentId<T> cid) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final <T extends IComponent> int delComponents(ComponentId<T> cid, List<? super T> outList) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final int countComponent(ComponentId<?> cid) {
        return getComponent(cid) != null ? 1 : 0;
    }

    // endregion

}