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

import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;
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
     * @return 如果Token尚未被取消，则将其更新为取消状态并返回true；否则返回false。
     * @throws IllegalArgumentException 如果code小于等于0；或reason部分为0
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
            Completion.fireNow(this, TYPE_ACCEPT, action);
            return Registration.CLOSED;
        }
        return pushCompletion(new Completion(executor, options, this, TYPE_ACCEPT, action));
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
            Completion.fireNow(this, TYPE_RUN, action);
            return Registration.CLOSED;
        }
        return pushCompletion(new Completion(executor, options, this, TYPE_RUN, action));
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
        Objects.requireNonNull(listener, "listener");
        if (isRequested() && executor == null) {
            Completion.fireNow(this, TYPE_NOTIFY, listener);
            return Registration.CLOSED;
        }
        return pushCompletion(new Completion(executor, options, this, TYPE_NOTIFY, listener));
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
            // 对象不复用，reentryId不再有意义，固定为0
            registration = new Registration(newHead, 0);
        } finally {
            exitLock();
        }
        if (!registration.hasResource()) {
            newHead.tryFire(SYNC);
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
    private static final int TYPE_RUN = 1;
    private static final int TYPE_NOTIFY = 2;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private static final int MASK_TASK_TYPE = 0x0F;

    /**
     * 生命周期：
     * 1.close()会将action置空并尝试从监听器列表删除，之后的tryFire变为空操作；
     * 2.tryFire在执行用户回调前会将action消费置空，之后的close()变为空操作。
     */
    private static class Completion implements ITask, IPooledCloseable {

        /** 非volatile，可见性由{@link CancelTokenSource#lock}保证 */
        Completion prev;
        Completion next;

        /** 互斥锁 */
        int lock;

        CancelTokenSource source;
        Executor executor;
        int options; // 包含任务类型信息
        Object action;

        Completion(Executor executor, int options, CancelTokenSource source, int type, Object action) {
            // 去除用户的低位，记录type
            this.executor = executor;
            this.options = (options & (~TaskOptions.MASK_CTL_RESERVED)) | type;
            this.source = source;
            this.action = action;
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
            // 如果走到这里，action可能已被close清理，即已收到取消信号
            CancelTokenSource source;
            Executor executor;
            int options;
            Object action;

            enterLock();
            boolean fire; // 没有goto，我们使用bool标记
            try {
                options = this.options;
                action = this.action;
                // 如果已被关闭，则直接结束
                if (action == null) {
                    return;
                }
                source = this.source;
                executor = this.executor;

                // 如果是同步模式，需要claim -- 与Future的实现不同，我们在锁外提交任务以避免阻塞，但需要先标记
                if (mode <= 0 && !ExecutorUtils.isInlinable(executor, options)) {
                    this.executor = null;
                    fire = false;
                } else {
                    // 数据已拷贝到临时变量；消费action，之后的close将变为空操作
                    this.action = null;
                    fire = true;
                }
            } finally {
                exitLock();
            }
            // 在锁外执行回调和提交任务
            if (fire) {
                fireNow(source, options, action);
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
                                    int options, Object rawAction) {
            int type = options & MASK_TASK_TYPE;
            try {
                switch (type) {
                    case TYPE_ACCEPT -> {
                        Consumer<ICancelToken> action = (Consumer<ICancelToken>) rawAction;
                        action.accept(source);
                    }
                    case TYPE_RUN -> {
                        Runnable action = (Runnable) rawAction;
                        action.run();
                    }
                    case TYPE_NOTIFY -> {
                        ICancelTokenListener action = (ICancelTokenListener) rawAction;
                        action.onCancelRequested(source);
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
            // 对象不复用，reentryId不再有意义
            enterLock();
            try {
                return this.action == null;
            } finally {
                exitLock();
            }
        }

        @Override
        public final void close(long reentryId) {
            enterLock();
            try {
                // action为null表示已执行回调或已关闭
                if (this.action == null) {
                    return;
                }
                this.action = null;
                // 从监听器列表删除 -- 节点可能已被通知流程摘除（不在列表中），此时删除会安全失败
                source.removeCompletion(this);
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