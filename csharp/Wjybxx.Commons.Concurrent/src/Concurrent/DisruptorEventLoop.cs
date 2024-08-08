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
/// 2.可以通过{@link #nextSequence()}和{@link #publish(long)}发布特殊的事件。
/// 3.也可以让Task实现{@link EventTranslator}，从而拷贝数据到既有事件对象上。
/// 
/// 关于时序正确性：
/// 1.由于{@link #scheduledTaskQueue}的任务都是从{@link RingBuffer}中拉取出来的，因此都是先于{@link RingBuffer}中剩余的任务的。
/// 2.我们总是先取得一个时间快照，然后先执行{@link #scheduledTaskQueue}中的任务，再执行{@link RingBuffer}中的任务，因此满足优先级相同时，先提交的任务先执行的约定
/// -- 反之，如果不使用时间快照，就可能导致后提交的任务先满足触发时间。
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
    private long p11, p12, p13, p14, p15, p16, p17;

    /** 线程状态 -- 变化不频繁，不缓存行填充 */
    private volatile int state = (int)EventLoopState.Unstarted;

    /** 事件队列 */
    private readonly EventSequencer<T> eventSequencer;
    /** 周期性任务队列 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private readonly IndexedPriorityQueue<IScheduledFutureTask> scheduledTaskQueue;
    /** 批量执行任务的大小 */
    private readonly int batchSize;
    /** 任务拒绝策略 */
    private readonly RejectedExecutionHandler rejectedExecutionHandler;
    /** 内部代理 */
    private readonly IEventLoopAgent<T> agent;
    /** 外部门面 */
    private readonly IEventLoopModule? mainModule;

    /** 退出时是否清理buffer -- 可清理意味着是消费链的末尾 */
    private readonly bool cleanBufferOnExit;
    /** 缓存值 -- 减少运行时测试 */
    private readonly MpUnboundedEventSequencer<T>? mpUnboundedEventSequencer;

    private readonly Thread thread;
    private readonly Worker worker;

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
        this.scheduledTaskQueue = new IndexedPriorityQueue<IScheduledFutureTask>(new ScheduledTaskComparator(), 64);

        this.batchSize = Math.Clamp(builder.BatchSize, MIN_BATCH_SIZE, MAX_BATCH_SIZE);
        this.rejectedExecutionHandler = builder.RejectedExecutionHandler ?? RejectedExecutionHandlers.ABORT;
        this.agent = builder.Agent ?? EmptyAgent<T>.INST;
        this.mainModule = builder.MainModule;

        this.cleanBufferOnExit = builder.CleanBufferOnExit;
        if (cleanBufferOnExit && eventSequencer is MpUnboundedEventSequencer<T> unboundedBuffer) {
            this.mpUnboundedEventSequencer = unboundedBuffer;
        } else {
            this.mpUnboundedEventSequencer = null;
        }

        runningPromise = new Promise<int>(this);
        terminationPromise = new Promise<int>(this);
        runningFuture = runningPromise.AsReadonly();
        terminationFuture = terminationPromise.AsReadonly();

        // worker只依赖生产者屏障
        WaitStrategy waitStrategy = builder.WaitStrategy;
        if (waitStrategy == null) {
            worker = new Worker(eventSequencer.NewSingleConsumerBarrier());
        } else {
            worker = new Worker(eventSequencer.NewSingleConsumerBarrier(waitStrategy));
        }
        thread = threadFactory.NewThread(worker);
        // 添加worker的sequence为网关sequence，生产者们会监听到线程的消费进度
        eventSequencer.AddGatingBarriers(worker.barrier);

        // 完成绑定
        this.agent.Inject(this);
    }

    public IEventLoopAgent<T> Agent => agent;

    public override IEventLoopModule? MainModule => mainModule;

    /** 仅用于测试 */
    [VisibleForTesting]
    public ConsumerBarrier GetBarrier() {
        return worker.barrier;
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
        long count = eventSequencer.ProducerBarrier.Sequence() - worker.sequence.GetVolatile();
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
            tryPublish(task, sequence.Value, task.Options);
        } else {
            // 其它线程调用，可能阻塞
            tryPublish(task, eventSequencer.Next(1), task.Options);
        }
    }

    public override void Execute(Action command, int options = 0) {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (IsShuttingDown) {
            rejectedExecutionHandler.Rejected(Executors.BoxAction(command, options), this);
            return;
        }
        if (InEventLoop()) {
            // 当前线程调用，需要使用tryNext以避免死锁
            long? sequence = eventSequencer.TryNext(1);
            if (sequence == null) {
                rejectedExecutionHandler.Rejected(Executors.BoxAction(command, options), this);
                return;
            }
            tryPublish(command, sequence.Value, options);
        } else {
            // 其它线程调用，可能阻塞
            tryPublish(command, eventSequencer.Next(1), options);
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
    private void tryPublish(object task, long sequence, int options) {
        if (IsShuttingDown) {
            // 先发布sequence，避免拒绝逻辑可能产生的阻塞，不可以覆盖数据
            eventSequencer.Publish(sequence);
            Reject(task, options);
        } else {
            T eventObj = eventSequencer.ProducerGet(sequence);
            if (task is EventTranslator<T> translator) {
                try {
                    translator.TranslateTo(eventObj, sequence);
                }
                catch (Exception ex) {
                    logger.Warn("translateTo caught exception", ex);
                }
            } else {
                eventObj.Type = 0;
                eventObj.Obj1 = task;
                eventObj.Options = options;
                if (task is IScheduledFutureTask futureTask) {
                    futureTask.Id = (sequence); // nice
                    futureTask.RegisterCancellation();
                }
            }
            eventSequencer.Publish(sequence);

            if (!InEventLoop()) {
                // 确保线程已启动 -- ringBuffer私有的情况下才可以测试 sequence == 0
                if (sequence == 0) {
                    ensureThreadStarted();
                } else if (TaskOption.IsEnabled(options, TaskOption.WAKEUP_THREAD)) {
                    wakeup();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reject(object objTask, int options) {
        if (objTask is Action action) {
            rejectedExecutionHandler.Rejected(Executors.BoxAction(action, options), this);
        } else {
            ITask task = (ITask)objTask;
            rejectedExecutionHandler.Rejected(task, this);
        }
    }

    public T GetEvent(long sequence) {
        CheckSequence(sequence);
        return eventSequencer.producerGet(sequence);
    }

    private static void CheckSequence(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException("invalid sequence " + sequence);
        }
    }

    /**
     * 开放的特殊接口
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre> {@code
     *      long sequence = eventLoop.NextSequence();
     *      try {
     *          RingBufferEvent event = eventLoop.GetEvent(sequence);
     *          // Do work.
     *      } finally {
     *          eventLoop.Publish(sequence)
     *      }
     * }</pre>
     *
     * @return 如果申请成功，则返回对应的sequence，否则返回null
     */
    [Beta]
    public long? NextSequence() {
        return NextSequence(1);
    }

    [Beta]
    public void Publish(long sequence) {
        CheckSequence(sequence);
        eventSequencer.ProducerBarrier.Publish(sequence);
        if (sequence == 0 && !InEventLoop()) {
            ensureThreadStarted();
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
        CheckSequence(lo);
        eventSequencer.ProducerBarrier.Publish(lo, hi);
        if (lo == 0 && !InEventLoop()) {
            ensureThreadStarted();
        }
    }

    protected internal override void ReschedulePeriodic(IScheduledFutureTask scheduledTask, bool triggered) {
        Debug.Assert(InEventLoop());
        if (IsShuttingDown) {
            scheduledTask.CancelWithoutRemove();
            return;
        }
        scheduledTaskQueue.Enqueue(scheduledTask);
    }

    protected internal override void RemoveScheduled(IScheduledFutureTask scheduledTask) {
        if (IsShuttingDown) {
            scheduledTaskQueue.Remove(scheduledTask);
        } else {
            scheduledTask.CancelWithoutRemove();
            Execute(scheduledTask); // task.run方法会检测取消信号，避免额外封装
        }
        // else 等待任务超时弹出时再删除 -- 延迟删除可能存在内存泄漏，但压任务又可能导致阻塞（有界队列）
    }

    protected internal override long TickTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _tickTime);
    }

    // C#的另一个坑，override的时候不能增加set...
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetTickTime(long tickTime) {
        Volatile.Write(ref _tickTime, tickTime);
    }

    #endregion

    #region 线程状态切换

    public override IFuture Start() {
        EnsureThreadStarted();
        return RunningFuture;
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
            state = ST_TERMINATED;

            runningPromise.TrySetException(new StartFailedException("Stillborn"));
            terminationPromise.TrySetResult(0);
        } else {
            // 等待策略是根据alert信号判断EventLoop是否已开始关闭的，因此即使inEventLoop也需要alert，否则可能丢失信号，在waitFor处无法停止
            worker.barrier.alert();
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
            if (realState >= targetState) {
                return;
            }
            // retry
            expectedState = realState;
        }
    }

    #endregion
}
}