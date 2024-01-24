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

import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.*;
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

    private static final ScheduledThreadPoolExecutor delayer;

    static {
        delayer = new ScheduledThreadPoolExecutor(1, new DefaultThreadFactory("CancelTokenSource.Delayer", true));
        delayer.setRemoveOnCancelPolicy(true);
    }

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
            ICancelToken.checkCode(code);
            VH_CODE.setRelease(this, code);
        }
    }

    @Override
    public ICancelToken asReadonly() {
        return new ReadonlyCancelToken(this);
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
    public int cancel(int cancelCode) {
        ICancelToken.checkCode(cancelCode);
        int preCode = internalCancel(cancelCode);
        if (preCode != 0) {
            return preCode;
        }
        postComplete(this);
        return 0;
    }

    /** 使用默认原因取消 */
    @Override
    public int cancel() {
        return cancel(REASON_DEFAULT);
    }

    /**
     * 该方法主要用于兼容JDK
     *
     * @param mayInterruptIfRunning 是否可以中断目标线程；注意该参数由任务自身处理，且任务监听了取消信号才有用
     */
    public int cancel(boolean mayInterruptIfRunning) {
        return cancel(mayInterruptIfRunning
                ? (REASON_DEFAULT & MASK_INTERRUPT)
                : REASON_DEFAULT);
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
            Canceller canceller = new Canceller(this, cancelCode);
            canceller.future = executor.schedule(canceller, delay, timeUnit);
            // jdk的scheduler不会响应取消令牌，我们通过Future及时取消定时任务 -- 未来更换实现后可避免
            this.thenAccept(canceller);
        }
    }

    private static class Canceller implements Runnable, Consumer<ICancelToken> {

        final CancelTokenSource source;
        final int cancelCode;
        ScheduledFuture<?> future;

        private Canceller(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        @Override
        public void run() {
            source.cancel(cancelCode);
        }

        @Override
        public void accept(ICancelToken cancelToken) {
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
    public boolean isCancelling() {
        return code != 0;
    }

    @Override
    public int reason() {
        return ICancelToken.reason(code);
    }

    @Override
    public int degree() {
        return ICancelToken.degree(code);
    }

    @Override
    public boolean isInterruptible() {
        return ICancelToken.isInterruptible(code);
    }

    @Override
    public boolean isWithoutRemove() {
        return ICancelToken.isWithoutRemove(code);
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
        return uniAccept1(null, action, options);
    }

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action) {
        return uniAccept1(null, action, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept1(executor, action, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept1(executor, action, options);
    }

    private IRegistration uniAccept1(Executor executor, Consumer<? super ICancelToken> action,
                                     int options) {
        Objects.requireNonNull(action);
        if (isCancelling()) {
            UniAccept1.fireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniAccept1(executor, options, this, action);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // region uni-accept-ctx

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        return uniAccept2(null, action, ctx, options);
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        return uniAccept2(null, action, ctx, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept2(executor, action, ctx, 0);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept2(executor, action, ctx, options);
    }

    private IRegistration uniAccept2(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action,
                                     IContext ctx, int options) {
        Objects.requireNonNull(action);
        if (ctx == null) ctx = IContext.NONE;
        if (isCancelling()) {
            UniAccept2.fireNow(this, action, ctx);
            return TOMBSTONE;
        }
        Completion completion = new UniAccept2(executor, options, this, action, ctx);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // region uni-run

    @Override
    public IRegistration thenRun(Runnable action, int options) {
        return uniRun1(null, action, options);
    }

    @Override
    public IRegistration thenRun(Runnable action) {
        return uniRun1(null, action, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action) {
        Objects.requireNonNull(executor, "executor");
        return uniRun1(executor, action, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniRun1(executor, action, options);
    }

    private IRegistration uniRun1(Executor executor, Runnable action, int options) {
        Objects.requireNonNull(action);
        if (isCancelling()) {
            UniRun1.fireNow(action);
            return TOMBSTONE;
        }
        Completion completion = new UniRun1(executor, options, this, action);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // region uni-run-ctx

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx, int options) {
        return uniRun2(null, action, ctx, options);
    }

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx) {
        return uniRun2(null, action, ctx, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        Objects.requireNonNull(executor, "executor");
        return uniRun2(executor, action, ctx, 0);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniRun2(executor, action, ctx, options);
    }

    private IRegistration uniRun2(Executor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        Objects.requireNonNull(action);
        if (ctx == null) ctx = IContext.NONE;
        if (isCancelling()) {
            UniRun2.fireNow(action, ctx);
            return TOMBSTONE;
        }
        Completion completion = new UniRun2(executor, options, this, action, ctx);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // region uni-notify

    @Override
    public IRegistration thenNotify(CancelTokenListener action, int options) {
        return uniNotify(null, action, options);
    }

    @Override
    public IRegistration thenNotify(CancelTokenListener action) {
        return uniNotify(null, action, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, 0);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniNotify(executor, action, options);
    }

    private IRegistration uniNotify(Executor executor, CancelTokenListener action, int options) {
        if (isCancelling()) {
            UniNotify.fireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniNotify(executor, options, this, action);
        return pushCompletion(completion) ? completion : TOMBSTONE;
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
        if (isCancelling()) {
            UniTransferTo.fireNow(this, Promise.SYNC, child);
            return TOMBSTONE;
        }
        Completion completion = new UniTransferTo(executor, options, this, child);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // endregion

    // region core

    /** @return preCode */
    private int internalCancel(int cancelCode) {
//        assert cancelCode != 0;
        return (int) VH_CODE.compareAndExchange(this, 0, cancelCode);
    }

    /** @return 是否压栈成功 */
    private boolean pushCompletion(Completion newHead) {
        if (isCancelling()) {
            newHead.tryFire(Promise.SYNC);
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
        newHead.tryFire(Promise.SYNC);
        return false;
    }

    /** @return newestHead */
    private Completion removeClosedNode(Completion expectedHead) {
        // 无需循环尝试，因为每个线程的逻辑是一样的
        Completion next = expectedHead.next;
        while (next != null && next.action == TOMBSTONE) {
            next = next.next;
        }
        Completion realHead = (Completion) VH_STACK.compareAndExchange(this, expectedHead, next);
        return realHead == expectedHead ? next : realHead;
    }

    private static void postComplete(CancelTokenSource source) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = clearListeners(source, next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                curr.next = null; // help gc

                source = curr.tryFire(Promise.NESTED);
                if (source != null) {
                    continue outer;
                }
            }
            break;
        }
    }

    private static Completion clearListeners(CancelTokenSource source, Completion onto) {
        Completion head;
        do {
            head = source.stack;
            if (head == TOMBSTONE) {
                return onto;
            }
        } while (!VH_STACK.compareAndSet(source, head, TOMBSTONE));

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            if (tmpHead.action == TOMBSTONE) {
                continue; // 跳过被删除节点
            }

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    // endregion

    // region internal

    private static final VarHandle VH_CODE;
    private static final VarHandle VH_STACK;
    private static final VarHandle VH_ACTION;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_CODE = l.findVarHandle(CancelTokenSource.class, "code", int.class);
            VH_STACK = l.findVarHandle(CancelTokenSource.class, "stack", Completion.class);
            VH_ACTION = l.findVarHandle(Completion.class, "action", Object.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    private static final Executor CLAIMED = Runnable::run;

    private static boolean submit(Completion completion, Executor e, int options) {
        // 尝试内联
        if (TaskOption.isEnabled(options, TaskOption.STAGE_TRY_INLINE)
                && e instanceof SingleThreadExecutor eventLoop
                && eventLoop.inEventLoop()) {
            return true;
        }
        // 判断是否需要传递选项
        if (options != 0
                && !TaskOption.isEnabled(options, TaskOption.STAGE_NON_TRANSITIVE)
                && e instanceof IExecutor exe) {
            exe.execute(completion, options);
        } else {
            e.execute(completion);
        }
        return false;
    }

    /** 不需要复用对象的情况下无需分配唯一id */
    private static abstract class Completion implements IRegistration, Runnable {

        /** 非volatile，由栈顶的cas更新保证可见性 */
        Completion next;

        Executor executor;
        int options;
        CancelTokenSource source;
        /**
         * 用户回调
         * 1.通知和清理时置为{@link #TOMBSTONE}
         * 2.子类在执行action之前需要调用{@link #popAction()}竞争。
         */
        volatile Object action;

        public Completion() {
            source = null;
        }

        public Completion(Executor executor, int options, CancelTokenSource source, Object action) {
            this.executor = executor;
            this.options = options;
            this.source = source;
            VH_ACTION.setRelease(this, action);
        }

        @Override
        public final void run() {
            tryFire(Promise.ASYNC);
        }

        public abstract CancelTokenSource tryFire(int mode);

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
            return submit(this, e, options);
        }

        /**
         * 删除action
         *
         * @return 如果action已被删除则返回null，否则返回绑定的action
         */
        @Nullable
        protected final Object popAction() {
            Object action = this.action;
            if (action == TOMBSTONE) { // 已被取消
                return null;
            }
            if (action == VH_ACTION.compareAndExchange(this, action, TOMBSTONE)) {
                return action;
            }
            return null; // 竞争失败-被取消或通知
        }

        @Override
        public final void close() {
            Object action = popAction();
            if (action == null) {
                return;
            }
            CancelTokenSource source = this.source;
            if (this == source.stack) {
                source.removeClosedNode(this);
            }
            clear();
        }

        /** 注意：不能修改{@link #action}的引用 */
        protected void clear() {
            executor = null;
            source = null;
        }

    }
    // endregion

    private static final Completion TOMBSTONE = new Completion() {
        @Override
        public CancelTokenSource tryFire(int mode) {
            return null;
        }
    };

    private static class UniAccept1 extends Completion {

        public UniAccept1(Executor executor, int options, CancelTokenSource source,
                          Consumer<? super ICancelToken> action) {
            super(executor, options, source, action);
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                @SuppressWarnings("unchecked") var action = (Consumer<? super ICancelToken>) popAction();
                if (action == null) {
                    return null;
                }
                action.accept(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAccept1 caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(CancelTokenSource source, Consumer<? super ICancelToken> action) {
            try {
                action.accept(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAccept1 caught an exception");
            }
        }
    }

    private static class UniAccept2 extends Completion {

        IContext ctx;

        public UniAccept2(Executor executor, int options, CancelTokenSource source,
                          BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
            super(executor, options, source, action);
            this.ctx = ctx;
        }

        @Override
        protected void clear() {
            super.clear();
            ctx = null;
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            tryComplete:
            try {
                if (ctx.cancelToken().isCancelling()) {
                    break tryComplete;
                }
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                @SuppressWarnings("unchecked") var action = (BiConsumer<? super IContext, ? super ICancelToken>) popAction();
                if (action == null) {
                    return null;
                }
                action.accept(ctx, source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAccept2 caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(CancelTokenSource source,
                            BiConsumer<? super IContext, ? super ICancelToken> action,
                            IContext ctx) {
            try {
                action.accept(ctx, source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAccept2 caught an exception");
            }
        }

    }

    private static class UniRun1 extends Completion {

        public UniRun1(Executor executor, int options, CancelTokenSource source,
                       Runnable action) {
            super(executor, options, source, action);
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                Runnable action = (Runnable) popAction();
                if (action == null) {
                    return null;
                }
                action.run();
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRun1 caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(Runnable action) {
            try {
                action.run();
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRun1 caught an exception");
            }
        }
    }

    private static class UniRun2 extends Completion {

        IContext ctx;

        public UniRun2(Executor executor, int options, CancelTokenSource source,
                       Consumer<? super IContext> action, IContext ctx) {
            super(executor, options, source, action);
            this.ctx = ctx;
        }

        @Override
        protected void clear() {
            super.clear();
            ctx = null;
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            tryComplete:
            try {
                if (ctx.cancelToken().isCancelling()) {
                    break tryComplete;
                }
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                @SuppressWarnings("unchecked") var action = (Consumer<? super IContext>) popAction();
                if (action == null) {
                    return null;
                }
                action.accept(ctx);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRun2 caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(Consumer<? super IContext> action,
                            IContext ctx) {
            try {
                action.accept(ctx);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRun2 caught an exception");
            }
        }
    }

    private static class UniNotify extends Completion {

        public UniNotify(Executor executor, int options, CancelTokenSource source,
                         CancelTokenListener action) {
            super(executor, options, source, action);
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                CancelTokenListener action = (CancelTokenListener) popAction();
                if (action == null) {
                    return null;
                }
                action.onCancelRequest(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniNotify caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(CancelTokenSource source, CancelTokenListener action) {
            try {
                action.onCancelRequest(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniNotify caught an exception");
            }
        }
    }

    private static class UniTransferTo extends Completion {

        public UniTransferTo(Executor executor, int options, CancelTokenSource source,
                             ICancelTokenSource action) {
            super(executor, options, source, action);
        }

        @Override
        public CancelTokenSource tryFire(int mode) {
            CancelTokenSource output;
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                ICancelTokenSource child = (ICancelTokenSource) popAction();
                if (child == null) {
                    return null;
                }
                output = fireNow(source, mode, child);
            } catch (Throwable ex) {
                output = null;
                FutureLogger.logCause(ex, "UniTransferTo caught an exception");
            }
            // help gc
            clear();
            return output;
        }

        static CancelTokenSource fireNow(CancelTokenSource source, int mode,
                                         ICancelTokenSource child) {
            if (!(child instanceof CancelTokenSource childSource)) {
                child.cancel(source.code);
                return null;
            }
            if (childSource.internalCancel(source.code) == 0) {
                if (mode < 0) { // 嵌套模式
                    return childSource;
                }
                postComplete(childSource);
                return null;
            }
            return null;
        }
    }
}