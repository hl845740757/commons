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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.annotation.Beta;
import cn.wjybxx.base.annotation.VisibleForTesting;
import cn.wjybxx.base.fx.ComponentStatus;
import cn.wjybxx.disruptor.*;

import javax.annotation.Nonnull;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Collections;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.LockSupport;

/**
 * 基于Disruptor框架的事件循环。
 * 1.这个实现持有私有的RingBuffer，可以有最好的性能。
 * 2.可以通过{@link #nextSequence()}和{@link #publish(long)}发布特殊的事件。
 * 3.不支持EventTranslator，用户可以通过申请序号和event发布
 * <p>
 * 关于时序正确性：
 * 1.由于{@link #schedulerHelper}中的任务都是从{@link #dataProvider}中拉取出来的，因此都是先于{@link #dataProvider}中剩余的任务的。
 * 2.我们总是先取得一个时间快照，然后先执行{@link #schedulerHelper}中的任务，再执行{@link #dataProvider}中的任务，因此满足优先级相同时，先提交的任务先执行的约定
 * -- 反之，如果不使用时间快照，就可能导致后提交的任务先满足触发时间。
 *
 * @author wjybxx
 * date 2023/4/10
 */
public class DisruptorEventLoop<T extends IAgentEvent> extends AbstractEventLoop implements IDisruptorEventLoop<T> {

    private static final int MIN_BATCH_SIZE = 64;
    private static final int MAX_BATCH_SIZE = 64 * 1024;

    /** 无效事件类型 */
    static final int TYPE_INVALID = IAgentEvent.TYPE_INVALID;
    /** 基础事件类型，参数列表为：task */
    static final int TYPE_RUNNABLE = 0;
    /** 删除延时任务，参数列表为：taskId */
    static final int TYPE_REMOVE_SCHEDULE = -1;

    // 填充开始 - 字段定义顺序不要随意调整
    @SuppressWarnings("unused")
    private long p1, p2, p3, p4, p5, p6, p7;
    /** 线程本地时间 -- 纳秒；时间的更新频率极高，进行缓存行填充隔离 */
    private volatile long tickTime;
    @SuppressWarnings("unused")
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    /** 线程状态 -- 下面的final字段充当缓存行填充 */
    private volatile int state = EventLoopState.ST_UNSTARTED;

    /** 事件队列 */
    private final EventSequencer<? extends T> eventSequencer;
    /** 缓存值 -- 减少转发 */
    private final DataProvider<? extends T> dataProvider;
    /** 缓存值 -- 减少运行时测试 */
    private final MpUnboundedBuffer<? extends T> unboundedBuffer;
    /** 定时任务调度器 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private final DisruptorSchedulerHelper schedulerHelper;
    /** 任务拒绝策略 */
    protected final RejectedExecutionHandler rejectedExecutionHandler;
    /** 事件循环主模块 */
    protected final IEventLoopAgent<T> agent;
    /** 批量执行任务的大小 */
    private final int batchSize;

    private final Thread thread;
    private final Worker worker;
    private final long consumerId;

    private final IPromise<Void> runningPromise = new Promise<>(this);
    private final IPromise<Void> terminationPromise = new Promise<>(this);
    // future 缓存
    private final IFuture<Void> runningFuture = runningPromise.asReadonly();
    private final IFuture<Void> terminationFuture = terminationPromise.asReadonly();

    public DisruptorEventLoop(EventLoopBuilder.DisruptorBuilder<T> builder) {
        this(builder, true);
    }

    /** 允许子类延迟绑定agent，以确保绑定时事件循环已初始化完成 */
    protected DisruptorEventLoop(EventLoopBuilder.DisruptorBuilder<T> builder, boolean bindAgent) {
        super(builder.getParent(), builder.getModules());
        ThreadFactory threadFactory = Objects.requireNonNull(builder.getThreadFactory(), "threadFactory");

        this.tickTime = System.nanoTime();
        this.eventSequencer = Objects.requireNonNull(builder.getEventSequencer());
        this.dataProvider = eventSequencer.dataProvider();
        this.schedulerHelper = new DisruptorSchedulerHelper(this);

        this.rejectedExecutionHandler = ObjectUtils.nullToDef(builder.getRejectedExecutionHandler(), RejectedExecutionHandlers.abort());
        this.agent = ObjectUtils.nullToDef(builder.getAgent(), EmptyAgent.getInstance());
        this.batchSize = MathCommon.clamp(builder.getBatchSize(), MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        // 缓存
        if (dataProvider instanceof MpUnboundedBuffer<? extends T> unboundedBuffer2) {
            this.unboundedBuffer = unboundedBuffer2;
        } else {
            this.unboundedBuffer = null;
        }

        // worker只依赖生产者屏障
        ConsumerBarrier barrier = eventSequencer.newSingleConsumerBarrier(builder.getWaitStrategy());
        worker = new Worker(barrier);
        thread = Objects.requireNonNull(threadFactory.newThread(worker), "newThread");
        consumerId = ObjectUtils.zeroToDef(builder.getConsumerId(), thread.threadId());
        DefaultThreadFactory.checkUncaughtExceptionHandler(thread);
        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        eventSequencer.addGatingBarriers(barrier);

        // 绑定Agent
        if (bindAgent) {
            agent.inject(this, consumerId);
        }
    }

    /** EventLoop绑定的Agent（代理） */
    @VisibleForTesting
    IEventLoopAgent<? super T> getAgent() {
        return agent;
    }

    @Override
    public final long tickTime() {
        return tickTime;
    }

    // region 状态查询

    @Override
    public final EventLoopState state() {
        return EventLoopState.valueOf(state);
    }

    @Override
    public final boolean isRunning() {
        return state == EventLoopState.ST_RUNNING;
    }

    @Override
    public final boolean isShuttingDown() {
        return state >= EventLoopState.ST_SHUTTING_DOWN;
    }

    @Override
    public final boolean isShutdown() {
        return state >= EventLoopState.ST_SHUTDOWN;
    }

    @Override
    public final boolean isTerminated() {
        return state == EventLoopState.ST_TERMINATED;
    }

    @Override
    public final IFuture<?> runningFuture() {
        return runningFuture;
    }

    @Override
    public IFuture<?> terminationFuture() {
        return terminationFuture;
    }

    @Override
    public boolean awaitTermination(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        return terminationPromise.await(timeout, unit);
    }

    @Override
    public final boolean inEventLoop() {
        return this.thread == Thread.currentThread();
    }

    @Override
    public final boolean inEventLoop(Thread thread) {
        return this.thread == thread;
    }

    // endregion

    // region 任务提交

    @Override
    public final void execute(Runnable command) {
        int options = command instanceof ITask task ? task.getOptions() : 0;
        execute(command, options);
    }

    @Override
    public final void execute(Runnable command, int options) {
        Objects.requireNonNull(command, "command");
        // 在申请序号之前注入Helper（初始化任务的调度时间，避免被阻塞导致延迟）
        ScheduledPromiseTask<?> promiseTask = null;
        if (command instanceof ScheduledPromiseTask<?> promiseTask2) {
            promiseTask = promiseTask2;
            promiseTask.inject(schedulerHelper);
        }
        long sequence = nextSequence(1);
        if (sequence < 0) {
            rejectedExecutionHandler.rejected(command, this);
            return;
        }
        if (promiseTask != null) {
            promiseTask.setId(sequence); // nice
        }
        publishTask(command, sequence, options);
    }

    /**
     * Q: 如何保证算法的安全性的？
     * A: 我们只需要保证申请到的sequence是有效的，且发布任务在{@link Worker#removeFromGatingBarriers()}之前即可。
     * 因为{@link Worker#removeFromGatingBarriers()}之前申请到的sequence一定是有效的，它考虑了EventLoop的消费进度。
     * <p>
     * 关键时序：
     * 1. {@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前。
     * 2. {@link Worker#cleanBuffer()}必须等待在这之前申请到的sequence发布。
     * 3. {@link Worker#cleanBuffer()}在所有生产者发布数据之后才{@link Worker#removeFromGatingBarriers()}
     * <p>
     * 因此，{@link Worker#cleanBuffer()}之前申请到的sequence是有效的；
     * 又因为{@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前，
     * 因此，如果sequence是在{@link #isShuttingDown()}为true之前申请到的，那么sequence一定是有效的，否则可能有效，也可能无效。
     * <p>
     * sequence的处理见{@link #nextSequence(int)}
     */
    private void publishTask(@Nonnull Runnable task, long sequence, int options) {
        T event = dataProvider.producerGet(sequence);
        try {
            event.setType(0);
            event.setObj1(task);
            event.setOptions(options);
        } finally {
            eventSequencer.publish(sequence);
        }

        // RingBuffer不再私有，不可测试sequence == 0
        if (state == EventLoopState.ST_UNSTARTED) {
            ensureThreadStarted();
        } else if ((options & TaskOptions.WAKEUP_THREAD) != 0 && !inEventLoop()) {
            wakeup();
        }
    }

    /** 获取事件循环的消费者id */
    @Beta
    public final long getConsumerId() {
        return consumerId;
    }

    @Override
    public final T getEvent(long sequence) {
        return dataProvider.producerGet(sequence);
    }

    @Override
    public final long tryNextSequence(int size) {
        if (isShuttingDown()) {
            return -1;
        }
        long sequence = eventSequencer.tryNext(size);
        if (sequence == -1) {
            return -1;
        }
        if (isShuttingDown()) {
            if (size == 1) {
                eventSequencer.publish(sequence);
            } else {
                long lo = sequence - (size - 1);
                eventSequencer.publish(lo, sequence);
            }
            return -1;
        }
        return sequence;
    }

    @Override
    public final long nextSequence() {
        return nextSequence(1);
    }

    @Override
    public final void publish(long sequence) {
        eventSequencer.publish(sequence);
        // RingBuffer不再私有，不可测试sequence == 0
        if (state == EventLoopState.ST_UNSTARTED) {
            ensureThreadStarted();
        }
    }

    @Override
    public final long nextSequence(int size) {
        if (isShuttingDown()) {
            return -1;
        }
        long sequence;
        if (inEventLoop()) {
            // 如果在当前事件循环，不可阻塞
            sequence = eventSequencer.tryNext(size);
            if (sequence == -1) {
                return -1;
            }
        } else {
            sequence = eventSequencer.next(size);
        }
        if (isShuttingDown()) {
            // 申请序号期间收到关闭请求，序号无效
            if (size == 1) {
                eventSequencer.publish(sequence);
            } else {
                long lo = sequence - (size - 1);
                eventSequencer.publish(lo, sequence);
            }
            return -1;
        }
        return sequence;
    }


    @Override
    public final void publish(long lo, long hi) {
        eventSequencer.publish(lo, hi);
        if (state == EventLoopState.ST_UNSTARTED) {
            ensureThreadStarted();
        }
    }

    @Override
    public void subscribe(int type, IAgentEventHandler<? super T> handler) {
        Objects.requireNonNull(handler, "handler");
        if (!inEventLoop()) {
            throw new IllegalStateException();
        }
        agent.subscribe(type, handler);
    }

    // endregion

    // region 线程状态切换

    @Override
    public void wakeup() {
        if (!inEventLoop() && thread.isAlive()) {
            thread.interrupt();
            agent.wakeup();
        }
    }

    @Override
    public IFuture<?> start() {
        ensureThreadStarted();
        return runningFuture;
    }

    @Override
    public void shutdown() {
        if (!runningPromise.isDone()) {
            runningPromise.trySetException(new StartFailedException("Shutdown"));
        }
        int expectedState = state;
        for (; ; ) {
            if (expectedState >= EventLoopState.ST_SHUTTING_DOWN) {
                return;
            }
            int realState = compareAndExchangeState(expectedState, EventLoopState.ST_SHUTTING_DOWN);
            if (realState == expectedState) {
                ensureThreadTerminable(expectedState);
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    @Nonnull
    @Override
    public List<Runnable> shutdownNow() {
        shutdown();
        advanceRunState(EventLoopState.ST_SHUTDOWN);
        // 这里不能操作ringBuffer中的数据，不能打破[多生产者单消费者]的架构
        return Collections.emptyList();
    }

    private void ensureThreadStarted() {
        if (state == EventLoopState.ST_UNSTARTED
                && STATE.compareAndSet(this, EventLoopState.ST_UNSTARTED, EventLoopState.ST_STARTING)) {
            thread.start();
        }
    }

    private void ensureThreadTerminable(int oldState) {
        if (oldState == EventLoopState.ST_UNSTARTED) {
            // TODO 是否需要启动线程，进行更彻底的清理？
            state = EventLoopState.ST_TERMINATED;
            worker.removeFromGatingBarriers(); // 防死锁

            runningPromise.trySetException(new StartFailedException("Stillborn"));
            terminationPromise.trySetResult(null);
        } else {
            // 等待策略是根据alert信号判断EventLoop是否已开始关闭的，因此即使inEventLoop也需要alert，否则可能丢失信号，在waitFor处无法停止
            worker.barrier.alert();
            // 唤醒线程 - 如果线程可能阻塞在其它地方
            wakeup();
        }
    }

    /**
     * 将运行状态转换为给定目标，或者至少保留给定状态。
     *
     * @param targetState 期望的目标状态
     */
    private void advanceRunState(int targetState) {
        int expectedState = state;
        for (; ; ) {
            if (expectedState >= targetState) {
                return;
            }
            int realState = compareAndExchangeState(expectedState, targetState);
            if (realState >= targetState) { // == 表示CAS成功， > 表示已进入目标状态
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    private int compareAndExchangeState(int expectedState, int targetState) {
        return (int) STATE.compareAndExchange(this, expectedState, targetState);
    }
    // endregion

    // region modules

    /** 事件循环启动 */
    protected void onStart() throws Throwable {
        agent.beforeEventLoopStart();
        startModules();
        agent.afterEventLoopStart();
    }

    /** 事件循环开始退出 */
    protected void onShutdown() throws Throwable {
        agent.beforeEventLoopShutdown();
        stopModules();
        destroyModules();
        agent.afterEventLoopShutdown();
    }

    /** 启动所有模块 */
    protected final void startModules() throws Throwable {
        // 模块的部分数据初始化 - OnReady
        for (EventLoopModule module : moduleList) {
            if (module.getCid().shared) {
                continue; // 共享组件
            }
            if (module.getEntity() != null) {
                continue; // 通常是主模块提前完成了绑定
            }
            module.setEventLoop(this);
        }
        // 解决模块之间的依赖
        for (EventLoopModule module : moduleList) {
            if (module.getCid().shared) {
                continue; // 共享组件
            }
            module.resolveDependence();
        }
        // 顺序启动 - Start
        for (EventLoopModule module : moduleList) {
            if (!module.getCid().isPrivateScript()) {
                continue;
            }
            Throwable ex = module.invokeStart();
            if (ex != null) {
                throw ex;
            }
        }
    }

    /** 停止所有模块 */
    protected final void stopModules() {
        // 逆序停止
        for (int i = moduleList.size() - 1; i >= 0; i--) {
            EventLoopModule module = moduleList.get(i);
            if (!module.getCid().isPrivateScript()) {
                continue;
            }
            if (module.getStatus() != ComponentStatus.STARTING
                    && module.getStatus() != ComponentStatus.RUNNING) {
                continue; // 未启动
            }
            Throwable ex = module.invokeStop();
            if (ex != null) { // stop异常记录下来，继续停止其它模块
                logger.warn("stop module caught exception", ex);
            }
        }
    }

    /** 销毁所有模块 -- 不删除引用 */
    protected final void destroyModules() {
        // 顺序销毁 -- 组件之间不能有时序依赖
        for (EventLoopModule module : moduleList) {
            if (module.getCid().shared) {
                continue;
            }
            if (module.getStatus() == ComponentStatus.NEW) {
                continue; // 未执行OnReady
            }
            Throwable ex = module.invokeDestroy();
            if (ex != null) { // destroy异常记录下来，继续销毁其它模块
                logger.warn("module.destroy caught exception", ex);
            }
        }
    }

    /** 迭代所有模块 */
    protected final void updateModules() {
        long tickTime = this.tickTime;
        while (agent.checkMainLoop(tickTime)) {
            agent.beforeMainLoop(tickTime);
            for (int i = 0; i < earlyUpdateModuleList.size(); i++) {
                EventLoopModule module = earlyUpdateModuleList.get(i);
                try {
                    module.earlyUpdate();
                } catch (Throwable ex) {
                    logger.info("module.earlyUpdate caught exception", ex);
                }
            }
            for (int i = 0; i < updateModuleList.size(); i++) {
                EventLoopModule module = updateModuleList.get(i);
                try {
                    module.update();
                } catch (Throwable ex) {
                    logger.info("module.update caught exception", ex);
                }
            }
            for (int i = 0; i < lateUpdateModuleList.size(); i++) {
                EventLoopModule module = lateUpdateModuleList.get(i);
                try {
                    module.lateUpdate();
                } catch (Throwable ex) {
                    logger.info("module.lateUpdate caught exception", ex);
                }
            }
            agent.afterMainLoop(tickTime);
        }
        agent.customUpdate(tickTime);
    }

    private void onInternalEvent(long curSequence, T event) throws Exception {
        int type = event.getType();
        if (type == TYPE_REMOVE_SCHEDULE) {
            // 删除延时任务
            long taskId = event.getLongVal1();
            schedulerHelper.removeTask(taskId);
        }
    }

    // endregion

    /**
     * 实现{@link RingBuffer}的消费者，实现基本和Disruptor的{@code BatchEventProcessor}一致。
     * 但解决了两个问题：
     * 1. 生产者调用{@link ProducerBarrier#next()}时，如果消费者已关闭，则会死锁！为避免死锁不得不使用{@link ProducerBarrier#tryNext()}，但是那样的代码并不友好。
     * 2. 内存泄漏问题，使用{@code  BatchEventProcessor}在关闭时无法清理{@link RingBuffer}中的数据。
     */
    private class Worker implements Runnable {

        private final ConsumerBarrier barrier;
        private final Sequence sequence;

        private Worker(ConsumerBarrier barrier) {
            this.barrier = barrier;
            this.sequence = barrier.groupSequence();
        }

        @Override
        public void run() {
            outer:
            try {
                tickTime = System.nanoTime();
                if (!runningPromise.trySetComputing()) {
                    break outer;
                }
                onStart();

                advanceRunState(EventLoopState.ST_RUNNING);
                if (runningPromise.trySetResult(null)) {
                    loop();
                }
            } catch (Throwable e) {
                logger.error("thread exit due to exception!", e);
                if (!runningPromise.isDone()) { // 启动失败
                    runningPromise.trySetException(new StartFailedException("StartFailed", e));
                }
            } finally {
                if (runningPromise.isSucceeded()) {
                    advanceRunState(EventLoopState.ST_SHUTTING_DOWN);
                } else {
                    // 启动失败直接进入清理状态，丢弃所有提交的任务
                    advanceRunState(EventLoopState.ST_SHUTDOWN);
                }

                try {
                    // 清理ringBuffer中的数据
                    cleanBuffer();
                    schedulerHelper.clearTaskQueue();
                } finally {
                    removeFromGatingBarriers();
                    advanceRunState(EventLoopState.ST_SHUTDOWN);

                    // 退出前进行必要的清理，释放系统资源
                    try {
                        onShutdown();
                    } catch (Throwable e) {
                        logger.error("thread exit caught exception!", e);
                    } finally {
                        // 设置为终止状态
                        state = EventLoopState.ST_TERMINATED;
                        terminationPromise.trySetResult(null);
                    }
                }
            }
        }

        private void loop() {
            long nextSequence = sequence.getVolatile() + 1L;
            long availableSequence = -1; // 多生产者模型下不可频繁调用waitFor，查询之后本地切割为小批次
            while (state == EventLoopState.ST_RUNNING) {
                try {
                    tickTime = System.nanoTime();
                    schedulerHelper.update(tickTime, false);
                    if (isShutdown()) {
                        break; // 前面的任务可能未执行首次逻辑，后续的任务也不能执行
                    }
                    if (availableSequence < nextSequence
                            && (availableSequence = barrier.waitFor(nextSequence)) < nextSequence) {
                        updateModules(); // 等待超时
                        continue;
                    }

                    long batchEndSequence = Math.min(availableSequence, nextSequence + batchSize - 1);
                    long curSequence = runTaskBatch(nextSequence, batchEndSequence);
                    sequence.setRelease(curSequence);
                    // 无界队列尝试主动回收块
                    if (unboundedBuffer != null) {
                        unboundedBuffer.tryMoveHeadToNext(curSequence);
                    }
                    nextSequence = curSequence + 1;
                    if (nextSequence <= batchEndSequence) {
                        assert isShuttingDown();
                        break;
                    }
                    // 每处理一批事件，都尝试Update模块
                    updateModules();
                } catch (AlertException | InterruptedException e) {
                    if (isShuttingDown()) {
                        break;
                    }
                    logger.warn("receive a confusing signal", e);
                } catch (Throwable e) {
                    logger.error("bad waitStrategy impl", e);
                }
            }
        }

        /** @return curSequence */
        private long runTaskBatch(final long batchBeginSequence, final long batchEndSequence) {
            DataProvider<? extends T> dataProvider = DisruptorEventLoop.this.dataProvider;
            IEventLoopAgent<? super T> agent = DisruptorEventLoop.this.agent;
            for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
                T event = dataProvider.consumerGet(curSequence);
                int type = event.getType();
                try {
                    if (type == 0) {
                        Runnable runnable = (Runnable) event.getObj1();
                        runnable.run();
                    } else if (type > 0) {
                        // 用户事件
                        agent.onEvent(curSequence, event);
                    } else if (type != TYPE_INVALID) {
                        // 事件循环事件
                        onInternalEvent(curSequence, event);
                    } else {
                        // 生产者放弃序号
                        if (isShuttingDown()) {
                            return curSequence;
                        }
                        logger.warn("user published invalid event: " + event);
                    }
                } catch (Throwable t) {
                    logCause(t);
                    if (isShuttingDown()) { // 可能是中断或Alert，检查关闭信号
                        return curSequence;
                    }
                } finally {
                    event.clean();
                }
            }
            return batchEndSequence;
        }

        /**
         * 将自己从网关序列中删除;
         * 这是解决死锁问题的关键，如果不从gatingBarriers中移除，则生产者无法从{@link ProducerBarrier#next()}中退出，
         */
        private void removeFromGatingBarriers() {
            eventSequencer.removeGatingBarrier(barrier);
        }

        private void cleanBuffer() {
            final long startTimeMillis = System.currentTimeMillis();
            // 处理延迟任务，保证时序 - 外部清理
            tickTime = System.nanoTime();
            schedulerHelper.update(tickTime, true);
            schedulerHelper.clearTaskQueue(); // 执行一次清理，避免下面的内部事件执行

            // 在新的架构下，EventSequencer可能是无界队列，这种情况下我们采用笨方法来清理；
            // 从当前序列开始消费，一直消费到最新的cursor，然后将自己从gatingBarrier中删除 -- 此时不论有界无界，生产者都将醒来。
            long nullCount = 0;
            long taskCount = 0;
            long discardCount = 0;

            final DataProvider<? extends T> dataProvider = DisruptorEventLoop.this.dataProvider;
            final ProducerBarrier producerBarrier = eventSequencer.producerBarrier();
            final IEventLoopAgent<? super T> agent = DisruptorEventLoop.this.agent;
            while (true) {
                long nextSequence = sequence.getVolatile() + 1;
                if (nextSequence > producerBarrier.sequence()) {
                    break;
                }
                while (!producerBarrier.isPublished(nextSequence)) {
                    LockSupport.parkNanos(1000_000); // 等待发布-1ms
                }
                final T event = dataProvider.consumerGet(nextSequence);
                final int type = event.getType();
                try {
                    if (type == TYPE_INVALID) {
                        nullCount++;
                        continue;
                    }
                    taskCount++;
                    if (isShutdown()) {
                        discardCount++;
                        event.cleanAll();
                        continue;
                    }
                    if (type == 0) {
                        Runnable runnable = (Runnable) event.getObj1();
                        runnable.run();
                    } else if (type > 0) {
                        // 用户事件
                        agent.onEvent(nextSequence, event);
                    } else {
                        // 事件循环事件
                        onInternalEvent(nextSequence, event);
                    }
                } catch (Throwable t) {
                    logCause(t);
                } finally {
                    event.cleanAll();
                    sequence.setRelease(nextSequence);
                }
                // 由于生产者已停止，如果不moveHead，后续的查询性能将越来越差
                if (unboundedBuffer != null && !unboundedBuffer.inSameChunk(nextSequence - 1, nextSequence)) {
                    unboundedBuffer.tryMoveHeadToNext(nextSequence);
                }
            }
            logger.info("cleanBuffer success!  nullCount = {}, taskCount = {}, discardCount {}, cost timeMillis = {}",
                    nullCount, taskCount, discardCount, (System.currentTimeMillis() - startTimeMillis));
        }
    }

    private static final VarHandle STATE;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            STATE = l.findVarHandle(DisruptorEventLoop.class, "state", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }

        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }
}
