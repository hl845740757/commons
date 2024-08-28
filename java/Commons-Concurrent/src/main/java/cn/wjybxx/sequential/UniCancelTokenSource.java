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

package cn.wjybxx.sequential;

import cn.wjybxx.base.IRegistration;
import cn.wjybxx.base.annotation.Beta;
import cn.wjybxx.base.concurrent.BetterCancellationException;
import cn.wjybxx.base.concurrent.CancelCodeBuilder;
import cn.wjybxx.base.concurrent.CancelCodes;
import cn.wjybxx.concurrent.*;

import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 单线程版的取消令牌。
 *
 * <h3>实现说明</h3>
 * 1. 去除了{@link #code}等的volatile操作，变更为普通字段。
 * 2. 默认时间单位为毫秒。
 * 3. {@link #tryInline}对executor的检测调整
 *
 * @author wjybxx
 * date - 2024/1/8
 */
@Beta
@NotThreadSafe
public final class UniCancelTokenSource implements ICancelTokenSource {

    /**
     * 取消码
     * - 0表示未收到取消信号
     * - 非0表示收到取消信号
     */
    private int code;
    /**
     * 当前对象上的所有监听器，使用栈方式存储
     * 如果{@code stack}为{@link #TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
     */
    private Completion stack;

    /** 用户线程 -- 如果为null，将禁止延迟取消操作 */
    private UniScheduledExecutor executor;

    public UniCancelTokenSource() {

    }

    public UniCancelTokenSource(int code) {
        this(null, code);
    }

    public UniCancelTokenSource(UniScheduledExecutor executor) {
        this(executor, 0);
    }

    public UniCancelTokenSource(UniScheduledExecutor executor, int code) {
        this.executor = executor;
        if (code != 0) {
            this.code = CancelCodes.checkCode(code);
        }
    }

    public UniScheduledExecutor getExecutor() {
        return executor;
    }

    public UniCancelTokenSource setExecutor(UniScheduledExecutor executor) {
        this.executor = executor;
        return this;
    }

    @Override
    public boolean canBeCancelled() {
        return true;
    }

    @Override
    public UniCancelTokenSource newInstance(boolean copyCode) {
        return new UniCancelTokenSource(executor, copyCode ? code : 0);
    }

    @Override
    public UniCancelTokenSource newInstance() {
        return new UniCancelTokenSource(executor);
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
    public boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

    /**
     * 在一段时间后发送取消命令
     *
     * @throws IllegalStateException 如果未绑定executor
     */
    public void cancelAfter(int cancelCode, long millisecondsDelay) {
        cancelAfter(cancelCode, millisecondsDelay, TimeUnit.MILLISECONDS, executor);
    }

    @Override
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit) {
        cancelAfter(cancelCode, delay, timeUnit, executor);
    }

    /** @param executor 用于延迟调度的executor */
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit, UniScheduledExecutor executor) {
        if (executor == null) throw new IllegalArgumentException("delayer is null");
        if (this.code == 0) {
            Canceller canceller = new Canceller(this, cancelCode);
            executor.scheduleAction(canceller, delay, timeUnit, this);
            // executor会自动监听延时任务的cancelToken
        }
    }

    private static class Canceller implements Runnable {

        final UniCancelTokenSource source;
        final int cancelCode;

        private Canceller(UniCancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        @Override
        public void run() {
            source.cancel(cancelCode);
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
        if (isCancelling() && executor == null) {
            UniAccept.fireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniAccept(executor, options, this, action);
        return pushCompletion(completion) ? completion : TOMBSTONE;
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
        if (isCancelling() && executor == null) {
            UniAcceptCtx.fireNow(this, action, ctx);
            return TOMBSTONE;
        }
        Completion completion = new UniAcceptCtx(executor, options, this, action, ctx);
        return pushCompletion(completion) ? completion : TOMBSTONE;
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
        if (isCancelling() && executor == null) {
            UniRun.fireNow(action);
            return TOMBSTONE;
        }
        Completion completion = new UniRun(executor, options, this, action);
        return pushCompletion(completion) ? completion : TOMBSTONE;
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
        if (isCancelling() && executor == null) {
            UniRunCtx.fireNow(action, ctx);
            return TOMBSTONE;
        }
        Completion completion = new UniRunCtx(executor, options, this, action, ctx);
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
        if (isCancelling() && executor == null) {
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
        if (isCancelling() && executor == null) {
            UniTransferTo.fireNow(this, SYNC, child);
            return TOMBSTONE;
        }
        Completion completion = new UniTransferTo(executor, options, this, child);
        return pushCompletion(completion) ? completion : TOMBSTONE;
    }

    // endregion

    // endregion

    // region core

    private static final int SYNC = UniPromise.SYNC;
    private static final int ASYNC = UniPromise.ASYNC;
    private static final int NESTED = UniPromise.NESTED;
    private static final Executor CLAIMED = UniPromise.CLAIMED;

    /** @return preCode */
    private int internalCancel(int cancelCode) {
//        assert cancelCode != 0;
        int preCode = this.code;
        if (preCode == 0) {
            this.code = cancelCode;
            return 0;
        }
        return preCode;
    }

    /** 单线程版实现即时删除节点 -- 未实现为双向链表，我们认为主动关闭监听的情况较少 */
    private void removeNode(Completion node) {
        Completion curr = stack;
        if (curr == null || curr == TOMBSTONE) {
            return;
        }
        Completion prev = null;
        while (curr != null && curr != node) {
            prev = curr;
            curr = curr.next;
        }
        if (curr == null) {
            return;
        }
        if (prev == null) {
            stack = node.next;
        } else {
            prev.next = curr.next;
        }
    }

    /** @return 是否压栈成功 */
    private boolean pushCompletion(Completion newHead) {
        if (isCancelling()) {
            newHead.tryFire(SYNC);
            return false;
        }
        newHead.next = this.stack;
        this.stack = newHead;
        return true;
    }

    private static void postComplete(UniCancelTokenSource source) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
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

    private static Completion clearListeners(UniCancelTokenSource source, Completion onto) {
        Completion head = source.stack;
        if (head == TOMBSTONE) {
            return onto;
        }
        source.stack = TOMBSTONE;

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
        if (TaskOptions.isEnabled(options, TaskOptions.STAGE_TRY_INLINE)) {
            if (e instanceof UniExecutorService) { // uni-executor支持
                return true;
            }
            if (e instanceof SingleThreadExecutor eventLoop
                    && eventLoop.inEventLoop()) {
                return true;
            }
        }
        // 判断是否需要传递选项
        if (options != 0
                && TaskOptions.isEnabled(options, TaskOptions.STAGE_PROPAGATE_OPTIONS)
                && e instanceof IExecutor exe) {
            exe.execute(completion);
        } else {
            completion.setOptions(0);
            e.execute(completion);
        }
        return false;
    }

    // endregion

    /** 不需要复用对象的情况下无需分配唯一id */
    private static abstract class Completion implements IRegistration, ITask {

        Completion next;
        UniCancelTokenSource source;

        Executor executor;
        int options;
        /**
         * 用户回调
         * 1.通知和清理时置为{@link #TOMBSTONE}
         * 2.子类在执行action之前需要调用{@link #popAction()}竞争。
         */
        Object action;

        public Completion() {
            source = null;
        }

        public Completion(Executor executor, int options, UniCancelTokenSource source, Object action) {
            this.executor = executor;
            this.options = options;
            this.source = source;
            this.action = action;
        }

        @Override
        public int getOptions() {
            return options;
        }

        public void setOptions(int options) {
            this.options = options;
        }

        @Override
        public final void run() {
            tryFire(ASYNC);
        }

        public abstract UniCancelTokenSource tryFire(int mode);

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
            this.action = TOMBSTONE;
            return action;
        }

        @Override
        public final void close() {
            Object action = popAction();
            if (action == null) {
                return;
            }
            source.removeNode(this);
            clear();
        }

        /** 注意：不能修改{@link #action}的引用 */
        protected void clear() {
            source = null;
            next = null;
            executor = null;
        }

    }
    // endregion

    private static final Completion TOMBSTONE = new Completion() {
        @Override
        public UniCancelTokenSource tryFire(int mode) {
            return null;
        }
    };

    private static class UniAccept extends Completion {

        public UniAccept(Executor executor, int options, UniCancelTokenSource source,
                         Consumer<? super ICancelToken> action) {
            super(executor, options, source, action);
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
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
                FutureLogger.logCause(ex, "UniAccept caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(UniCancelTokenSource source, Consumer<? super ICancelToken> action) {
            try {
                action.accept(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAccept caught an exception");
            }
        }
    }

    private static class UniAcceptCtx extends Completion {

        Object ctx;

        public UniAcceptCtx(Executor executor, int options, UniCancelTokenSource source,
                            BiConsumer<? super ICancelToken, Object> action, Object ctx) {
            super(executor, options, source, action);
            this.ctx = ctx;
        }

        @Override
        protected void clear() {
            super.clear();
            ctx = null;
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                @SuppressWarnings("unchecked") var action = (BiConsumer<? super ICancelToken, Object>) popAction();
                if (action == null) {
                    return null;
                }
                if (!UniPromise.isCancelling(ctx, options)) {
                    action.accept(source, ctx);
                }
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAcceptCtx caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(UniCancelTokenSource source,
                            BiConsumer<? super ICancelToken, Object> action, Object ctx) {
            try {
                action.accept(source, ctx);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniAcceptCtx caught an exception");
            }
        }

    }

    private static class UniRun extends Completion {

        public UniRun(Executor executor, int options, UniCancelTokenSource source,
                      Runnable action) {
            super(executor, options, source, action);
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
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
                FutureLogger.logCause(ex, "UniRun caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(Runnable action) {
            try {
                action.run();
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRun caught an exception");
            }
        }
    }

    private static class UniRunCtx extends Completion {

        Object ctx;

        public UniRunCtx(Executor executor, int options, UniCancelTokenSource source,
                         Consumer<Object> action, Object ctx) {
            super(executor, options, source, action);
            this.ctx = ctx;
        }

        @Override
        protected void clear() {
            super.clear();
            ctx = null;
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                @SuppressWarnings("unchecked") var action = (Consumer<Object>) popAction();
                if (action == null) {
                    return null;
                }
                if (!UniPromise.isCancelling(ctx, options)) {
                    action.accept(ctx);
                }
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRunCtx caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(Consumer<Object> action, Object ctx) {
            try {
                action.accept(ctx);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniRunCtx caught an exception");
            }
        }
    }

    private static class UniNotify extends Completion {

        public UniNotify(Executor executor, int options, UniCancelTokenSource source,
                         CancelTokenListener action) {
            super(executor, options, source, action);
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
            try {
                if (mode <= 0 && !claim()) {
                    return null; // 下次执行
                }
                CancelTokenListener action = (CancelTokenListener) popAction();
                if (action == null) {
                    return null;
                }
                action.onCancelRequested(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniNotify caught an exception");
            }
            // help gc
            clear();
            return null;
        }

        static void fireNow(UniCancelTokenSource source, CancelTokenListener action) {
            try {
                action.onCancelRequested(source);
            } catch (Throwable ex) {
                FutureLogger.logCause(ex, "UniNotify caught an exception");
            }
        }
    }

    private static class UniTransferTo extends Completion {

        public UniTransferTo(Executor executor, int options, UniCancelTokenSource source,
                             ICancelTokenSource action) {
            super(executor, options, source, action);
        }

        @Override
        public UniCancelTokenSource tryFire(int mode) {
            UniCancelTokenSource output;
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

        static UniCancelTokenSource fireNow(UniCancelTokenSource source, int mode,
                                            ICancelTokenSource child) {
            if (!(child instanceof UniCancelTokenSource childSource)) {
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