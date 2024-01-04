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

package cn.wjybxx.common.concurrent2;

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.ex.NoLogRequiredException;
import cn.wjybxx.common.concurrent.BlockingOperationException;
import cn.wjybxx.common.concurrent.SingleThreadExecutor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiConsumer;

/**
 * {@link IPromise}的基础实现
 *
 * @author wjybxx
 * @version 1.0
 * date - 2020/3/6
 */
public class Promise<V> implements IPromise<V> {

    protected static final Logger logger = LoggerFactory.getLogger(Promise.class);

    /** 1毫秒多少纳秒 */
    private static final int NANO_PER_MILLISECOND = 1000_000;
    /** 表示任务开始运行 */
    private static final Object COMPUTING = new Object();
    /** 如果一个任务成功时没有结果（或结果为null），使用该对象代替。 */
    private static final Object NIL = new Object();

    private static final int SIGNAL_INIT = 0;
    private static final int SIGNAL_NEEDED = 1;
    private static final int SIGNAL_NOTIFIED = 2;

    /**
     * Future关联的任务的计算结果，它同时也存储者{@code Future}的状态信息。
     * <ul>
     * <li>{@code null}表示初始状态</li>
     * <li>{@link #COMPUTING}表示任务正在进行，取消不能被立即响应</li>
     * <li>{@link #NIL}表示终止状态，表示正常完成，但是计算结果为null</li>
     * <li>{@link AltResult}表示终止状态，表示计算中出现异常，{@link AltResult#cause}为计算失败的原因。</li>
     * <li>其它任何非null值，表示正常完成，且计算结果非null。</li>
     * </ul>
     */
    private volatile Object result;

    /**
     * 当前对象上的所有监听器，使用栈方式存储
     * 如果{@code stack}为{@link Promise#TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
     */
    private volatile Completion stack;

    /** 任务绑定的上下文 */
    private final IContext ctx;
    /** 任务绑定的executor */
    private final Executor executor;


    /**
     * 是否需要调用{@link #notifyAll()} - 是否可能有线程阻塞在当前{@code Future}上，减少锁获取和notifyAll调用。
     */
    @SuppressWarnings("unused")
    private volatile int signalNeeded;

    public Promise() {
        this.ctx = null;
        this.executor = null;
    }

    public Promise(IContext ctx, Executor executor) {
        this.ctx = ctx;
        this.executor = executor;
    }

    Promise(IContext ctx, Executor executor, Object result) {
        this.ctx = ctx;
        this.executor = executor;
        VH_RESULT.setRelease(this, result);
    }

    public static <V> Promise<V> completedPromise(V result) {
        return new Promise<>(null, null, result == null ? NIL : result);
    }

    public static <V> Promise<V> completedPromise(IContext ctx, Executor executor, V result) {
        return new Promise<>(ctx, executor, result == null ? NIL : result);
    }

    public static <V> Promise<V> failedPromise(Throwable cause) {
        Objects.requireNonNull(cause);
        return new Promise<>(null, null, new AltResult(cause));
    }

    public static <V> Promise<V> failedPromise(IContext ctx, Executor executor, Throwable cause) {
        Objects.requireNonNull(cause);
        return new Promise<>(ctx, executor, new AltResult(cause));
    }

    // region internal

    /** 异常结果包装对象，只有该类型表示失败 */
    static class AltResult {

        final Throwable cause;

        AltResult(Throwable cause) {
            this.cause = cause;
        }
    }

    final Object encodeValue(V value) {
        return (value == null) ? NIL : value;
    }

    @SuppressWarnings("unchecked")
    final V decodeValue(Object result) {
        return result == NIL ? null : (V) result;
    }

    /**
     * 非取消完成可以由初始状态或不可取消状态进入完成状态
     * CAS{@code null}或者{@link #COMPUTING} 到指定结果值
     */
    private boolean internalComplete(Object result) {
        // 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势
        Object preResult = VH_RESULT.compareAndExchange(this, null, result);
        if (preResult == null) {
            return true;
        }
        if (preResult == COMPUTING) {
            return VH_RESULT.compareAndSet(this, COMPUTING, result);
        }
        return false;
    }

    /**
     * 标记为需要通知
     *
     * @return 如果标记成功，则返回true，否则返回false。如果失败，通常意味着future已完成。
     */
    private boolean markSignalNeeded() {
        final int preValue = (int) VH_SIGNALNEEDED.compareAndExchange(this, SIGNAL_INIT, SIGNAL_NEEDED);
        return preValue != SIGNAL_NOTIFIED;
    }

    /**
     * 标记为已通知
     *
     * @return 是否需要通知，如果需要获取锁进行通知，则返回true，否则返回false。如果返回false，意味着没有线程阻塞。
     */
    private boolean markSignalNotified() {
        final int preValue = (int) VH_SIGNALNEEDED.getAndSet(this, SIGNAL_NOTIFIED);
        return preValue == SIGNAL_NEEDED;
    }
    // endregion

    // region ctx

    @Override
    public IContext ctx() {
        return ctx;
    }

    @Nullable
    @Override
    public Executor executor() {
        return executor;
    }

    // endregion

    // region 状态查询

    /**
     * Q:为什么多线程的代码喜欢将volatile变量存为临时变量或传递给某一个方法做判断？
     * A:为了保证数据的一致性。当对volatile执行多个操作时，如果不把volatile变量保存下来，则每次读取的结果可能是不一样的。
     */
    static boolean isDone0(Object result) {
        return result != null && result != COMPUTING;
    }

    private static boolean isSucceeded0(Object result) {
        if (result == null || result == COMPUTING) {
            return false;
        }
        // 测试特殊值有一丢丢的收益
        return result == NIL
                || !(result instanceof AltResult);
    }

    private static boolean isFailed0(Object result) {
        if (result == null || result == COMPUTING || result == NIL) {
            return false;
        }
        return result instanceof AltResult altResult
                && !(altResult.cause instanceof CancellationException);
    }

    private static boolean isCancelled0(Object result) {
        if (result == null || result == COMPUTING || result == NIL) {
            return false;
        }
        return result instanceof AltResult altResult
                && altResult.cause instanceof CancellationException;
    }

    @Override
    public final State state() {
        Object r = result;
        if (r == null) {
            return State.RUNNING;
        }
        if (r instanceof AltResult altResult) {
            if (altResult.cause instanceof CancellationException) {
                return State.CANCELLED;
            } else {
                return State.FAILED;
            }
        }
        return State.SUCCESS;
    }

    @Override
    public final boolean isPending() {
        return result == null;
    }

    @Override
    public final boolean isComputing() {
        return result == COMPUTING;
    }

    @Override
    public final boolean isDone() {
        return isDone0(result);
    }

    @Override
    public final boolean isCancelled() {
        return isCancelled0(result);
    }

    @Override
    public boolean isSucceeded() {
        return isSucceeded0(result);
    }

    @Override
    public final boolean isFailed() {
        return isFailed0(result);
    }

    @Override
    public final boolean isFailedOrCancelled() {
        return result instanceof AltResult;
    }

    // endregion

    // region 状态更新

    @Override
    public final boolean trySetComputing(boolean resultIfComputing) {
        Object preResult = VH_RESULT.compareAndExchange(this, null, COMPUTING);
        if (preResult == null) {
            return true;
        }
        if (preResult == COMPUTING) {
            return resultIfComputing;
        }
        return false;
    }

    @Override
    public final void setComputing() {
        if (!trySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    @Override
    public final boolean trySetResult(V result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public final void setResult(V result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return;
        }
        throw new IllegalStateException("Already complete");
    }

    @Override
    public final boolean trySetException(@Nonnull Throwable cause, boolean logCause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public final void setException(@Nonnull Throwable cause, boolean logCause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            postComplete(this);
            return;
        }
        throw new IllegalStateException("Already complete");
    }

    @Override
    public final boolean trySetCancelled() {
        // 该方法通常是由于任务的执行者检测到取消信号调用的，因此无需捕获堆栈
        return trySetException(StacklessCancellationException.INSTANCE, false);
    }

    @Override
    public final void setCancelled() {
        setException(StacklessCancellationException.INSTANCE, false);
    }

    @Override
    public final boolean cancel(boolean mayInterruptIfRunning) {
        // 由于要创建异常，先测试一下result
        Object r = result;
        if (isDone0(r)) {
            return isCancelled0(r);
        }
        if (internalComplete(new AltResult(new CancellationException()))) {
            postComplete(this);
            return true;
        }
        // 可能被其它线程取消
        return isCancelled();
    }
    // endregion

    // region 非阻塞结果查询

    @Override
    public final V getNow() {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        return null;
    }

    @Override
    public final V getNow(V valueIfAbsent) {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        return valueIfAbsent;
    }

    @Override
    public final boolean acceptNow(@Nonnull BiConsumer<? super V, ? super Throwable> action) {
        final Object r = result;
        if (!isDone0(r)) {
            return false;
        }
        if (r == NIL) {
            action.accept(null, null);
            return true;
        }
        if (r instanceof AltResult) {
            action.accept(null, ((AltResult) r).cause);
        } else {
            @SuppressWarnings("unchecked") final V value = (V) r;
            action.accept(value, null);
        }
        return true;
    }

    @Override
    public final V resultNow() {
        final Object r = result;
        if (!isDone0(r)) {
            throw new IllegalStateException("Task has not completed");
        }
        if (r == NIL) {
            return null;
        }
        if (r instanceof AltResult altResult) {
            if (altResult.cause instanceof CancellationException) {
                throw new IllegalStateException("Task was cancelled");
            } else {
                throw new IllegalStateException("Task completed with exception");
            }
        }
        @SuppressWarnings("unchecked") V value = (V) r;
        return value;
    }

    @Override
    public final Throwable exceptionNow(boolean throwIfCancelled) {
        final Object r = result;
        if (r instanceof AltResult altResult) {
            if (throwIfCancelled && altResult.cause instanceof CancellationException) {
                throw new IllegalStateException("Task was cancelled");
            }
            return altResult.cause;
        }
        if (!isDone0(r)) {
            throw new IllegalStateException("Task has not completed");
        } else {
            throw new IllegalStateException("Task completed with a result");
        }
    }

    /**
     * 不命名为{@code reportGetNow}是为了放大不同之处。
     */
    @SuppressWarnings("unchecked")
    private static <T> T reportJoin(final Object r) {
        if (r == NIL) {
            return null;
        }
        if (r instanceof AltResult altResult) {
            Throwable cause = altResult.cause;
            if (cause instanceof CancellationException) {
                throw (CancellationException) cause;
            }
            throw new CompletionException(cause);
        }
        return (T) r;
    }

    @SuppressWarnings("unchecked")
    private static <T> T reportGet(Object r) throws ExecutionException {
        if (r == NIL) {
            return null;
        }
        if (r instanceof AltResult altResult) {
            Throwable cause = altResult.cause;
            if (cause instanceof CancellationException) {
                throw (CancellationException) cause;
            }
            throw new ExecutionException(cause);
        }
        return (T) r;
    }
    // endregion

    // region 阻塞结果查询

    /** 死锁检查 */
    protected final void checkDeadlock() {
        if (executor instanceof SingleThreadExecutor eventLoop && eventLoop.inEventLoop()) {
            throw new BlockingOperationException();
        }
    }

    @Override
    public final V get() throws InterruptedException, ExecutionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportGet(r);
        }
        await();
        return reportGet(result);
    }

    @Override
    public final V get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        final Object r = result;
        if (isDone0(r)) {
            return reportGet(r);
        }
        if (await(timeout, unit)) {
            return reportGet(result);
        }
        throw new TimeoutException();
    }

    @Override
    public final V join() throws CompletionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        awaitUninterruptedly();
        return reportJoin(result);
    }

    @Override
    public final Promise<V> await() throws InterruptedException {
        if (isDone()) {
            return this;
        }
        checkDeadlock();

        ThreadUtils.checkInterrupted();
        synchronized (this) {
            while (!isDone() && markSignalNeeded()) {
                this.wait();
            }
        }
        return this;
    }

    @Override
    public final Promise<V> awaitUninterruptedly() {
        if (isDone()) {
            return this;
        }
        checkDeadlock();

        boolean interrupted = Thread.interrupted();
        try {
            synchronized (this) {
                while (!isDone() && markSignalNeeded()) {
                    try {
                        this.wait();
                    } catch (InterruptedException e) {
                        interrupted = true;
                    }
                }
            }
        } finally {
            if (interrupted) {
                ThreadUtils.recoveryInterrupted();
            }
        }
        return this;
    }

    @Override
    public final boolean await(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        if (timeout <= 0) {
            return isDone();
        }
        if (isDone()) {
            return true;
        }
        // 在执行阻塞操作前检测死锁 -- 这是我们的Future的重要功能之一
        checkDeadlock();

        // 在执行一个耗时操作之前检查中断是有必要的
        ThreadUtils.checkInterrupted();
        final long deadline = System.nanoTime() + unit.toNanos(timeout);
        synchronized (this) {
            // 其实可以不判断isDone()，但是判断isDone可以更早的感知future进入完成状态
            while (!isDone() && markSignalNeeded()) {
                // 获取锁需要时间，因此应该在获取锁之后计算剩余时间
                final long remainNano = deadline - System.nanoTime();
                if (remainNano <= 0) {
                    return false;
                }
                this.wait(remainNano / NANO_PER_MILLISECOND, (int) (remainNano % NANO_PER_MILLISECOND));
            }
            return true;
        }
    }

    @Override
    public final boolean awaitUninterruptedly(long timeout, @Nonnull TimeUnit unit) {
        if (timeout <= 0) {
            return isDone();
        }
        if (isDone()) {
            return true;
        }
        checkDeadlock();

        boolean interrupted = Thread.interrupted();
        final long deadline = System.nanoTime() + unit.toNanos(timeout);
        try {
            synchronized (this) {
                while (!isDone() && markSignalNeeded()) {
                    final long remainNano = deadline - System.nanoTime();
                    if (remainNano <= 0) {
                        return false;
                    }
                    try {
                        this.wait(remainNano / NANO_PER_MILLISECOND, (int) (remainNano % NANO_PER_MILLISECOND));
                    } catch (InterruptedException e) {
                        interrupted = true;
                    }
                }
                return true;
            }
        } finally {
            if (interrupted) {
                Thread.currentThread().interrupt();
            }
        }
    }

    // endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Modes for Completion.tryFire. Signedness matters.
    /**
     * 同步调用模式，表示压栈过程中发现{@code Future}已进入完成状态，从而调用的{@link Completion#tryFire(int)}。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(Promise)}。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    static final int SYNC = 0;
    /**
     * 异步调用模式，表示提交到{@link Executor}之后调用{@link Completion#tryFire(int)}
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(Promise)}。
     * 2. 在该模式，表示已获得执行权限，可立即执行。
     */
    static final int ASYNC = 1;
    /**
     * 嵌套调用模式，表示由{@link #postComplete(Promise)}中触发调用。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，不触发目标{@code Future}的完成事件，而是返回目标{@code Future}，由当前{@code Future}代为推送。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    static final int NESTED = -1;

    static boolean isSyncOrNestedMode(int mode) {
        return mode <= 0;
    }

    /**
     * 是否是嵌套模式
     */
    static boolean isNestedMode(int mode) {
        return mode < 0;
    }

    final void pushCompletion(Completion newHead) {
        // 如果future已完成，则立即执行
        if (isDone()) {
            newHead.tryFire(SYNC);
            return;
        }

        Completion expectedHead = stack;
        Completion realHead;

        while (true) {
            // .next 由下面的CAS保证可见性
            newHead.next = expectedHead;
            realHead = (Completion) VH_STACK.compareAndExchange(this, expectedHead, newHead);
            if (realHead == expectedHead) {
                // 成功添加completion到头部，其会在Future进入完成状态时被通知
                return;
            }
            if (realHead == TOMBSTONE) {
                // 有线程触发了Future的完成事件，该completion需要立即被通知
                break;
            }
            // retry
            expectedHead = realHead;
        }

        // 到这里的时候 head == TOMBSTONE，表示目标Future已进入完成状态，且正在被通知或已经通知完毕。
        // 由于Future已进入完成状态，且我们的Completion压栈失败，因此新的completion需要当前线程来通知
        newHead.next = null;
        newHead.tryFire(SYNC);
    }

    /**
     * 推送future的完成事件。
     * - 声明为静态会更清晰易懂
     */
    static void postComplete(Promise<?> future) {
        assert future.isDone();

        Completion next = null;
        outer:
        while (true) {
            // 在通知监听器之前，先唤醒阻塞的线程
            future.releaseWaiters();

            // 将当前future上的监听器添加到next前面
            next = future.clearListeners(next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                // help gc
                curr.next = null;

                // Completion的tryFire实现不可以抛出异常，否则会导致其它监听器也丢失信号
                future = curr.tryFire(NESTED);

                if (future != null) {
                    // 如果某个Completion使另一个Future进入完成状态，则更新为新的Future，并重试整个流程
                    continue outer;
                }
            }
            break;
        }
    }

    /** 释放等待中的线程 */
    private void releaseWaiters() {
        if (markSignalNotified()) {
            synchronized (this) {
                notifyAll();
            }
        }
    }

    /**
     * 清空当前{@code Future}上的监听器，并将当前{@code Future}上的监听器逆序方式插入到{@code onto}前面。
     * <p>
     * Q: 这步操作是要干什么？<br>
     * A: 由于一个{@link Completion}在执行时可能使另一个{@code Future}进入完成状态，如果不做处理的话，则可能产生一个很深的递归，
     * 从而造成堆栈溢出，也影响性能。该操作就是将可能通知的监听器由树结构展开为链表结构，消除深嵌套的递归。
     * Guava中{@code AbstractFuture}和{@link CompletableFuture}都有类似处理。
     * <pre>
     *      Future1(stack) -> Completion1_1 ->  Completion1_2 -> Completion1_3
     *                              ↓
     *                          Future2(stack) -> Completion2_1 ->  Completion2_2 -> Completion2_3
     *                                                   ↓
     *                                              Future3(stack) -> Completion3_1 ->  Completion3_2 -> Completion3_3
     * </pre>
     * 转换后的结构如下：
     * <pre>
     *      Future1(stack) -> Completion1_1 ->  Completion2_1 ->  Completion2_2 -> Completion2_3 -> Completion1_2 -> Completion1_3
     *                           (已执行)                 ↓
     *                                              Future3(stack) -> Completion3_1 ->  Completion3_2 -> Completion3_3
     * </pre>
     */
    private Completion clearListeners(Completion onto) {
        // 我们需要进行三件事
        // 1. 原子方式将当前Listeners赋值为TOMBSTONE，因为pushCompletion添加的监听器的可见性是由CAS提供的。
        // 2. 将当前栈内元素逆序，因为即使在接口层进行了说明（不提供监听器执行时序保证），但仍然有人依赖于监听器的执行时序(期望先添加的先执行)
        // 3. 将逆序后的元素插入到'onto'前面，即插入到原本要被通知的下一个监听器的前面

        Completion head;
        do {
            head = stack;
            if (head == TOMBSTONE) {
                return onto;
            }
        } while (!VH_STACK.compareAndSet(this, head, TOMBSTONE));

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static final VarHandle VH_RESULT;
    private static final VarHandle VH_STACK;
    private static final VarHandle VH_SIGNALNEEDED;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_RESULT = l.findVarHandle(Promise.class, "result", Object.class);
            VH_STACK = l.findVarHandle(Promise.class, "stack", Completion.class);
            VH_SIGNALNEEDED = l.findVarHandle(Promise.class, "signalNeeded", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    // region signallers

    // 开放给Completion的方法

    final boolean completeNull() {
        return internalComplete(NIL);
    }

    final boolean completeValue(V value) {
        return internalComplete(encodeValue(value));
    }

    // 在异常处理上不同于CompletableFuture，这里保留原始结果和异常，不强制将异常转换为{@link CompletionException}。
    // 这样有助于客户端捕获正确的异常类型，而不是一个奇怪的CompletionException

    /**
     * 如果一个{@link Completion}在计算中出现异常，则使用该方法使目标进入完成状态。
     * (出现新的异常)
     */
    final boolean completeThrowable(@Nonnull Throwable x) {
        logCause(x);
        return internalComplete(encodeThrowable(x));
    }

    private static void logCause(Throwable x) {
        if (!(x instanceof NoLogRequiredException)) {
            logger.warn("future completed with exception", x);
        }
    }

    private static AltResult encodeThrowable(Throwable x) {
        return new AltResult((x instanceof CompletionException) ? x :
                new CompletionException(x));
    }

    /**
     * 使用依赖项的结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     */
    final boolean completeRelay(Object r) {
        return internalComplete(r);
    }

    /**
     * 使用依赖项的异常结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     * 在已知依赖项异常完成的时候可以调用该方法，减少开销。
     * 这里实现和{@link CompletableFuture}不同，这里保留原始结果，不强制将异常转换为{@link CompletionException}。
     */
    final boolean completeRelayThrowable(AltResult r) {
        return internalComplete(r);
    }

    /**
     * 实现{@link Runnable}接口是因为可能需要在另一个线程执行。
     */
    static abstract class Completion implements Runnable {

        /** 非volatile，通过{@link Promise#stack}的原子更新来保证可见性 */
        Completion next;

        @Override
        public final void run() {
            tryFire(ASYNC);
        }

        /**
         * 当依赖的某个{@code Future}进入完成状态时，该方法会被调用。
         * 如果tryFire使得另一个{@code Future}进入完成状态，分两种情况：
         * 1. mode指示不要调用{@link #postComplete(Promise)}方法时，则返回新进入完成状态的{@code Future}。
         * 2. mode指示可以调用{@link #postComplete(Promise)}方法时，则直接推送其进入完成状态的事件。
         * <p>
         * Q: 为什么没有{@code Future}参数？
         * A: 因为调用者可能是其它{@link Future}...
         */
        abstract Promise<?> tryFire(int mode);

    }

    /** 表示stack已被清理 */
    static final Completion TOMBSTONE = new Completion() {
        @Override
        Promise<Object> tryFire(int mode) {
            return null;
        }
    };

    // endregion
}
