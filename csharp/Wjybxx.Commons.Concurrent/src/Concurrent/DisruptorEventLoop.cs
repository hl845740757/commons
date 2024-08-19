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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Logger;
using Wjybxx.Disruptor;

#pragma warning disable CS0169

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 基于Disruptor框架的事件循环。
/// 1.这个实现持有私有的RingBuffer，可以有最好的性能。
/// 2.可以通过<see cref="NextSequence()"/>和<see cref="Publish(long)"/>发布特殊的事件。
/// 
/// 关于时序正确性：
/// 1.由于<see cref="scheduledTaskQueue"/>中的延时任务都是从<see cref="dataProvider"/>中拉取出来的，因此都是先于<see cref="dataProvider"/>中剩余的任务的。
/// 2.我们总是先取得一个时间快照，然后先执行<see cref="scheduledTaskQueue"/>中的任务，再执行<see cref="dataProvider"/>中的任务，
/// 因此满足优先级相同时，先提交的任务先执行的约定；反之，如果不使用时间快照，就可能导致后提交的任务先满足触发时间。
/// </summary>
public class DisruptorEventLoop<T> : AbstractScheduledEventLoop where T : IAgentEvent
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(DisruptorEventLoop<T>));

    private const int ST_UNSTARTED = (int)EventLoopState.Unstarted;
    private const int ST_STARTING = (int)EventLoopState.Starting;
    private const int ST_RUNNING = (int)EventLoopState.Running;
    private const int ST_SHUTTING_DOWN = (int)EventLoopState.ShuttingDown;
    private const int ST_SHUTDOWN = (int)EventLoopState.Shutdown;
    private const int ST_TERMINATED = (int)EventLoopState.Terminated;

    private const int MIN_BATCH_SIZE = 64;
    private const int MAX_BATCH_SIZE = 64 * 1024;

    // 填充开始 - 字段定义顺序不要随意调整
    private long p1, p2, p3, p4, p5, p6, p7;
    /** 线程本地时间 -- 时间的更新频率极高，进行缓存行填充隔离；使用volatile读写 */
    private long _tickTime;
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    /** 线程状态 -- 下面的final字段充当缓存行填充 */
    private volatile int state = ST_UNSTARTED;

    /** 事件队列 */
    private readonly EventSequencer<T> eventSequencer;
    /** 缓存值 -- 减少转发 */
    private readonly DataProvider<T> dataProvider;
    /** 缓存值 -- 减少运行时测试 */
    private readonly MpUnboundedBuffer<T>? unboundedBuffer;

    /** 周期性任务队列 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private readonly IndexedPriorityQueue<IScheduledFutureTask> scheduledTaskQueue;
    private readonly ScheduledHelper scheduledHelper;

    /** 任务拒绝策略 */
    private readonly RejectedExecutionHandler rejectedExecutionHandler;
    /** 内部代理 */
    private readonly IEventLoopAgent<T> agent;
    /** 外部门面 */
    private readonly IEventLoopModule? mainModule;

    /** 批量执行任务的大小 */
    private readonly int batchSize;
    /** 消费事件后是否清理事件 -- 可清理意味着单消费者模型 */
    private readonly bool cleanEventAfterConsumed;
    /** 退出时是否清理buffer -- 可清理意味着是消费链的末尾 */
    private readonly bool cleanBufferOnExit;

    private readonly Thread thread;
    /** 消费者屏障 -- 由于C#的委托不是接口，因此委托依赖的数据存储在EventLoop上 */
    private readonly ConsumerBarrier barrier;
    /** 消费进度 */
    private readonly Sequence sequence;

    /** 进入运行状态的promise */
    private readonly IPromise<int> runningPromise;
    /** 进入终止状态的promise */
    private readonly IPromise<int> terminationPromise;
    /** 只读future - 缓存字段 */
    private readonly IFuture runningFuture;
    private readonly IFuture terminationFuture;

    public DisruptorEventLoop(DisruptorEventLoopBuilder<T> builder) : base(builder.Parent) {
        ThreadFactory threadFactory = builder.ThreadFactory ?? throw new ArgumentException("builder.ThreadFactory");

        this._tickTime = ObjectUtil.SystemTicks();
        this.eventSequencer = builder.EventSequencer ?? throw new ArgumentException("builder.EventSequencer");
        this.dataProvider = eventSequencer.DataProvider;
        this.scheduledTaskQueue = new IndexedPriorityQueue<IScheduledFutureTask>(new ScheduledTaskComparator(), 64);
        this.scheduledHelper = new ScheduledHelper(this);

        this.rejectedExecutionHandler = builder.RejectedExecutionHandler ?? RejectedExecutionHandlers.ABORT;
        this.agent = builder.Agent ?? EmptyAgent<T>.Inst;
        this.mainModule = builder.MainModule;

        this.batchSize = Math.Clamp(builder.BatchSize, MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.cleanEventAfterConsumed = builder.CleanEventAfterConsumed;
        this.cleanBufferOnExit = builder.CleanBufferOnExit;
        if (cleanBufferOnExit && dataProvider is MpUnboundedBuffer<T> unboundedBuffer) {
            this.unboundedBuffer = unboundedBuffer;
        } else {
            this.unboundedBuffer = null;
        }

        runningPromise = new Promise<int>(this);
        terminationPromise = new Promise<int>(this);
        runningFuture = runningPromise.AsReadonly();
        terminationFuture = terminationPromise.AsReadonly();

        // worker只依赖生产者屏障
        barrier = eventSequencer.NewSingleConsumerBarrier(builder.WaitStrategy);
        sequence = new Sequence();
        thread = threadFactory.NewThread(MainLoopEntry);
        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        eventSequencer.AddGatingBarriers(barrier);

        // 完成绑定
        this.agent.Inject(this);
    }

    public IEventLoopAgent<T> Agent => agent;

    public override IEventLoopModule? MainModule => mainModule;

    /** 仅用于测试 */
    [VisibleForTesting]
    public ConsumerBarrier GetBarrier() {
        return barrier;
    }

    /** EventLoop绑定的事件生成器 - 可用于发布事件 */
    public EventSequencer<T> GetEventSequencer() {
        return eventSequencer;
    }

    #region 状态查询

    public override EventLoopState State => (EventLoopState)state;
    public override bool IsRunning => state == ST_RUNNING;
    public override bool IsShuttingDown => state >= ST_SHUTTING_DOWN;
    public override bool IsShutdown => state >= ST_SHUTDOWN;
    public override bool IsTerminated => state == ST_TERMINATED;

    public override IFuture RunningFuture => runningFuture;
    public override IFuture TerminationFuture => terminationFuture;

    public override bool InEventLoop() {
        return this.thread == Thread.CurrentThread;
    }

    public override bool InEventLoop(Thread thread) {
        return this.thread == thread;
    }

    public override void Wakeup() {
        if (!InEventLoop() && thread.IsAlive) {
            thread.Interrupt();
            agent.Wakeup();
        }
    }

    /**
     * 当前任务数
     * 注意：返回值是一个估算值！
     */
    [Beta]
    public int TaskCount() {
        long count = eventSequencer.ProducerBarrier.Sequence() - sequence.GetVolatile();
        if (eventSequencer.Capacity > 0 && count >= eventSequencer.Capacity) {
            return eventSequencer.Capacity;
        }
        return Math.Max(0, (int)count);
    }

    #endregion

    #region 任务提交

    public override void Execute(ITask task) {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (IsShuttingDown) {
            rejectedExecutionHandler.Rejected(task, this);
            return;
        }
        if (InEventLoop()) {
            // 当前线程调用，需要使用tryNext以避免死锁
            long? sequence = eventSequencer.TryNext(1);
            if (sequence == null) {
                rejectedExecutionHandler.Rejected(task, this);
                return;
            }
            TryPublish(task, sequence.Value, task.Options);
        } else {
            // 其它线程调用，可能阻塞
            TryPublish(task, eventSequencer.Next(1), task.Options);
        }
    }

    public override void Execute(Action command, int options = 0) {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (IsShuttingDown) {
            rejectedExecutionHandler.Rejected(Executors.ToTask(command, options), this);
            return;
        }
        if (InEventLoop()) {
            // 当前线程调用，需要使用tryNext以避免死锁
            long? sequence = eventSequencer.TryNext(1);
            if (sequence == null) {
                rejectedExecutionHandler.Rejected(Executors.ToTask(command, options), this);
                return;
            }
            TryPublish(command, sequence.Value, options);
        } else {
            // 其它线程调用，可能阻塞
            TryPublish(command, eventSequencer.Next(1), options);
        }
    }

    /// <summary>
    /// Q: 如何保证算法的安全性的？
    /// A: 我们只需要保证申请到的sequence是有效的，且发布任务在{@link Worker#removeFromGatingBarriers()}之前即可。
    /// 因为{@link Worker#removeFromGatingBarriers()}之前申请到的sequence一定是有效的，它考虑了EventLoop的消费进度。
    /// 
    /// 关键时序：
    /// 1. {@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前。
    /// 2. {@link Worker#cleanBuffer()}必须等待在这之前申请到的sequence发布。
    /// 3. {@link Worker#cleanBuffer()}在所有生产者发布数据之后才{@link Worker#removeFromGatingBarriers()}
    /// 
    /// 因此，{@link Worker#cleanBuffer()}之前申请到的sequence是有效的；
    /// 又因为{@link #isShuttingDown()}为true一定在{@link Worker#cleanBuffer()}之前，
    /// 因此，如果sequence是在{@link #isShuttingDown()}为true之前申请到的，那么sequence一定是有效的，否则可能有效，也可能无效。
    /// </summary>
    /// <param name="task"></param>
    /// <param name="sequence"></param>
    /// <param name="options"></param>
    private void TryPublish(object task, long sequence, int options) {
        if (IsShuttingDown) {
            // 先发布sequence，避免拒绝逻辑可能产生的阻塞，不可以覆盖数据
            eventSequencer.Publish(sequence);
            Reject(task, options);
        } else {
            // 不需要支持EventTranslator，用户可以通过申请序号和event发布
            ref T eventObj = ref dataProvider.ProducerGetRef(sequence);
            eventObj.Type = 0;
            eventObj.Obj1 = task;
            eventObj.Options = options;
            if (task is IScheduledFutureTask futureTask) {
                futureTask.Id = sequence; // nice
                futureTask.RegisterCancellation();
            }
            eventSequencer.Publish(sequence);

            if (!InEventLoop()) {
                // 确保线程已启动 -- ringBuffer私有的情况下才可以测试 sequence == 0
                if (sequence == 0) {
                    EnsureThreadStarted();
                } else if (TaskOptions.IsEnabled(options, TaskOptions.WAKEUP_THREAD)) {
                    Wakeup();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reject(object objTask, int options) {
        if (objTask is Action action) {
            rejectedExecutionHandler.Rejected(Executors.ToTask(action, options), this);
        } else {
            ITask task = (ITask)objTask;
            rejectedExecutionHandler.Rejected(task, this);
        }
    }

    /** 适用Class类型事件 */
    public T GetEvent(long sequence) {
        return dataProvider.ProducerGet(sequence);
    }

    /** 适用结构体类型事件 */
    public ref T GetEventRef(long sequence) {
        return ref dataProvider.ProducerGetRef(sequence);
    }

    /** 适用结构体类型事件 */
    public void SetEvent(long sequence, T eventObj) {
        dataProvider.ProducerSet(sequence, eventObj);
    }

    /**
     * 开放的特殊接口
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为null时必须检查
     * <code>
     *      long sequence = eventLoop.NextSequence();
     *      try {
     *          RingBufferEvent event = eventLoop.GetEvent(sequence);
     *          // Do work.
     *      } finally {
     *          eventLoop.Publish(sequence)
     *      }
     * </code>
     *
     * @return 如果申请成功，则返回对应的sequence，否则返回null
     */
    [Beta]
    public long? NextSequence() {
        return NextSequence(1);
    }

    [Beta]
    public void Publish(long sequence) {
        eventSequencer.ProducerBarrier.Publish(sequence);
        if (sequence == 0 && !InEventLoop()) {
            EnsureThreadStarted();
        }
    }

    /**
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为null时必须检查
     * <code>
     *   int n = 10;
     *   long hi = eventLoop.NextSequence(n);
     *   try {
     *      long lo = hi - (n - 1);
     *      for (long sequence = lo; sequence &lt;= hi; sequence++) {
     *          RingBufferEvent event = eventLoop.GetEvent(sequence);
     *          // Do work.
     *      }
     *   } finally {
     *      eventLoop.Publish(lo, hi);
     *   }
     * </code>
     *
     * @param size 申请的空间大小
     * @return 如果申请成功，则返回申请空间的最大序号，否则返回null
     */
    [Beta]
    public long? NextSequence(int size) {
        if (IsShuttingDown) {
            return null;
        }
        long? sequence;
        if (InEventLoop()) {
            sequence = eventSequencer.TryNext(size);
            if (sequence == null) {
                return null;
            }
        } else {
            sequence = eventSequencer.Next(size);
        }
        if (IsShuttingDown) {
            // sequence不一定有效了，申请的全部序号都要发布
            long lo = sequence.Value - (size - 1);
            eventSequencer.Publish(lo, sequence.Value);
            return null;
        }
        return sequence;
    }

    /**
     * @param lo inclusive
     * @param hi inclusive
     */
    [Beta]
    public void Publish(long lo, long hi) {
        eventSequencer.ProducerBarrier.Publish(lo, hi);
        if (lo == 0 && !InEventLoop()) {
            EnsureThreadStarted();
        }
    }

    protected override IScheduledHelper Helper => scheduledHelper;

    private class ScheduledHelper : IScheduledHelper
    {
        private readonly DisruptorEventLoop<T> _eventLoop;

        public ScheduledHelper(DisruptorEventLoop<T> eventLoop) {
            _eventLoop = eventLoop;
        }

        public long TickTime => _eventLoop.TickTime;

        public long Normalize(long worldTime, TimeSpan timeUnit) {
            return worldTime * timeUnit.Ticks;
        }

        public long Denormalize(long localTime, TimeSpan timeUnit) {
            return localTime / timeUnit.Ticks;
        }

        public void Reschedule(IScheduledFutureTask futureTask) {
            Debug.Assert(_eventLoop.InEventLoop());
            if (_eventLoop.IsShuttingDown) {
                futureTask.TrySetCancelled();
                OnCompleted(futureTask);
            } else {
                _eventLoop.scheduledTaskQueue.Enqueue(futureTask);
            }
        }

        public void OnCompleted(IScheduledFutureTask futureTask) {
            futureTask.Clear();
        }

        public void OnCancelRequested(IScheduledFutureTask futureTask, int cancelCode) {
            futureTask.TrySetCancelled(cancelCode);
            if (CancelCodes.IsWithoutRemove(cancelCode)) {
                return; // 用户选择不立即从队列删除
            }
            if (_eventLoop.InEventLoop()) {
                // 如果在事件循环线程内，有这些特殊情况：
                // 1.futureTask可能尚未被压入调度队列
                // 2.futureTask可能正在执行trigger方法 -- 这两种情况都导致不在调度队列
                futureTask.NextTriggerTime = 0;
                if (futureTask.CollectionIndex(_eventLoop.scheduledTaskQueue) >= 0) {
                    _eventLoop.scheduledTaskQueue.PriorityChanged(futureTask);
                }
            } else {
                // 如果在其它线程，有这些情况：
                // 1.如果EventLoop是有界队列，压任务可能导致阻塞
                // 2.如果futureTask可能会被池化，不能简单的异步删除 -- id验证都有风险，因为id也会变
                // 结论：要保证其它线程可取消，定时任务不能被池化
                _eventLoop.Execute(futureTask); // run方法会检测取消信号，避免额外封装
            }
        }
    }

    protected long TickTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _tickTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long UpdateTickTime() {
        long tickTime = ObjectUtil.SystemTicks();
        Volatile.Write(ref _tickTime, tickTime);
        return tickTime;
    }

    #endregion

    #region 线程状态切换

    public override IFuture Start() {
        EnsureThreadStarted();
        return runningFuture;
    }

    public override void Shutdown() {
        if (!runningPromise.IsCompleted) { // 尚未启动成功就关闭
            runningPromise.TrySetCancelled(CancelCodes.REASON_SHUTDOWN);
        }
        int expectedState = state;
        for (;;) {
            if (expectedState >= ST_SHUTTING_DOWN) {
                return;
            }
            int realState = Interlocked.CompareExchange(ref state, ST_SHUTTING_DOWN, expectedState);
            if (realState == expectedState) {
                EnsureThreadTerminable(expectedState);
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    public override List<ITask> ShutdownNow() {
        Shutdown();
        AdvanceRunState(ST_SHUTDOWN);
        // 这里不能操作ringBuffer中的数据，不能打破[多生产者单消费者]的架构
        return new List<ITask>(0);
    }

    private void EnsureThreadStarted() {
        if (state == ST_UNSTARTED
            && Interlocked.CompareExchange(ref state, ST_STARTING, ST_UNSTARTED) == ST_UNSTARTED) {
            thread.Start();
        }
    }

    /// <summary>
    /// 确保线程可关闭
    /// </summary>
    /// <param name="oldState">切换为关闭状态之前的状态</param>
    private void EnsureThreadTerminable(int oldState) {
        if (oldState == ST_UNSTARTED) {
            // TODO 是否需要启动线程，进行更彻底的清理？
            state = ST_TERMINATED;
            RemoveFromGatingBarriers(); // 防死锁

            runningPromise.TrySetException(new StartFailedException("Stillborn"));
            terminationPromise.TrySetResult(0);
        } else {
            // 等待策略是根据alert信号判断EventLoop是否已开始关闭的，因此即使inEventLoop也需要alert，否则可能丢失信号，在waitFor处无法停止
            barrier.Alert();
            // 唤醒线程 - 如果线程可能阻塞在其它地方
            Wakeup();
        }
    }

    /// <summary>
    /// 如果事件循环尚未到达指定状态，则更新为指定状态
    /// </summary>
    /// <param name="targetState">要到达的状态</param>
    private void AdvanceRunState(int targetState) {
        int expectedState = state;
        for (;;) {
            if (expectedState >= targetState) {
                return;
            }
            int realState = Interlocked.CompareExchange(ref state, targetState, expectedState);
            if (realState >= targetState) { // == 表示CAS成功， > 表示已进入目标状态
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    #endregion

    #region mainloop

    /// <summary>
    /// 主循环入口
    /// 由于C#的委托与Java有所差异，不能直接实现委托，因此我们将数据存储在EventLoop上，数据。
    /// </summary>
    private void MainLoopEntry() {
        // 设置同步上下文，使得EventLoop创建的Task的下游任务默认继续在EventLoop上执行
        SynchronizationContext.SetSynchronizationContext(AsSyncContext());
        try {
            if (!runningPromise.TrySetComputing()) {
                goto loopEnd;
            }
            UpdateTickTime();
            agent.OnStart();

            AdvanceRunState(ST_RUNNING);
            if (runningPromise.TrySetResult(0)) {
                Loop();
            }

            loopEnd:
            {
            }
        }
        catch (Exception e) {
            logger.Error(e, "thread exit due to exception!");
            if (!runningPromise.IsCompleted) { // 启动失败
                runningPromise.TrySetException(new StartFailedException("StartFailed", e));
            }
        }
        finally {
            if (runningPromise.IsSucceeded) {
                AdvanceRunState(ST_SHUTTING_DOWN);
            } else {
                // 启动失败直接进入清理状态，丢弃所有提交的任务
                AdvanceRunState(ST_SHUTDOWN);
            }

            try {
                // 清理ringBuffer中的数据
                if (cleanBufferOnExit) {
                    CleanBuffer();
                }
                scheduledTaskQueue.ClearIgnoringIndexes();
            }
            finally {
                RemoveFromGatingBarriers();
                // 标记为已进入最终清理阶段
                AdvanceRunState(ST_SHUTDOWN);

                // 退出前进行必要的清理，释放系统资源
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    agent.OnShutdown();
                }
                catch (Exception e) {
                    logger.Error(e, "thread exit caught exception!");
                }
                finally {
                    // 设置为终止状态
                    state = ST_TERMINATED;
                    terminationPromise.TrySetResult(0);
                }
            }
        }
    }

    private void Loop() {
        long nextSequence = sequence.GetVolatile() + 1L;
        long availableSequence = -1;
        while (state == ST_RUNNING) {
            try {
                long tickTime = UpdateTickTime();
                ProcessScheduledQueue(tickTime, false);

                // 多生产者模型下不可频繁调用waitFor，会在查询可用sequence时产生巨大的开销，因此查询之后本地切割为小批次
                if (availableSequence < nextSequence
                    && (availableSequence = barrier.WaitFor(nextSequence)) < nextSequence) {
                    InvokeAgentUpdate();
                    continue;
                }

                long batchEndSequence = Math.Min(availableSequence, nextSequence + batchSize - 1);
                long curSequence = RunTaskBatch(nextSequence, batchEndSequence);
                sequence.SetRelease(curSequence);
                // 无界队列尝试主动回收块
                if (unboundedBuffer != null) {
                    unboundedBuffer.TryMoveHeadToNext(curSequence);
                }
                nextSequence = curSequence + 1;
                if (nextSequence <= batchEndSequence) {
                    Debug.Assert(IsShuttingDown);
                    break;
                }

                InvokeAgentUpdate();
            }
            catch (TimeoutException) {
                // 优先先响应关闭，若未关闭，表用户主动退出等待，执行一次用户循环
                if (IsShuttingDown) {
                    break;
                }
                long tickTime = UpdateTickTime();
                ProcessScheduledQueue(tickTime, false);
                InvokeAgentUpdate();
            }
            catch (ThreadInterruptedException e) {
                if (IsShuttingDown) {
                    break;
                }
                logger.Warn(e, "receive a confusing signal");
            }
            catch (AlertException e) {
                if (IsShuttingDown) {
                    break;
                }
                logger.Warn(e, "receive a confusing signal");
            }
            catch (Exception e) {
                // 不好的等待策略实现
                logger.Error(e, "bad waitStrategy impl");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvokeAgentUpdate() {
        try {
            agent.Update();
        }
        catch (Exception ex) {
            logger.Warn(ex, "agent.update caught exception");
        }
    }

    /// <summary>
    /// 处理周期性任务，传入的限制只有在遇见低优先级任务的时候才生效，因此限制为0则表示遇见低优先级任务立即结束
    /// (为避免时序错误，处理周期性任务期间不响应关闭，不容易安全实现)
    /// </summary>
    /// <param name="tickTime">当前时间</param>
    /// <param name="shuttingDownMode">是否是退出模式</param>
    private void ProcessScheduledQueue(long tickTime, bool shuttingDownMode) {
        IndexedPriorityQueue<IScheduledFutureTask> taskQueue = scheduledTaskQueue;
        IScheduledFutureTask queueTask;
        while (taskQueue.TryPeekHead(out queueTask)) {
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (tickTime < queueTask.NextTriggerTime) {
                return;
            }

            taskQueue.Dequeue();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (queueTask.IsTriggered || queueTask.Trigger(tickTime)) {
                    queueTask.TrySetCancelled();
                    scheduledHelper.OnCompleted(queueTask);
                }
            } else {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 需等同Reschedule
                if (queueTask.Trigger(tickTime)) {
                    if (IsShuttingDown) {
                        queueTask.TrySetCancelled();
                        scheduledHelper.OnCompleted(queueTask);
                    } else {
                        taskQueue.Enqueue(queueTask);
                        continue;
                    }
                } else {
                    scheduledHelper.OnCompleted(queueTask);
                }
            }

            // 响应关闭
            if (IsShutdown) {
                return;
            }
        }
    }

    /// <summary>
    /// 批量处理任务
    /// </summary>
    /// <param name="batchBeginSequence">批处理的第一个序号</param>
    /// <param name="batchEndSequence">批处理的最后一个序号</param>
    /// <returns>curSequence</returns>
    private long RunTaskBatch(long batchBeginSequence, long batchEndSequence) {
        DataProvider<T> dataProvider = this.dataProvider;
        IEventLoopAgent<T> agent = this.agent;
        bool cleanEventAfterConsumed = this.cleanEventAfterConsumed;

        for (long curSequence = batchBeginSequence; curSequence <= batchEndSequence; curSequence++) {
            ref T eventObj = ref dataProvider.ConsumerGetRef(curSequence);
            try {
                if (eventObj.Type == 0) {
                    if (eventObj.Obj1 is Action action) {
                        action();
                    } else {
                        ITask task = (ITask)eventObj.Obj1;
                        task.Run();
                    }
                } else if (eventObj.Type > 0) {
                    agent.OnEvent(curSequence, ref eventObj);
                } else {
                    if (IsShuttingDown) { // 生产者在观察到关闭时发布了不连续的数据
                        return curSequence;
                    }
                    logger.Warn("user published invalid event: " + eventObj); // 用户发布了非法数据
                }
            }
            catch (Exception ex) {
                logger.Info(ex, "execute task caught exception");
                if (IsShuttingDown) { // 可能是中断或Alert，检查关闭信号
                    return curSequence;
                }
            }
            finally {
                if (cleanEventAfterConsumed) {
                    eventObj.Clean();
                }
            }
        }
        return batchEndSequence;
    }

    /// <summary>
    /// 将自己从网关序列中删除
    /// 这是解决死锁问题的关键，如果不从gatingBarriers中移除，则生产者无法从{@link ProducerBarrier#next()}中退出，
    /// </summary>
    private void RemoveFromGatingBarriers() {
        eventSequencer.RemoveGatingBarrier(barrier);
    }

    /// <summary>
    /// 清理缓冲区
    /// </summary>
    private void CleanBuffer() {
        long startTimeMillis = ObjectUtil.SystemTickMillis();

        // 处理延迟任务
        long tickTime = UpdateTickTime();
        ProcessScheduledQueue(tickTime, true);
        scheduledTaskQueue.ClearIgnoringIndexes();

        // 在新的架构下，EventSequencer可能是无界队列，这种情况下我们采用笨方法来清理；
        // 从当前序列开始消费，一直消费到最新的cursor，然后将自己从gatingBarrier中删除 -- 此时不论有界无界，生产者都将醒来。
        long nullCount = 0;
        long taskCount = 0;
        long discardCount = 0;

        DataProvider<T> dataProvider = this.dataProvider;
        IEventLoopAgent<T> agent = this.agent;
        ProducerBarrier producerBarrier = eventSequencer.ProducerBarrier;
        Sequence sequence = this.sequence;
        while (true) {
            long nextSequence = sequence.GetVolatile() + 1;
            if (nextSequence > producerBarrier.Sequence()) {
                break;
            }
            while (!producerBarrier.IsPublished(nextSequence)) {
                Thread.SpinWait(10); // 等待发布
            }
            ref T eventObj = ref dataProvider.ConsumerGetRef(nextSequence);
            try {
                if (eventObj.Type < 0) { // 生产者在观察到关闭时发布了不连续的数据
                    nullCount++;
                    continue;
                }
                taskCount++;
                if (IsShutdown) { // 如果已进入shutdown阶段，则直接丢弃任务
                    discardCount++;
                    eventObj.CleanAll();
                    continue;
                }
                if (eventObj.Type == 0) {
                    if (eventObj.Obj1 is Action action) {
                        action();
                    } else {
                        ITask task = (ITask)eventObj.Obj1;
                        task.Run();
                    }
                } else {
                    agent.OnEvent(nextSequence, ref eventObj);
                }
            }
            catch (Exception ex) {
                logger.Info(ex, "execute task caught exception");
            }
            finally {
                eventObj.CleanAll();
                sequence.SetRelease(nextSequence);
            }
        }
        // 清理内存
        if (unboundedBuffer != null) {
            unboundedBuffer.TryMoveHeadToNext(sequence.GetVolatile());
        }
        long costTime = ObjectUtil.SystemTickMillis() - startTimeMillis;
        logger.Info(
            $"cleanBuffer success!  nullCount = {nullCount}, taskCount = {taskCount}, discardCount {discardCount}, cost timeMillis = {costTime}");
    }

    #endregion
}
}