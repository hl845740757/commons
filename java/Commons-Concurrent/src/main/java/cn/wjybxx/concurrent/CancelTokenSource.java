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

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.LockSupport;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 取消令牌源由任务的创建者（发起者）持有，具备取消权限。
 * <h3>实现说明</h3>
 * 这里的实现是{@link Promise}的翻版，但不同的是：取消令牌需要支持删除监听，而且取消令牌存在频繁增删监听的情况！
 * 由于实现高效且安全的删除并不容易，这里暂时采用延迟删除的方案。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public final class CancelTokenSource implements ICancelTokenSource {

    private static final IEventLoop delayer = GlobalEventLoop.INST;

    /**
     * 取消码
     * - 0表示未收到取消信号
     * - 非0表示收到取消信号
     */
    @SuppressWarnings("unused")
    private volatile int code;
    /**
     * 当前对象上的所有监听器，使用栈方式存储
     * 如果{@code stack}为{@link #TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
     */
    @SuppressWarnings("unused")
    private volatile Completion stack;

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
                ? (CancelCodes.REASON_DEFAULT & CancelCodes.MASK_INTERRUPT)
                : CancelCodes.REASON_DEFAULT);
    }

    @Override
    public void cancelAfter(int cancelCode, long millisecondsDelay) {
        cancelAfter(cancelCode, millisecondsDelay, TimeUnit.MILLISECONDS, delayer);
    }

    /**
     * 在一段时间后发送取消命令
     * (将由默认的调度器调度)
     */
    @Override
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit) {
        cancelAfter(cancelCode, delay, timeUnit, delayer);
    }

    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit, ScheduledExecutorService executor) {
        if (executor == null) throw new IllegalArgumentException("delayer is null");
        if (this.code == 0) {
            if (executor instanceof IScheduledExecutorService betterExecutor) {
                Canceller canceller = new Canceller(this, cancelCode);
                betterExecutor.scheduleAction(canceller, delay, timeUnit, this);
                // executor会自动监听延时任务的cancelToken
            } else {
                JDKCanceller canceller = new JDKCanceller(this, cancelCode);
                canceller.future = executor.schedule(canceller, delay, timeUnit);
                // jdk的scheduler不会响应取消令牌，我们通过Future及时取消定时任务 -- 未来更换实现后可避免
                this.thenNotify(canceller);
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
        public void onCancelRequested(ICancelToken cancelToken) {
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
    public boolean isCancelRequested() {
        return code != 0;
    }

    @Override
    public int reason() {
        return CancelCodes.getReason(code);
    }

    @Override
    public int degree() {
        return CancelCodes.getDegree(code);
    }

    @Override
    public boolean isInterruptible() {
        return CancelCodes.isInterruptible(code);
    }

    @Override
    public boolean isWithoutRemove() {
        return CancelCodes.isWithoutRemove(code);
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
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_ACCEPT, action, null);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_ACCEPT, action, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
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
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_ACCEPT_CTX, action, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_ACCEPT_CTX, action, ctx);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
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
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_RUN, action, null);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_RUN, action, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
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
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_RUN_CTX, action, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_RUN_CTX, action, ctx);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
    }

    // endregion

    // region uni-notify

    @Override
    public IRegistration thenNotify(ICancelTokenListener action, int options) {
        return uniNotify(null, action, options);
    }

    @Override
    public IRegistration thenNotify(ICancelTokenListener action) {
        return uniNotify(null, action, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, ICancelTokenListener action) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, ICancelTokenListener action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, options);
    }

    private IRegistration uniNotify(Executor executor, ICancelTokenListener listener, int options) {
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_NOTIFY, listener, null);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_NOTIFY, listener, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
    }

    // endregion

    // region uni-transferTo

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child) {
        return uniTransferTo(null, child, 0);
    }

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child, int options) {
        return uniTransferTo(null, child, options);
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child) {
        Objects.requireNonNull(executor, "executor");
        return uniTransferTo(executor, child, 0);
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniTransferTo(executor, child, options);
    }

    private IRegistration uniTransferTo(Executor executor, ICancelTokenSource child, int options) {
        Objects.requireNonNull(child, "child");
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_TRANSFER, child, null);
            return Registration.CLOSED;
        }
        Completion completion = getComplete(executor, options, this, TYPE_TRANSFER, child, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion.rid);
        return pushCompletion(completion) ? registration : Registration.CLOSED;
    }

    // endregion

    // endregion

    // region core

    private static final VarHandle VH_CODE;
    private static final VarHandle VH_STACK;
    private static final VarHandle VH_RID;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_CODE = l.findVarHandle(CancelTokenSource.class, "code", int.class);
            VH_STACK = l.findVarHandle(CancelTokenSource.class, "stack", Completion.class);
            VH_RID = l.findVarHandle(Completion.class, "rid", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    private static final int SYNC = Promise.SYNC;
    private static final int ASYNC = Promise.ASYNC;
    private static final int NESTED = Promise.NESTED;
    private static final Executor CLAIMED = Promise.CLAIMED;

    /** @return preCode */
    private int internalCancel(int cancelCode) {
//        assert cancelCode != 0;
        return (int) VH_CODE.compareAndExchange(this, 0, cancelCode);
    }

    /** @return 是否压栈成功 */
    private boolean pushCompletion(Completion newHead) {
        if (isCancelRequested()) {
            newHead.tryFire(SYNC);
            return false;
        }
        Completion expectedHead = stack;
        Completion realHead;
        while (expectedHead != TOMBSTONE) {
            newHead.next = expectedHead;
            realHead = (Completion) VH_STACK.compareAndExchange(this, expectedHead, newHead);
            if (realHead == expectedHead) { // success
                return true;
            }
            expectedHead = realHead; // retry
        }
        newHead.next = null;
        newHead.tryFire(SYNC);
        return false;
    }

    private static void postComplete(CancelTokenSource source) {
        Completion next = null;
        outer:
        while (true) {
            next = clearListeners(source, next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                curr.next = null; // help gc

                source = curr.tryFire(NESTED);
                if (source != null) {
                    continue outer;
                }
            }
            break;
        }
    }

    private static Completion clearListeners(CancelTokenSource source, Completion onto) {
        Completion head = source.stack;
        while (true) {
            if (head == TOMBSTONE) {
                return onto;
            }
            Completion realHead = (Completion) VH_STACK.compareAndExchange(source, head, TOMBSTONE);
            if (realHead == head) {
                break;
            }
            head = realHead;
        }

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    private static boolean tryInline(Completion completion, Executor e, int options) {
        // 尝试内联
        if (FutureUtils.isInlinable(e, options)) {
            return true;
        }
        e.execute(completion);
        return false;
    }
    // endregion

    // region completion

    private static final int TYPE_ACCEPT = 0;
    private static final int TYPE_ACCEPT_CTX = 1;
    private static final int TYPE_RUN = 2;
    private static final int TYPE_RUN_CTX = 3;
    private static final int TYPE_NOTIFY = 4;
    private static final int TYPE_TRANSFER = 5;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private static final int MASK_TASK_TYPE = 0x0F;

    private static final Completion TOMBSTONE = new Completion();
    private static final ConcurrentObjectPool<Completion> POOL = new ConcurrentObjectPool<>(Completion::new,
            Completion::reset, 100);

    /** 申请一个{@link Completion}对象 */
    private static Completion getComplete(Executor executor, int options, CancelTokenSource source,
                                          int type, Object action, Object ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_PRIORITY_AND_SCHEDULE_PHASE);
        options |= type;

        Completion completion = POOL.acquire();
        int rid = completion.rid + 1;
        completion.rid = rid;
        completion.fireId = rid;

        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    /** 为简化逻辑，我们总是在触发回调的时候才回收对象 */
    private static class Completion implements ITask, IPooledCloseable {

        /** 非volatile，由栈顶的cas更新保证可见性 */
        Completion next;
        /** 重入id -- 只增不减 */
        volatile int rid;
        /**
         * cts添加回调时的{@link #rid}快照
         * 1.该值永不清理，用于识别能否进行通知
         * 2.{@link #tryFire(int)}方法需要通过该值竞争更新{@link #rid}
         * 3.{@link #close(int)}方法通过用户持有的rid竞争更新{@link #rid}
         */
        int fireId;

        CancelTokenSource source;
        Executor executor;
        /** 任务的调度选项，包含任务的类型 */
        int options;
        Object action;
        Object ctx;

        protected void reset() {
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

        /** 可参考{@link Promise}中的该方法 */
        public final boolean claim() {
            Executor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (e == null) {
                return true;
            }
            return tryInline(this, e, options);
        }

        private boolean tryIncrementRid(int reentryId) {
            return VH_RID.compareAndSet(this, reentryId, reentryId + 1);
        }

        public CancelTokenSource tryFire(int mode) {
            outer:
            {
                // 同步Fire时，必须先竞争Action - 如果竞争失败，需要等待Close调用结束
                if (mode <= 0 && !tryIncrementRid(fireId)) {
                    while (rid < fireId + 2) { // 等待close完毕
                        LockSupport.parkNanos(1);
                    }
                    break outer;
                }
                if (FutureUtils.isCancelRequested(ctx, options)) {
                    break outer;
                }
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                assert action != null;
                fireNow(source, options, action, ctx);
            }
            outer:
            POOL.release(this);
            return null;
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
                        action.onCancelRequested(source);
                    }
                    case TYPE_TRANSFER -> {
                        // 这里本来有一个递归优化，为简化逻辑删除了
                        ICancelTokenSource action = (ICancelTokenSource) rawAction;
                        action.cancel(source.code);
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
        public final void close(int reentryId) {
            if (tryIncrementRid(reentryId)) {
                // 这里只释放action资源
                action = null;
                ctx = null;
                // 更新为+2表示关闭完毕
                rid = reentryId + 2;
            }
        }
    }
    // endregion
}