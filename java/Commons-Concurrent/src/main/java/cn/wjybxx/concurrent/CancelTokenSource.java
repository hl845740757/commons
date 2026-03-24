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

import cn.wjybxx.base.IPooledCloseable;
import cn.wjybxx.base.IRegistration;
import cn.wjybxx.base.Registration;
import cn.wjybxx.base.concurrent.BetterCancellationException;
import cn.wjybxx.base.concurrent.CancelCodeBuilder;
import cn.wjybxx.base.concurrent.CancelCodes;
import cn.wjybxx.base.pool.ConcurrentObjectPool;

import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 取消令牌源由任务的创建者（发起者）持有，具备取消权限。
 * <p>
 * 与{@link Promise}的实现差异：
 * 1.监听器列表为双向链表，通过锁互斥。
 * 2.监听器的通知未做栈深度优化，因为与监听器列表的删除冲突。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public final class CancelTokenSource implements ICancelTokenSource {

    private static final IEventLoop delayer = GlobalEventLoop.INST;

    @SuppressWarnings("unused")
    private volatile int code;
    @SuppressWarnings("unused")
    private volatile int lock;
    private Completion _head;
    private Completion _tail;

    public CancelTokenSource() {

    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            CancelCodes.checkCode(code);
            VH_CODE.setRelease(this, code);
        }
    }

    @Override
    public boolean canBeCancelled() {
        return true;
    }

    @Override
    public CancelTokenSource newInstance(boolean copyCode) {
        return new CancelTokenSource(copyCode ? code : 0);
    }

    @Override
    public CancelTokenSource newInstance() {
        return new CancelTokenSource();
    }

    // region tokenSource

    /**
     * 将Token置为取消状态
     *
     * @param cancelCode 取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}
     * @return 如果Token已被取消，则返回旧值（大于0）；如果Token尚未被取消，则将Token更新为取消状态，并返回0。
     * @throws IllegalArgumentException      如果code小于等于0；或reason部分为0
     * @throws UnsupportedOperationException 如果context是只读的
     */
    @Override
    public boolean cancel(int cancelCode) {
        CancelCodes.checkCode(cancelCode);
        int preCode = internalCancel(cancelCode);
        if (preCode != 0) {
            return false;
        }
        postComplete(this);
        return true;
    }

    /** 使用默认原因取消 */
    @Override
    public boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

    /**
     * 该方法主要用于兼容JDK
     *
     * @param mayInterruptIfRunning 是否可以中断目标线程；注意该参数由任务自身处理，且任务监听了取消信号才有用
     */
    public boolean cancel(boolean mayInterruptIfRunning) {
        return cancel(mayInterruptIfRunning
                ? (CancelCodes.REASON_DEFAULT | CancelCodes.MASK_INTERRUPT)
                : CancelCodes.REASON_DEFAULT);
    }

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode        取消码
     * @param millisecondsDelay 延迟时间(毫秒) -- 单线程版的话，真实单位取决于约定。
     */
    public void cancelAfter(int cancelCode, long millisecondsDelay) {
        cancelAfter(cancelCode, millisecondsDelay, TimeUnit.MILLISECONDS, delayer);
    }

    /**
     * 在一段时间后发送取消命令
     * (将由默认的调度器调度)
     *
     * @param cancelCode 取消码
     * @param delay      延迟时间
     * @param timeUnit   时间单位
     */
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit) {
        cancelAfter(cancelCode, delay, timeUnit, delayer);
    }

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode 取消码
     * @param delay      延迟时间
     * @param timeUnit   时间单位
     * @param executor   调度器
     */
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit, ScheduledExecutorService executor) {
        if (executor == null) throw new IllegalArgumentException("delayer is null");
        if (this.code == 0) {
            if (executor instanceof IScheduledExecutorService betterExecutor) {
                Canceller canceller = new Canceller(this, cancelCode);
                betterExecutor.scheduleAction(canceller, delay, timeUnit, this);
                // executor会自动监听延时任务的cancelToken
            } else {
                // jdk的scheduler不会响应取消令牌，我们通过Future及时取消定时任务 -- 未来更换实现后可避免
                JDKCanceller canceller = new JDKCanceller(this, cancelCode);
                canceller.future = executor.schedule(canceller, delay, timeUnit);
                this.thenNotify(canceller, null);
            }
        }
    }

    private static class Canceller implements Runnable {

        final CancelTokenSource source;
        final int cancelCode;

        private Canceller(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        @Override
        public void run() {
            source.cancel(cancelCode);
        }
    }

    private static class JDKCanceller implements Runnable, ICancelTokenListener {

        final CancelTokenSource source;
        final int cancelCode;
        ScheduledFuture<?> future;

        private JDKCanceller(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        @Override
        public void run() {
            source.cancel(cancelCode);
        }

        @Override
        public void onCancelRequested(ICancelToken cancelToken, Object ctx) {
            future.cancel(false);
        }
    }

    // endregion

    // region token

    @Override
    public int cancelCode() {
        return code;
    }

    @Override
    public boolean isRequested() {
        return code != 0;
    }

    @Override
    public int reason() {
        return CancelCodes.getReason(code);
    }

    @Override
    public void checkCancel() {
        int code = this.code;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    // endregion

    // region 监听器

    // region uni-accept

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action, int options) {
        return uniAccept(null, action, options);
    }

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action) {
        return uniAccept(null, action, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept(executor, action, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept(executor, action, options);
    }

    private IRegistration uniAccept(Executor executor, Consumer<? super ICancelToken> action,
                                    int options) {
        Objects.requireNonNull(action);
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_ACCEPT, action, null);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_ACCEPT, action, null);
        return pushCompletion(completion);
    }

    // endregion

    // region uni-accept-ctx

    @Override
    public IRegistration thenAccept(BiConsumer<? super ICancelToken, Object> action, Object ctx, int options) {
        return uniAcceptCtx(null, action, ctx, options);
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super ICancelToken, Object> action, Object ctx) {
        return uniAcceptCtx(null, action, ctx, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super ICancelToken, Object> action, Object ctx) {
        Objects.requireNonNull(executor, "executor");
        return uniAcceptCtx(executor, action, ctx, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super ICancelToken, Object> action, Object ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniAcceptCtx(executor, action, ctx, options);
    }

    private IRegistration uniAcceptCtx(Executor executor, BiConsumer<? super ICancelToken, Object> action,
                                       Object ctx, int options) {
        Objects.requireNonNull(action);
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_ACCEPT_CTX, action, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_ACCEPT_CTX, action, ctx);
        return pushCompletion(completion);
    }

    // endregion

    // region uni-run

    @Override
    public IRegistration thenRun(Runnable action, int options) {
        return uniRun(null, action, options);
    }

    @Override
    public IRegistration thenRun(Runnable action) {
        return uniRun(null, action, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action) {
        Objects.requireNonNull(executor, "executor");
        return uniRun(executor, action, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniRun(executor, action, options);
    }

    private IRegistration uniRun(Executor executor, Runnable action, int options) {
        Objects.requireNonNull(action);
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_RUN, action, null);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_RUN, action, null);
        return pushCompletion(completion);
    }

    // endregion

    // region uni-run-ctx

    @Override
    public IRegistration thenRun(Consumer<Object> action, Object ctx, int options) {
        return uniRunCtx(null, action, ctx, options);
    }

    @Override
    public IRegistration thenRun(Consumer<Object> action, Object ctx) {
        return uniRunCtx(null, action, ctx, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<Object> action, Object ctx) {
        Objects.requireNonNull(executor, "executor");
        return uniRunCtx(executor, action, ctx, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<Object> action, Object ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniRunCtx(executor, action, ctx, options);
    }

    private IRegistration uniRunCtx(Executor executor, Consumer<Object> action, Object ctx, int options) {
        Objects.requireNonNull(action);
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_RUN_CTX, action, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_RUN_CTX, action, ctx);
        return pushCompletion(completion);
    }

    // endregion

    // region uni-notify

    @Override
    public IRegistration thenNotify(ICancelTokenListener action, Object ctx, int options) {
        return uniNotify(null, action, ctx, options);
    }

    @Override
    public IRegistration thenNotify(ICancelTokenListener action, Object ctx) {
        return uniNotify(null, action, ctx, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, ICancelTokenListener action, Object ctx) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, ctx, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, ICancelTokenListener action, Object ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, ctx, options);
    }

    private IRegistration uniNotify(Executor executor, ICancelTokenListener listener, Object ctx, int options) {
        Objects.requireNonNull(listener, "listener");
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_NOTIFY, listener, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_NOTIFY, listener, ctx);
        return pushCompletion(completion);
    }

    // endregion

    // endregion

    // region core

    private static final VarHandle VH_CODE;
    private static final VarHandle VH_CTS_LOCK;
    private static final VarHandle VH_NODE_LOCK;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_CODE = l.findVarHandle(CancelTokenSource.class, "code", int.class);
            VH_CTS_LOCK = l.findVarHandle(CancelTokenSource.class, "lock", int.class);
            VH_NODE_LOCK = l.findVarHandle(Completion.class, "lock", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    private static final int SYNC = Promise.SYNC;
    private static final int ASYNC = Promise.ASYNC;
    private static final int NESTED = Promise.NESTED;

    /** @return preCode */
    private int internalCancel(int cancelCode) {
//        assert cancelCode != 0;
        return (int) VH_CODE.compareAndExchange(this, 0, cancelCode);
    }

    private void enterLock() {
        while (!VH_CTS_LOCK.compareAndSet(this, 0, 1)) {
            Thread.onSpinWait();
        }
    }

    private void exitLock() {
        assert (lock == 1);
        VH_CTS_LOCK.setVolatile(this, 0);
    }

    private Registration pushCompletion(Completion newHead) {
        ICancelToken cancelToken = ExecutorUtils.getCancelToken(newHead.ctx, newHead.options);
        if (cancelToken.isRequested()) {
            POOL.release(newHead);
            return Registration.CLOSED;
        }
        if (isRequested()) {
            newHead.tryFire(SYNC);
            return Registration.CLOSED;
        }

        Registration registration;
        enterLock();
        outer:
        try {
            if (isRequested()) {
                registration = Registration.CLOSED;
                break outer;
            }
            // 双向绑定
            if (_head != null) {
                newHead.next = _head;
                _head.prev = newHead;
            }
            _head = newHead;
            if (_tail == null) {
                _tail = newHead;
            }
            registration = new Registration(newHead, newHead.rid);
        } finally {
            exitLock();
        }
        if (!registration.hasResource()) {
            newHead.tryFire(SYNC);
        } else {
            // 如果目标令牌是池化的对象，可能添加到目标CTS新的生命周期，导致内存泄漏...不过，并发库的CTS是不能重用的
            if (cancelToken.canBeCancelled() &&
                    TaskOptions.isEnabled(newHead.options, TaskOptions.LISTEN_CANCEL_TOKEN)) {
                cancelToken.thenRun(INVOKER, registration, TaskOptions.STAGE_UNCANCELLABLE_CTX);
            }
        }
        return registration;
    }

    @Nullable
    private Completion popCompletion() {
        enterLock();
        try {
            Completion head = _head;
            if (head == null) {
                return null;
            }
            _head = head.next;
            if (_tail == head) {
                _tail = null;
            }
            // 解除双向绑定
            if (_head != null) {
                head.next = null;
                _head.prev = null;
            }
            assert head.prev == null;
            return head;
        } finally {
            exitLock();
        }
    }

    /**
     * 如果返回true，则应该由调用方触发回收
     * <p>
     * 注意：虽然node的前后置是由当前锁维护可见性的，但source不是，因此应当在Node的锁内调用。
     *
     * @return 是否删除成功
     */
    private boolean removeCompletion(Completion node) {
        enterLock();
        try {
            Completion prev = node.prev;
            Completion next = node.next;
            boolean contains;
            if (prev == null) {
                contains = node == _head;
            } else if (next == null) {
                contains = node == _tail;
            } else {
                contains = true;
            }
            if (!contains) {
                return false;
            }
            // fixPointers
            if (_head == _tail) {
                _head = _tail = null;
            } else if (node == _head) {
                _head = next;
                next.prev = null;
            } else if (node == _tail) {
                _tail = prev;
                prev.next = null;
            } else {
                prev.next = next;
                next.prev = prev;
            }
            // help gc
            node.prev = null;
            node.next = null;
            return true;
        } finally {
            exitLock();
        }
    }

    private static void postComplete(CancelTokenSource source) {
        Completion next;
        while ((next = source.popCompletion()) != null) {
            next.tryFire(SYNC);
        }
    }

    // endregion

    // region completion

    private static final int TYPE_ACCEPT = 0;
    private static final int TYPE_ACCEPT_CTX = 1;
    private static final int TYPE_RUN = 2;
    private static final int TYPE_RUN_CTX = 3;
    private static final int TYPE_NOTIFY = 4;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private static final int MASK_TASK_TYPE = 0x0F;
    /** 已加入异步队列 -- 不能被立即销毁；必须由TryFire销毁 */
    private static final int MASK_ASYNC_FIRING = 0x10;

    private static final ConcurrentObjectPool<Completion> POOL = new ConcurrentObjectPool<>(Completion::new,
            Completion::reset, TaskPoolConfig.getPoolSize(TaskPoolType.CTS_COMPLETION));

    /** 申请一个{@link Completion}对象 */
    private static Completion getCompletion(Executor executor, int options, CancelTokenSource source,
                                            int type, Object action, Object ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        Completion completion = POOL.acquire();
        completion.rid++; // 从池中取出时也加1
        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    /** 用于关闭监听器 */
    private static final Consumer<Object> INVOKER = ctx -> {
        Registration registration = (Registration) ctx;
        registration.close();
    };

    /**
     * 回收的时机：
     * 1.如果已进入异步派发阶段，则由{@code tryFire(ASYNC)}负责回收
     * 2.如果尚未进入异步派发阶段，则由{@link #removeCompletion(Completion)}成功的一方负责回收。
     */
    private static class Completion implements ITask, IPooledCloseable {

        /** 非volatile，可见性由{@link CancelTokenSource#lock}保证 */
        Completion prev;
        Completion next;

        /** 重入id -- 只增不减 */
        int rid;
        /** 互斥锁 */
        int lock;

        CancelTokenSource source;
        Executor executor;
        int options; // 包含任务类型信息
        Object action;
        Object ctx;

        protected void reset() {
            assert prev == null && next == null;
//            assert lock == 1; // Push失败时不需要加锁

            rid++; // 池化时+1，volatile安全
            source = null;
            executor = null;
            options = 0;
            action = null;
            ctx = null;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public final void run() {
            tryFire(ASYNC);
        }

        public void tryFire(int mode) {
            // 如果走到这里，当前Completion一定未被回收，但action可能已被清理，即已收到取消信号
            CancelTokenSource source;
            Executor executor;
            int options;
            Object action;
            Object ctx;

            enterLock();
            boolean fire; // 没有goto，我们使用bool标记
            try {
                options = this.options;
                action = this.action;
                ctx = this.ctx;
                // 如果已收到取消信号，则直接回收
                if (action == null || ExecutorUtils.isCancelRequested(ctx, options)) {
                    POOL.release(this);
                    return;
                }
                source = this.source;
                executor = this.executor;

                // 如果是同步模式，需要claim -- 与Future的实现不同，我们在锁外提交任务以避免阻塞，但需要先标记
                if (mode <= 0 && !ExecutorUtils.isInlinable(executor, options)) {
                    this.options |= MASK_ASYNC_FIRING;
                    this.executor = null;
                    fire = false;
                } else {
                    // 数据已拷贝到临时变量；在锁内回收，锁外执行
                    POOL.release(this);
                    fire = true;
                }
            } finally {
                exitLock();
            }
            // 在锁外执行回调和提交任务
            if (fire) {
                fireNow(source, options, action, ctx);
                return;
            }
            try {
                executor.execute(this);
            } catch (Exception ex) {
                FutureLogger.logCause(ex);
            }
        }

        @SuppressWarnings("unchecked")
        private static void fireNow(CancelTokenSource source,
                                    int options, Object rawAction, Object ctx) {
            int type = options & MASK_TASK_TYPE;
            try {
                switch (type) {
                    case TYPE_ACCEPT -> {
                        Consumer<ICancelToken> action = (Consumer<ICancelToken>) rawAction;
                        action.accept(source);
                    }
                    case TYPE_ACCEPT_CTX -> {
                        BiConsumer<ICancelToken, Object> action = (BiConsumer<ICancelToken, Object>) rawAction;
                        action.accept(source, ctx);
                    }
                    case TYPE_RUN -> {
                        Runnable action = (Runnable) rawAction;
                        action.run();
                    }
                    case TYPE_RUN_CTX -> {
                        Consumer<Object> action = (Consumer<Object>) rawAction;
                        action.accept(ctx);
                    }
                    case TYPE_NOTIFY -> {
                        ICancelTokenListener action = (ICancelTokenListener) rawAction;
                        action.onCancelRequested(source, ctx);
                    }
                    default -> {
                        throw new IllegalStateException();
                    }
                }
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "Action caught an exception");
            }
        }

        @Override
        public boolean isClosed(long reentryId) {
            enterLock();
            try {
                return rid != reentryId || this.action == null;
            } finally {
                exitLock();
            }
        }

        @Override
        public final void close(long reentryId) {
            enterLock();
            try {
                // 只有rid匹配时，才能保证数据有效性 -- action为null表示已执行回调
                if (rid != reentryId || this.action == null) {
                    return;
                }
                this.action = null;
                this.ctx = null;
                // 如果当前未进入异步执行，尝试立即回收 -- 可能正处于TryFire等待锁的状态，因此删除可能会失败
                if ((options & MASK_ASYNC_FIRING) == 0 && source.removeCompletion(this)) {
                    POOL.release(this);
                }
            } finally {
                exitLock();
            }
        }

        private void enterLock() {
            while (!VH_NODE_LOCK.compareAndSet(this, 0, 1)) {
                Thread.onSpinWait();
            }
        }

        private void exitLock() {
            assert (lock == 1);
            VH_NODE_LOCK.setVolatile(this, 0);
        }
    }
    // endregion
}