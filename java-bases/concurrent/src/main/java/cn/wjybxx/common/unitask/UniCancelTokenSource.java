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

package cn.wjybxx.common.unitask;

import cn.wjybxx.base.annotation.Beta;
import cn.wjybxx.common.concurrent.*;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.Consumer;

/**
 * 单线程版的取消令牌。
 *
 * <h3>实现说明</h3>
 * 1. 去除了{@link #code}等的volatile操作，变更为普通字段。
 * 2. 默认时间单位为毫秒。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
@Beta
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
    private CallbackNode stack;

    /** 用户线程 */
    private final UniScheduledExecutor executor;

    public UniCancelTokenSource(UniScheduledExecutor executor) {
        this.executor = Objects.requireNonNull(executor);
    }

    public UniCancelTokenSource(UniScheduledExecutor executor, int code) {
        this.executor = Objects.requireNonNull(executor);
        if (code != 0) {
            checkCode(code);
            this.code = code;
        }
    }

    private static void checkCode(int code) {
        if (ICancelToken.reason(code) == 0) {
            throw new IllegalArgumentException("reason == 0");
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
    public int cancel(int cancelCode) {
        checkCode(cancelCode);
        int preCode = internalCancel(cancelCode);
        if (preCode != 0) {
            return preCode;
        }
        postComplete(this);
        return 0;
    }

    /** 使用默认原因取消 */
    public int cancel() {
        return cancel(REASON_DEFAULT);
    }

    /**
     * 在一段时间后发送取消命令
     * (将由默认的调度器调度)
     */
    public void cancelAfter(int cancelCode, long millisecondsDelay) {
        if (executor == null) {
            throw new IllegalStateException("delayer is not set");
        }
        if (this.code == 0) {
            Canceller canceller = new Canceller(this, cancelCode);
            executor.scheduleAction(canceller, canceller, millisecondsDelay);
            // executor会自动监听延时任务的cancelToken
        }
    }

    @Override
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit) {
        long millisecondsDelay = timeUnit.toMillis(delay);
        cancelAfter(cancelCode, millisecondsDelay);
    }

    private static class Canceller implements Consumer<Object>, IContext {

        final UniCancelTokenSource source;
        final int cancelCode;

        private Canceller(UniCancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        @Override
        public void accept(Object tokenOrSelf) {
            if (tokenOrSelf == this) {
                source.cancel(cancelCode);
            }
        }

        @Nonnull
        @Override
        public ICancelToken cancelToken() {
            return source;
        }

        @Override
        public Object blackboard() {
            return null;
        }

        @Override
        public Object sharedProps() {
            return null;
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

    @Override
    public IRegistration register(Consumer<? super ICancelToken> action) {
        Objects.requireNonNull(action, "action");
        if (code != 0) {
            notifyListener(this, action);
            return TOMBSTONE;
        }
        CallbackNode callbackNode = new CallbackNode(nextId(), this, TYPE_CONSUMER, action);
        if (pushCompletion(callbackNode)) {
            return callbackNode;
        }
        return TOMBSTONE;
    }

    @Override
    public IRegistration registerRun(Runnable action) {
        Objects.requireNonNull(action, "action");
        if (code != 0) {
            notifyListener(action);
            return TOMBSTONE;
        }
        CallbackNode callbackNode = new CallbackNode(nextId(), this, TYPE_RUNNABLE, action);
        if (pushCompletion(callbackNode)) {
            return callbackNode;
        }
        return TOMBSTONE;
    }

    @Override
    public IRegistration registerTyped(CancelTokenListener action) {
        Objects.requireNonNull(action, "action");
        if (code != 0) {
            notifyListener(this, action);
            return TOMBSTONE;
        }
        CallbackNode callbackNode = new CallbackNode(nextId(), this, TYPE_RUNNABLE, action);
        if (pushCompletion(callbackNode)) {
            return callbackNode;
        }
        return TOMBSTONE;
    }

    @Override
    public IRegistration registerChild(ICancelTokenSource child) {
        Objects.requireNonNull(child, "child");
        if (child == this) {
            throw new IllegalArgumentException("register self as child");
        }
        int code = this.code;
        if (code != 0) {
            child.cancel(code);
            return TOMBSTONE;
        }
        CallbackNode callbackNode = new CallbackNode(nextId(), this, TYPE_CHILD, child);
        if (pushCompletion(callbackNode)) {
            return callbackNode;
        }
        return TOMBSTONE;
    }

    private static void notifyListener(Runnable action) {
        try {
            action.run();
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "CancelTokenListener caught exception");
        }
    }

    private static void notifyListener(UniCancelTokenSource source,
                                       Consumer<? super ICancelToken> action) {
        try {
            action.accept(source);
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "CancelTokenListener caught exception");
        }
    }

    private static void notifyListener(UniCancelTokenSource source,
                                       CancelTokenListener action) {
        try {
            action.onCancelRequest(source);
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "CancelTokenListener caught exception");
        }
    }

    // endregion

    // region core

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

    /** @return 是否压栈成功 */
    private boolean pushCompletion(CallbackNode newHead) {
        if (isCancelling()) {
            newHead.tryFire(UniPromise.SYNC);
            return false;
        }
        CallbackNode expectedHead = stack;
        if (expectedHead.action == TOMBSTONE) {
            expectedHead = removeClosedNode(expectedHead);
        }
        newHead.next = expectedHead;
        stack = newHead;
        return true;
    }

    /** @return newestHead */
    private CallbackNode removeClosedNode(CallbackNode expectedHead) {
        CallbackNode next = expectedHead.next;
        while (next != null && next.action == TOMBSTONE) {
            next = next.next;
        }
        stack = next;
        return next;
    }

    private static void postComplete(UniCancelTokenSource source) {
        CallbackNode next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = clearListeners(source, next);

            while (next != null) {
                CallbackNode curr = next;
                next = next.next;
                curr.next = null; // help gc

                source = curr.tryFire(UniPromise.NESTED);
                if (source != null) {
                    continue outer;
                }
            }
            break;
        }
    }

    private static CallbackNode clearListeners(UniCancelTokenSource source, CallbackNode onto) {
        CallbackNode head = source.stack;
        if (head == TOMBSTONE) {
            return onto;
        }

        CallbackNode ontoHead = onto;
        while (head != null) {
            CallbackNode tmpHead = head;
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

    /** {@link #register(Consumer)} */
    private static final int TYPE_CONSUMER = 0;
    /** {@link #registerRun(Runnable)} */
    private static final int TYPE_RUNNABLE = 1;
    /** {@link #registerTyped(CancelTokenListener)} */
    private static final int TYPE_TYPED = 2;
    /** {@link #registerChild(ICancelTokenSource)} */
    private static final int TYPE_CHILD = 3;


    /** 分配唯一id */
    private static final AtomicLong idAllocator = new AtomicLong(1);

    private static long nextId() {
        return idAllocator.getAndIncrement();
    }

    private static final CallbackNode TOMBSTONE = new CallbackNode();

    private static class CallbackNode implements IRegistration {

        CallbackNode next;

        /** 唯一id */
        final long id;
        /** 暂非final，暂不允许用户访问 */
        UniCancelTokenSource source;
        /** 任务的类型 -- 不想过多的子类实现 */
        int type;
        /** 用户回调 -- 通知和清理时置为{@link #TOMBSTONE} */
        Object action;

        public CallbackNode() {
            id = 0; // TOMBSTONE
            source = null;
        }

        public CallbackNode(long id, UniCancelTokenSource source, int type, Object action) {
            this.id = id;
            this.source = source;
            this.type = type;
            this.action = action;
        }

        public UniCancelTokenSource tryFire(int mode) {
            Object action = this.action;
            if (action == TOMBSTONE) {
                return null;
            }
            if (!casAction2Tombstone(action)) {
                return null; // 当前节点被取消
            }
            UniCancelTokenSource source = this.source;
            this.source = null;

            switch (type) {
                case TYPE_CONSUMER -> {
                    @SuppressWarnings("unchecked") var castAction = (Consumer<? super ICancelToken>) action;
                    notifyListener(source, castAction);
                    return null;
                }
                case TYPE_RUNNABLE -> {
                    Runnable castAction = (Runnable) action;
                    notifyListener(castAction);
                    return null;
                }
                case TYPE_CHILD -> {
                    ICancelTokenSource childSource = (ICancelTokenSource) action;
                    return notifyChild(source, mode, childSource);
                }
                case TYPE_TYPED -> {
                    CancelTokenListener listener = (CancelTokenListener) action;
                    notifyListener(source, listener);
                    return null;
                }
                default -> {
                    throw new AssertionError();
                }
            }
        }

        private static UniCancelTokenSource notifyChild(UniCancelTokenSource source, int mode, ICancelTokenSource child) {
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

        private boolean casAction2Tombstone(Object action) {
            if (this.action == action) {
                this.action = TOMBSTONE;
                return true;
            }
            return false;
        }

        @Override
        public void close() {
            if (this == TOMBSTONE) { // 较大概率
                return;
            }
            Object action = this.action;
            if (action == TOMBSTONE) {
                return;
            }
            if (casAction2Tombstone(action)) {
                UniCancelTokenSource source = this.source;
                if (this == source.stack) {
                    source.removeClosedNode(this);
                }
                this.source = null;
            }
        }
    }

}