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
import cn.wjybxx.base.collection.DefaultIndexedPriorityQueue;
import cn.wjybxx.base.collection.IndexedPriorityQueue;
import cn.wjybxx.base.concurrent.CancelCodes;
import cn.wjybxx.disruptor.*;

import javax.annotation.Nonnull;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Collections;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.LockSupport;

/**
 * 基于Disruptor框架的事件循环。
 * 1.这个实现持有私有的RingBuffer，可以有最好的性能。
 * 2.可以通过{@link #nextSequence()}和{@link #publish(long)}发布特殊的事件。
 * <p>
 * 关于时序正确性：
 * 1.由于{@link #scheduledTaskQueue}的任务都是从{@link #dataProvider}中拉取出来的，因此都是先于{@link #dataProvider}中剩余的任务的。
 * 2.我们总是先取得一个时间快照，然后先执行{@link #scheduledTaskQueue}中的任务，再执行{@link #dataProvider}中的任务，因此满足优先级相同时，先提交的任务先执行的约定
 * -- 反之，如果不使用时间快照，就可能导致后提交的任务先满足触发时间。
 *
 * @author wjybxx
 * date 2023/4/10
 */
public class DisruptorEventLoop<T extends IAgentEvent> extends AbstractScheduledEventLoop {

    private static final int MIN_BATCH_SIZE = 64;
    private static final int MAX_BATCH_SIZE = 64 * 1024;
    private static final int BATCH_PUBLISH_THRESHOLD = 1024 - 1;

    private static final int HIGHER_PRIORITY_QUEUE_ID = 0;
    private static final int LOWER_PRIORITY_QUEUE_ID = 1;

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

    /** 周期性任务队列 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private final IndexedPriorityQueue<ScheduledPromiseTask<?>> scheduledTaskQueue;
    private final ScheduledHelper scheduledHelper;

    /** 任务拒绝策略 */
    private final RejectedExecutionHandler rejectedExecutionHandler;
    /** 内部代理 */
    private final EventLoopAgent<? super T> agent;
    /** 外部门面 */
    private final EventLoopModule mainModule;

    /** 批量执行任务的大小 */
    private final int batchSize;
    /** 消费事件后是否清理事件 -- 可清理意味着单消费者模型 */
    private final boolean cleanEventAfterConsumed;
    /** 退出时是否清理buffer -- 可清理意味着是消费链的末尾 */
    private final boolean cleanBufferOnExit;

    private final Thread thread;
    private final Worker worker;

    private final IPromise<Void> runningPromise = new Promise<>(this);
    private final IPromise<Void> terminationPromise = new Promise<>(this);
    // future 缓存
    private final IFuture<Void> runningFuture = runningPromise.asReadonly();
    private final IFuture<Void> terminationFuture = terminationPromise.asReadonly();

    public DisruptorEventLoop(EventLoopBuilder.DisruptorBuilder<T> builder) {
        super(builder.getParent());
        ThreadFactory threadFactory = Objects.requireNonNull(builder.getThreadFactory(), "threadFactory");

        this.tickTime = System.nanoTime();
        this.eventSequencer = Objects.requireNonNull(builder.getEventSequencer());
        this.dataProvider = eventSequencer.dataProvider();
        this.scheduledTaskQueue = new DefaultIndexedPriorityQueue<>(ScheduledPromiseTask::compareToExplicitly, 64);
        this.scheduledHelper = new ScheduledHelper();

        this.rejectedExecutionHandler = ObjectUtils.nullToDef(builder.getRejectedExecutionHandler(), RejectedExecutionHandlers.abort());
        this.agent = ObjectUtils.nullToDef(builder.getAgent(), EmptyAgent.getInstance());
        this.mainModule = builder.getMainModule();

        this.batchSize = MathCommon.clamp(builder.getBatchSize(), MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.cleanEventAfterConsumed = builder.isCleanEventAfterConsumed();
        this.cleanBufferOnExit = builder.isCleanBufferOnExit();

        // 缓存
        if (cleanBufferOnExit && dataProvider instanceof MpUnboundedBuffer<? extends T> unboundedBuffer) {
            this.unboundedBuffer = unboundedBuffer;
        } else {
            this.unboundedBuffer = null;
        }

        // worker只依赖生产者屏障
        ConsumerBarrier barrier = eventSequencer.newSingleConsumerBarrier(builder.getWaitStrategy());
        worker = new Worker(barrier);
        thread = Objects.requireNonNull(threadFactory.newThread(worker), "newThread");
        DefaultThreadFactory.checkUncaughtExceptionHandler(thread);

        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        eventSequencer.addGatingBarriers(barrier);

        // 完成绑定
        this.agent.inject(this);
    }

    /** EventLoop绑定的Agent（代理） */
    public EventLoopAgent<? super T> getAgent() {
        return agent;
    }

    @Override
    public EventLoopModule mainModule() {
        return mainModule;
    }

    /** 仅用于测试 */
    @VisibleForTesting
    public ConsumerBarrier getBarrier() {
        return worker.barrier;
    }

    /** EventLoop绑定的事件生成器 - 可用于发布事件 */
    public EventSequencer<? extends T> getEventSequencer() {
        return eventSequencer;
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
    public final IFuture<?> terminationFuture() {
        return terminationFuture;
    }

    @Override
    public final boolean awaitTermination(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
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

    @Override
    public final void wakeup() {
        if (!inEventLoop() && thread.isAlive()) {
            thread.interrupt();
            agent.wakeup();
        }
    }

    /**
     * 当前任务数
     * 注意：返回值是一个估算值！
     */
    @Beta
    public int taskCount() {
        long count = eventSequencer.producerBarrier().sequence() - worker.sequence.getVolatile();
        if (eventSequencer.capacity() != EventSequencer.UNBOUNDED_CAPACITY
                && count >= eventSequencer.capacity()) {
            return eventSequencer.capacity();
        }
        return Math.max(0, (int) count);
    }

    // endregion

    // region 任务提交

    @Override
    public void execute(Runnable command) {
        int options = command instanceof ITask task ? task.getOptions() : 0;
        execute(command, options);
    }

    @Override
    public void execute(Runnable command, int options) {
        Objects.requireNonNull(command, "command");
        if (isShuttingDown()) {
            rejectedExecutionHandler.rejected(command, this);
            return;
        }
        if (inEventLoop()) {
            // 当前线程调用，需要使用tryNext以避免死锁
            long sequence = eventSequencer.tryNext(1);
            if (sequence == -1) {
                rejectedExecutionHandler.rejected(command, this);
                return;
            }
            tryPublish(command, sequence, options);
        } else {
            // 其它线程调用，可能阻塞
            tryPublish(command, eventSequencer.next(1), options);
        }
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
     */
    private void tryPublish(@Nonnull Runnable task, long sequence, int options) {
        if (isShuttingDown()) {
            // 先发布sequence，避免拒绝逻辑可能产生的阻塞，不可以覆盖数据
            eventSequencer.publish(sequence);
            rejectedExecutionHandler.rejected(task, this);
        } else {
            // 不需要支持EventTranslator，用户可以通过申请序号和event发布
            T event = dataProvider.producerGet(sequence);
            event.setType(0);
            event.setObj1(task);
            event.setOptions(options);
            if (task instanceof ScheduledPromiseTask<?> futureTask) {
                futureTask.setId(sequence); // nice
                futureTask.registerCancellation();
            }
            eventSequencer.publish(sequence);

            if (!inEventLoop()) {
                // 确保线程已启动 -- ringBuffer私有的情况下才可以测试 sequence == 0
                if (sequence == 0) {
                    ensureThreadStarted();
                } else if (TaskOptions.isEnabled(options, TaskOptions.WAKEUP_THREAD)) {
                    wakeup();
                }
            }
        }
    }

    public final T getEvent(long sequence) {
        return dataProvider.producerGet(sequence);
    }

    /**
     * 开放的特殊接口
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre> {@code
     *      long sequence = eventLoop.nextSequence();
     *      try {
     *          RingBufferEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      } finally {
     *          eventLoop.publish(sequence)
     *      }
     * }</pre>
     *
     * @return 如果申请成功，则返回对应的sequence，否则返回 -1
     */
    @Beta
    public final long nextSequence() {
        return nextSequence(1);
    }

    @Beta
    public final void publish(long sequence) {
        eventSequencer.publish(sequence);
        if (sequence == 0 && !inEventLoop()) {
            ensureThreadStarted();
        }
    }

    /**
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre>{@code
     *   int n = 10;
     *   long hi = eventLoop.nextSequence(n);
     *   try {
     *      long lo = hi - (n - 1);
     *      for (long sequence = lo; sequence <= hi; sequence++) {
     *          RingBufferEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      }
     *   } finally {
     *      eventLoop.publish(lo, hi);
     *   }
     * }</pre>
     *
     * @param size 申请的空间大小
     * @return 如果申请成功，则返回申请空间的最大序号，否则返回-1
     */
    @Beta
    public final long nextSequence(int size) {
        if (isShuttingDown()) {
            return -1;
        }
        long sequence;
        if (inEventLoop()) {
            sequence = eventSequencer.tryNext(size);
            if (sequence == -1) {
                return -1;
            }
        } else {
            sequence = eventSequencer.next(size);
        }
        if (isShuttingDown()) {
            // sequence不一定有效了，申请的全部序号都要发布
            long lo = sequence - (size - 1);
            eventSequencer.publish(lo, sequence);
            return -1;
        }
        return sequence;
    }

    /**
     * @param lo inclusive
     * @param hi inclusive
     */
    @Beta
    public final void publish(long lo, long hi) {
        eventSequencer.producerBarrier().publish(lo, hi);
        if (lo == 0 && !inEventLoop()) {
            ensureThreadStarted();
        }
    }

    @Override
    protected IScheduledHelper helper() {
        return scheduledHelper;
    }

    private class ScheduledHelper implements IScheduledHelper {

        @Override
        public long tickTime() {
            return tickTime;
        }

        @Override
        public long normalize(long worldTime, TimeUnit timeUnit) {
            return timeUnit.toNanos(worldTime);
        }

        @Override
        public long denormalize(long localTime, TimeUnit timeUnit) {
            return timeUnit.convert(localTime, TimeUnit.NANOSECONDS);
        }

        @Override
        public void reschedule(ScheduledPromiseTask<?> futureTask) {
            assert inEventLoop();
            if (isShuttingDown()) {
                futureTask.trySetCancelled();
                onCompleted(futureTask);
            } else {
                scheduledTaskQueue.add(futureTask);
            }
        }

        @Override
        public void onCompleted(ScheduledPromiseTask<?> futureTask) {
            futureTask.clear();
        }

        @Override
        public void onCancelRequested(ScheduledPromiseTask<?> futureTask, int cancelCode) {
            futureTask.trySetCancelled(cancelCode);
            if (CancelCodes.isWithoutRemove(cancelCode)) {
                return;
            }
            if (inEventLoop()) {
                // 如果在事件循环线程内，有这些特殊情况：
                // 1.futureTask可能尚未被压入调度队列
                // 2.futureTask可能正在执行trigger方法 -- 这两种情况都导致不在调度队列
                futureTask.setNextTriggerTime(0);
                if (futureTask.collectionIndex(scheduledTaskQueue) >= 0) {
                    scheduledTaskQueue.priorityChanged(futureTask);
                }
            } else {
                // 如果在其它线程，有这些情况：
                // 1.如果EventLoop是有界队列，压任务可能导致阻塞
                // 2.如果futureTask可能会被池化，不能简单的异步删除 -- id验证都有风险，因为id也会变
                // 结论：要保证其它线程可取消，定时任务不能被池化
                execute(futureTask, futureTask.getOptions()); // run方法会检测取消信号，避免额外封装
            }
        }
    }

    // endregion

    // region 线程状态切换

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
                if (!runningPromise.trySetComputing()) {
                    break outer;
                }
                tickTime = System.nanoTime();
                agent.onStart();

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
                    if (cleanBufferOnExit) {
                        cleanBuffer();
                    }
                    scheduledTaskQueue.clearIgnoringIndexes();
                } finally {
                    removeFromGatingBarriers();
                    // 标记为已进入最终清理阶段
                    advanceRunState(EventLoopState.ST_SHUTDOWN);

                    // 退出前进行必要的清理，释放系统资源
                    try {
                        agent.onShutdown();
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
            long availableSequence = -1;
            while (state == EventLoopState.ST_RUNNING) {
                try {
                    tickTime = System.nanoTime();
                    processScheduledQueue(tickTime, false);

                    // 多生产者模型下不可频繁调用waitFor，会在查询可用sequence时产生巨大的开销，因此查询之后本地切割为小批次
                    if (availableSequence < nextSequence
                            && (availableSequence = barrier.waitFor(nextSequence)) < nextSequence) {
                        invokeAgentUpdate();
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

                    invokeAgentUpdate();
                } catch (TimeoutException e) {
                    // 优先先响应关闭，若未关闭，表用户主动退出等待，执行一次用户循环
                    if (isShuttingDown()) {
                        break;
                    }
                    tickTime = System.nanoTime();
                    processScheduledQueue(tickTime, false);
                    invokeAgentUpdate();
                } catch (AlertException | InterruptedException e) {
                    if (isShuttingDown()) {
                        break;
                    }
                    logger.warn("receive a confusing signal", e);
                } catch (Throwable e) {
                    // 不好的等待策略实现
                    logger.error("bad waitStrategy impl", e);
                }
            }
        }

        private void invokeAgentUpdate() {
            try {
                agent.update();
            } catch (Throwable t) {
                if (t instanceof VirtualMachineError) {
                    logger.error("agent.update caught exception", t);
                } else {
                    logger.warn("agent.update caught exception", t);
                }
            }
        }

        /**
         * 处理周期性任务，传入的限制只有在遇见低优先级任务的时候才生效，因此限制为0则表示遇见低优先级任务立即结束
         * (为避免时序错误，处理周期性任务期间不响应关闭，不容易安全实现)
         *
         * @param shuttingDownMode 是否是退出模式
         */
        private void processScheduledQueue(long tickTime, boolean shuttingDownMode) {
            final IndexedPriorityQueue<ScheduledPromiseTask<?>> taskQueue = scheduledTaskQueue;
            ScheduledPromiseTask<?> queueTask;
            while ((queueTask = taskQueue.peek()) != null) {
                // 优先级最高的任务不需要执行，那么后面的也不需要执行
                if (tickTime < queueTask.getNextTriggerTime()) {
                    return;
                }

                taskQueue.poll();
                if (shuttingDownMode) {
                    // 关闭模式下，不再重复执行任务
                    if (queueTask.isTriggered() || queueTask.trigger(tickTime)) {
                        queueTask.trySetCancelled();
                        scheduledHelper.onCompleted(queueTask);
                    }
                } else {
                    // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 需等同Reschedule
                    if (queueTask.trigger(tickTime)) {
                        if (isShuttingDown()) {
                            queueTask.trySetCancelled();
                            scheduledHelper.onCompleted(queueTask);
                        } else {
                            taskQueue.offer(queueTask);
                            continue;
                        }
                    } else {
                        scheduledHelper.onCompleted(queueTask);
                    }
                }

                // 响应关闭
                if (isShutdown()) {
                    return;
                }
            }
        }

        /** @return curSequence */
        private long runTaskBatch(final long batchBeginSequence, final long batchEndSequence) {
            DataProvider<? extends T> dataProvider = DisruptorEventLoop.this.dataProvider;
            EventLoopAgent<? super T> agent = DisruptorEventLoop.this.agent;
            boolean cleanEventAfterConsumed = DisruptorEventLoop.this.cleanEventAfterConsumed;

            for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
                T event = dataProvider.consumerGet(curSequence);
                try {
                    if (event.getType() == 0) {
                        Runnable runnable = (Runnable) event.getObj1();
                        runnable.run();
                    } else if (event.getType() > 0) {
                        agent.onEvent(curSequence, event);
                    } else {
                        if (isShuttingDown()) { // 生产者在观察到关闭时发布了不连续的数据
                            return curSequence;
                        }
                        logger.warn("user published invalid event: " + event); // 用户发布了非法数据
                    }
                } catch (Throwable t) {
                    logCause(t);
                    if (isShuttingDown()) { // 可能是中断或Alert，检查关闭信号
                        return curSequence;
                    }
                } finally {
                    if (cleanEventAfterConsumed) {
                        event.clean();
                    }
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

            // 处理延迟任务
            tickTime = System.nanoTime();
            processScheduledQueue(tickTime, true);
            scheduledTaskQueue.clearIgnoringIndexes();

            // 在新的架构下，EventSequencer可能是无界队列，这种情况下我们采用笨方法来清理；
            // 从当前序列开始消费，一直消费到最新的cursor，然后将自己从gatingBarrier中删除 -- 此时不论有界无界，生产者都将醒来。
            long nullCount = 0;
            long taskCount = 0;
            long discardCount = 0;

            final EventLoopAgent<? super T> agent = DisruptorEventLoop.this.agent;
            final DataProvider<? extends T> dataProvider = DisruptorEventLoop.this.dataProvider;
            final ProducerBarrier producerBarrier = eventSequencer.producerBarrier();
            final Sequence sequence = this.sequence;
            while (true) {
                long nextSequence = sequence.getVolatile() + 1;
                if (nextSequence > producerBarrier.sequence()) {
                    break;
                }
                while (!producerBarrier.isPublished(nextSequence)) {
                    Thread.onSpinWait(); // 等待发布
                }
                final T event = dataProvider.consumerGet(nextSequence);
                try {
                    if (event.getType() < 0) { // 生产者在观察到关闭时发布了不连续的数据
                        nullCount++;
                        continue;
                    }
                    taskCount++;
                    if (isShutdown()) { // 如果已进入shutdown阶段，则直接丢弃任务
                        discardCount++;
                        event.cleanAll();
                        continue;
                    }
                    if (event.getType() == 0) {
                        Runnable runnable = (Runnable) event.getObj1();
                        runnable.run();
                    } else {
                        agent.onEvent(nextSequence, event);
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
