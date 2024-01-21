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

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;
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
    private volatile CallbackNode stack;

    public CancelTokenSource() {

    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            checkCode(code);
            VH_CODE.setRelease(this, code);
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
    @Override
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
        cancelAfter(cancelCode, millisecondsDelay, TimeUnit.MILLISECONDS);
    }

    /**
     * 在一段时间后发送取消命令
     * (将由默认的调度器调度)
     */
    @Override
    public void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit) {
        if (this.code == 0) {
            Canceller canceller = new Canceller(this, cancelCode);
            canceller.future = delayer.schedule(canceller, delay, timeUnit);
            // jdk的scheduler不会响应取消令牌，我们通过Future及时取消定时任务 -- 未来更换实现后可避免
            register(canceller);
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
        CallbackNode callbackNode = new CallbackNode(nextId(), this, TYPE_TYPED, action);
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

    private static void notifyListener(CancelTokenSource source,
                                       Consumer<? super ICancelToken> action) {
        try {
            action.accept(source);
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "CancelTokenListener caught exception");
        }
    }

    private static void notifyListener(CancelTokenSource source,
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
        return (int) VH_CODE.compareAndExchange(this, 0, cancelCode);
    }

    /** @return 是否压栈成功 */
    private boolean pushCompletion(CallbackNode newHead) {
        if (isCancelling()) {
            newHead.tryFire(Promise.SYNC);
            return false;
        }
        CallbackNode expectedHead = stack;
        CallbackNode realHead;
        while (expectedHead != TOMBSTONE) {
            newHead.next = expectedHead;
            realHead = (CallbackNode) VH_STACK.compareAndExchange(this, expectedHead, newHead);
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
    private CallbackNode removeClosedNode(CallbackNode expectedHead) {
        // 无需循环尝试，因为每个线程的逻辑是一样的
        CallbackNode next = expectedHead.next;
        while (next != null && next.action == TOMBSTONE) {
            next = next.next;
        }
        CallbackNode realHead = (CallbackNode) VH_STACK.compareAndExchange(this, expectedHead, next);
        return realHead == expectedHead ? next : realHead;
    }

    private static void postComplete(CancelTokenSource source) {
        CallbackNode next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = clearListeners(source, next);

            while (next != null) {
                CallbackNode curr = next;
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

    private static CallbackNode clearListeners(CancelTokenSource source, CallbackNode onto) {
        CallbackNode head;
        do {
            head = source.stack;
            if (head == TOMBSTONE) {
                return onto;
            }
        } while (!VH_STACK.compareAndSet(source, head, TOMBSTONE));

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

    // region

    private static final VarHandle VH_CODE;
    private static final VarHandle VH_STACK;
    private static final VarHandle VH_ACTION;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_CODE = l.findVarHandle(CancelTokenSource.class, "code", int.class);
            VH_STACK = l.findVarHandle(CancelTokenSource.class, "stack", CallbackNode.class);
            VH_ACTION = l.findVarHandle(CallbackNode.class, "action", Object.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
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

        /** 非volatile，由栈顶的cas更新保证可见性 */
        CallbackNode next;

        /** 唯一id */
        final long id;
        /** 暂非final，暂不允许用户访问 */
        CancelTokenSource source;

        /** 任务的类型 -- 不想过多的子类实现 */
        int type;
        /** 用户回调 -- 通知和清理时置为{@link #TOMBSTONE} */
        volatile Object action;

        public CallbackNode() {
            id = 0; // TOMBSTONE
            source = null;
        }

        public CallbackNode(long id, CancelTokenSource source, int type, Object action) {
            this.id = id;
            this.source = source;
            this.type = type;
            VH_ACTION.setRelease(this, action);
        }

        public CancelTokenSource tryFire(int mode) {
            Object action = this.action;
            if (action == TOMBSTONE) {
                return null;
            }
            if (!casAction2Tombstone(action)) {
                return null; // 当前节点被取消
            }
            CancelTokenSource source = this.source;
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

        private static CancelTokenSource notifyChild(CancelTokenSource source, int mode, ICancelTokenSource child) {
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

        private boolean casAction2Tombstone(Object action) {
            return action == VH_ACTION.compareAndExchange(this, action, TOMBSTONE);
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
                CancelTokenSource source = this.source;
                if (this == source.stack) {
                    source.removeClosedNode(this);
                }
                this.source = null;
            }
        }
    }

}