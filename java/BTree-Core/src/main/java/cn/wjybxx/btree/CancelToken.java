/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.btree;

import cn.wjybxx.base.IPooledCloseable;
import cn.wjybxx.base.IRegistration;
import cn.wjybxx.base.Registration;
import cn.wjybxx.base.collection.SmallDynamicArray;
import cn.wjybxx.concurrent.*;

import java.util.ArrayDeque;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 行为树模块使用的取消令牌
 * 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
 * 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
 * 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
 * 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
 * 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
 *
 * @author wjybxx
 * date - 2024/7/14
 */
public class CancelToken implements ICancelTokenSource, ICancelTokenListener {

    /** 取消码 -- 0表示未收到信号 */
    private int code;
    /** 监听器列表 -- 通知期间可能会被重用 */
    private final SmallDynamicArray<ICancelTokenListener> listeners = new SmallDynamicArray<>(4);
    /** 用于检测复用 -- short应当足够 */
    private short reentryId;

    public CancelToken() {
    }

    public CancelToken(int code) {
        if (code != 0) {
            CancelCodes.checkCode(code);
        }
        this.code = code;
    }

    /** 收到其它地方的取消信号 -- 用户不应该调用该方法 */
    @Deprecated
    @Override
    public final void onCancelRequested(ICancelToken cancelToken, Object ctx) {
        cancel(cancelToken.cancelCode());
    }

    /** 创建一个同类型实例(默认只拷贝环境数据) */
    @Override
    public CancelToken newInstance() {
        return newInstance(false);
    }

    /**
     * 创建一个同类型实例(默认只拷贝环境数据)
     *
     * @param copyCode 是否拷贝当前取消码
     * @return 新实例
     */
    @Override
    public CancelToken newInstance(boolean copyCode) {
        return new CancelToken(copyCode ? code : 0);
    }

    /** 重置状态(行为树模块取消令牌需要复用) */
    public void reset() {
        reentryId++;
        code = 0;
        if (listeners.elementCount() == 0) {
            return;
        }
        // 需要将监听器归还到池
        listeners.beginItr();
        try {
            for (int idx = listeners.indexOf(null), len = listeners.length(); idx < len; idx++) {
                var listener = listeners.set(idx, null);
                if (listener instanceof Completion completion) {
                    releaseCompletion(completion);
                }
            }
        } finally {
            listeners.endItr();
        }
    }

    /** 重入id，允许外部捕获 */
    public final int getReentryId() {
        return reentryId;
    }

    /** 是否正在通知监听器 */
    protected final boolean isFiring() {
        return listeners.isIterating();
    }

    //region query

    @Override
    public final boolean canBeCancelled() {
        return true;
    }

    /** 取消码 */
    @Override
    public final int cancelCode() {
        return code;
    }

    /** 是否已收到取消信号 */
    @Override
    public final boolean isCancelRequested() {
        return code != 0;
    }

    /** 取消的原因 */
    @Override
    public final int reason() {
        return CancelCodes.getReason(code);
    }

    /** 取消的紧急程度 */
    public final int degree() {
        return CancelCodes.getDegree(code);
    }

    //endregion

    //region cancel

    @Override
    public final boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

    @Override
    public final boolean cancel(int cancelCode) {
        CancelCodes.checkCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            postComplete(this);
            return true;
        }
        return false;
    }

    private static void postComplete(CancelToken cancelToken) {
        SmallDynamicArray<ICancelTokenListener> listeners = cancelToken.listeners;
        if (listeners.length() == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        listeners.beginItr();
        try {
            for (int idx = 0, len = listeners.length(); idx < len; idx++) {
                var listener = listeners.set(idx, null);
                if (listener == null) {
                    continue;
                }
                try {
                    listener.onCancelRequested(cancelToken, null);
                } catch (Throwable e) {
                    Task.logger.info("listener caught exception", e);
                }
                // 在通知期间被Reset
                if (reentryId != cancelToken.reentryId) {
                    return;
                }
            }
        } finally {
            listeners.endItr();
        }
    }

    //endregion

    //region 监听器

    /** 添加监听器 */
    public final void addListener(ICancelTokenListener listener) {
        Objects.requireNonNull(listener);
        if (listener == this) throw new IllegalArgumentException("add self");
        if (code != 0) {
            try {
                listener.onCancelRequested(this, null);
            } catch (Throwable e) {
                Task.logger.info("listener caught exception", e);
            }
        } else {
            listeners.add(listener);
        }
    }

    /** 删除指定监听器 */
    public final boolean remListener(ICancelTokenListener listener) {
        return remListener(listener, false);
    }

    /**
     * 删除监听器
     * 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
     *
     * @param listener        要删除的监听器
     * @param firstOccurrence 是否强制正向查找删除
     * @return 存在匹配的监听器则返回true
     */
    public final boolean remListener(ICancelTokenListener listener, boolean firstOccurrence) {
        int index = firstOccurrence
                ? listeners.indexOfRef(listener)
                : listeners.lastIndexOfRef(listener);
        if (index < 0) {
            return false;
        }
        listeners.set(index, null);
        return true;
    }

    /** 查询是否存在给定的监听器 */
    public final boolean hasListener(ICancelTokenListener listener) {
        return listeners.containsRef(listener);
    }

    /** 监听器数量 */
    public final int listenerCount() {
        return listeners.elementCount();
    }

    //endregion

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
        if (isCancelRequested() && executor == null) {
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
        if (isCancelRequested() && executor == null) {
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
        if (isCancelRequested() && executor == null) {
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
        if (isCancelRequested() && executor == null) {
            Completion.fireNow(this, TYPE_NOTIFY, listener, ctx);
            return Registration.CLOSED;
        }
        Completion completion = getCompletion(executor, options, this, TYPE_NOTIFY, listener, ctx);
        return pushCompletion(completion);
    }

    // endregion

    // endregion

    // region core

    private static final int SYNC = 0;
    private static final int ASYNC = 1;
    private static final int NESTED = -1;

    private Registration pushCompletion(Completion newHead) {
        ICancelToken cancelToken = ExecutorUtils.getCancelToken(newHead.ctx, newHead.options);
        if (cancelToken.isCancelRequested()) {
            return Registration.CLOSED;
        }
        if (isCancelRequested()) {
            newHead.tryFire(SYNC);
            return Registration.CLOSED;
        }
        Registration registration = new Registration(newHead, newHead.rid);
        listeners.add(newHead);

        if (cancelToken.canBeCancelled() &&
                TaskOptions.isEnabled(newHead.options, TaskOptions.STAGE_LISTEN_CANCEL_TOKEN)) {
            cancelToken.thenRun(INVOKER, registration, TaskOptions.STAGE_UNCANCELLABLE_CTX);
        }
        return registration;
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
    /** 已加入异步队列 -- 不能被立即销毁；必须由TryFire销毁 */
    private static final int MASK_ASYNC_FIRING = 0x10;

    /** 行为树通常只在业务线程调用，避免多线程开销 */
    private static final ThreadLocal<ArrayDeque<Completion>> POOL = ThreadLocal.withInitial(ArrayDeque::new);

    /** 申请一个{@link Completion}对象 */
    private static Completion getCompletion(Executor executor, int options, CancelToken source,
                                            int type, Object action, Object ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        Completion completion = POOL.get().pollFirst();
        if (completion == null) {
            completion = new Completion();
        }
        completion.rid++; // 从池中取出时也加1
        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    private static void releaseCompletion(Completion completion) {
        completion.reset();
        POOL.get().addFirst(completion);
    }

    /** 用于关闭监听器 */
    private static final Consumer<Object> INVOKER = ctx -> {
        Registration registration = (Registration) ctx;
        registration.close();
    };

    private static class Completion implements ITask, ICancelTokenListener, IPooledCloseable {

        /** 重入id -- 只增不减 */
        int rid;

        CancelToken source;
        Executor executor;
        int options; // 包含任务类型信息
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

        @Override
        public final void onCancelRequested(ICancelToken cancelToken, Object ctx) {
            tryFire(SYNC);
        }

        public void tryFire(int mode) {
            // 如果走到这里，当前Completion一定未被回收，但action可能已被清理，即已收到取消信号
            CancelToken source;
            Executor executor;
            int options;
            Object action;
            Object ctx;
            // 代码可参考并发库中的实现
            boolean fire;
            {
                options = this.options;
                action = this.action;
                ctx = this.ctx;
                // 如果已收到取消信号，则直接回收
                if (action == null || ExecutorUtils.isCancelRequested(ctx, options)) {
                    releaseCompletion(this);
                    return;
                }
                source = this.source;
                executor = this.executor;

                // 如果是同步模式，需要claim=
                if (mode <= 0 && !ExecutorUtils.isInlinable(executor, options)) {
                    this.options |= MASK_ASYNC_FIRING;
                    this.executor = null;
                    fire = false;
                } else {
                    // 数据已拷贝到临时变量
                    releaseCompletion(this);
                    fire = true;
                }
            }
            if (fire) {
                fireNow(source, options, action, ctx);
                return;
            }
            try {
                executor.execute(this);
            } catch (Exception ex) {
                Task.logger.info("claim caught exception", ex);
            }
        }

        @SuppressWarnings("unchecked")
        private static void fireNow(CancelToken source,
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
                Task.logger.info("Action caught an exception", ex);
            }
        }

        @Override
        public boolean isClosed(long reentryId) {
            return reentryId != rid || this.action == null;
        }

        @Override
        public final void close(long reentryId) {
            // 只有rid匹配时，才能保证数据有效性 -- action为null表示已执行回调
            if (rid != reentryId || this.action == null) {
                return;
            }
            this.action = null;
            this.ctx = null;
            // 如果当前未进入异步执行，尝试立即回收 -- 可能正处于TryFire等待锁的状态，因此删除可能会失败
            if ((options & MASK_ASYNC_FIRING) == 0 && source.remListener(this)) {
                releaseCompletion(this);
            }
        }
    }
    // endregion
}