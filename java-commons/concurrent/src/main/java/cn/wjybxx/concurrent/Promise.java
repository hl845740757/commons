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

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.function.TriConsumer;
import cn.wjybxx.base.function.TriFunction;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.GuardedBy;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 声明{@link IFuture}接口加快instanceof测试
 *
 * @author wjybxx
 * date - 2024/1/10
 */
@ThreadSafe
public class Promise<T> implements IPromise<T>, IFuture<T> {

    /** 1毫秒多少纳秒 */
    private static final long NANO_PER_MILLISECOND = 1000_000L;
    /** 表示任务开始运行 */
    private static final Object COMPUTING = new Object();
    /** 如果一个任务成功时没有结果（或结果为null），使用该对象代替。 */
    private static final Object NIL = new Object();

    /**
     * Future关联的任务的计算结果，它同时也存储者{@code Future}的状态信息。
     * <ul>
     * <li>{@code null}表示初始状态</li>
     * <li>{@link #COMPUTING}表示任务正在进行，取消不能被立即响应；只能由任务自身响应取消</li>
     * <li>{@link #NIL}表示终止状态，表示正常完成，但是计算结果为null</li>
     * <li>{@link AltResult}表示终止状态，表示计算中出现异常，{@link AltResult#cause}为计算失败的原因。</li>
     * <li>其它任何非null值，表示正常完成，且计算结果非null。</li>
     * </ul>
     */
    @SuppressWarnings("unused")
    private volatile Object result;
    /**
     * 当前对象上的所有监听器，使用栈方式存储
     * 如果{@code stack}为{@link #TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
     */
    @SuppressWarnings("unused")
    private volatile Completion stack;

    /** 任务绑定的executor */
    private final Executor _executor;
    /** 任务绑定的上下文 */
    private final IContext _ctx;

    public Promise() {
        this._executor = null;
        this._ctx = IContext.NONE;
    }

    public Promise(Executor executor) {
        this._executor = executor;
        this._ctx = IContext.NONE;
    }

    public Promise(Executor executor, IContext ctx) {
        this._executor = executor;
        this._ctx = ctx == null ? IContext.NONE : ctx;
    }

    // region factory

    private Promise(Executor executor, IContext ctx, Object result) {
        this._executor = executor;
        this._ctx = ctx == null ? IContext.NONE : ctx;
        VH_RESULT.setRelease(this, result);
    }

    public static <V> Promise<V> completedPromise(V result) {
        return new Promise<>(null, null, result == null ? NIL : result);
    }

    public static <V> Promise<V> completedPromise(V result, Executor executor) {
        return new Promise<>(executor, IContext.NONE, encodeValue(result));
    }

    public static <V> Promise<V> completedPromise(V result, Executor executor, IContext ctx) {
        return new Promise<>(executor, ctx, encodeValue(result));
    }

    public static <V> Promise<V> failedPromise(Throwable cause) {
        Objects.requireNonNull(cause);
        return new Promise<>(null, null, new AltResult(cause));
    }

    public static <V> Promise<V> failedPromise(Throwable cause, Executor executor) {
        Objects.requireNonNull(cause);
        return new Promise<>(executor, IContext.NONE, new AltResult(cause));
    }

    public static <V> Promise<V> failedPromise(Throwable cause, Executor executor, IContext ctx) {
        Objects.requireNonNull(cause);
        return new Promise<>(executor, ctx, new AltResult(cause));
    }
    // endregion

    // region internal

    /** 异常结果包装对象，只有该类型表示失败 */
    private static class AltResult {

        final Throwable cause;

        AltResult(Throwable cause) {
            this.cause = cause;
        }
    }

    private static <T> Object encodeValue(T value) {
        return (value == null) ? NIL : value;
    }

    @SuppressWarnings("unchecked")
    private T decodeValue(Object result) {
        return result == NIL ? null : (T) result;
    }

    private static AltResult encodeThrowable(Throwable x) {
        return new AltResult((x instanceof CompletionException) ? x :
                new CompletionException(x));
    }

    /**
     * 非取消完成可以由初始状态或不可取消状态进入完成状态
     * CAS{@code null}或者{@link #COMPUTING} 到指定结果值
     */
    private boolean internalComplete(Object result) {
        // 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势 -- 这里先不优化
        Object preResult = VH_RESULT.compareAndExchange(this, null, result);
        if (preResult == null) {
            return true;
        }
        if (preResult == COMPUTING) {
            return VH_RESULT.compareAndSet(this, COMPUTING, result);
        }
        return false;
    }

    // endregion

    // region ctx

    @Nonnull
    @Override
    public final IContext ctx() {
        return _ctx;
    }

    /** 允许重写 */
    @Nullable
    @Override
    public Executor executor() {
        return _executor;
    }

    @Override
    public IFuture<T> asReadonly() {
        return new ReadOnlyFuture<>(this);
    }

    @Nonnull
    @Override
    public IFuture<T> toFuture() {
        return this;
    }

    // endregion

    // region 状态查询

    private static boolean isDone0(Object result) {
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
        if (r == null || r == COMPUTING) {
            return State.RUNNING;
        }
        if (r == NIL) {
            return State.SUCCESS;
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
    public final TaskStatus status() {
        return futureState(result);
    }

    private static TaskStatus futureState(Object r) {
        if (r == null) {
            return TaskStatus.PENDING;
        }
        if (r == COMPUTING) {
            return TaskStatus.COMPUTING;
        }
        if (r == NIL) {
            return TaskStatus.SUCCESS;
        }
        if (r instanceof AltResult altResult) {
            if (altResult.cause instanceof CancellationException) {
                return TaskStatus.CANCELLED;
            } else {
                return TaskStatus.FAILED;
            }
        }
        return TaskStatus.SUCCESS;
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
    public final boolean trySetComputing() {
        Object preResult = VH_RESULT.compareAndExchange(this, null, COMPUTING);
        return preResult == null;
    }

    @Override
    public final TaskStatus trySetComputing2() {
        Object preResult = VH_RESULT.compareAndExchange(this, null, COMPUTING);
        return futureState(preResult);
    }

    @Override
    public final void setComputing() {
        if (!trySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    @Override
    public final boolean trySetResult(T result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public final void setResult(T result) {
        if (!trySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    @Override
    public final boolean trySetException(@Nonnull Throwable cause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            FutureLogger.logCause(cause); // 尝试记录日志
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public final void setException(@Nonnull Throwable cause) {
        if (!trySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    @Override
    public boolean trySetCancelled(int code) {
        Throwable cause = code == 1
                ? StacklessCancellationException.INSTANCE
                : new StacklessCancellationException(code);
        if (internalComplete(new AltResult(cause))) {
            postComplete(this); // 不记录日志
            return true;
        }
        return false;
    }

    @Override
    public void setCancelled(int code) {
        if (!trySetCancelled(code)) {
            throw new IllegalStateException("Already complete");
        }
    }

    @Override
    public final boolean trySetCancelled() {
        return trySetCancelled(1);
    }

    @Override
    public final void setCancelled() {
        if (!trySetCancelled(1)) {
            throw new IllegalStateException("Already complete");
        }
    }

    @Deprecated
    @Override
    public final boolean cancel(boolean mayInterruptIfRunning) {
        // 由于要创建异常，先测试一下result
        Object r = result;
        if (isDone0(r)) {
            return isCancelled0(r);
        }
        if (trySetException(new CancellationException())) {
            return true;
        }
        // 可能被其它线程取消
        return isCancelled();
    }

    @Override
    public final boolean tryTransferFrom(IFuture<? extends T> input) {
        Objects.requireNonNull(input);
        if (isDone()) {
            return false;
        }
        if (tryTransferTo(input, this)) {
            postComplete(this);
            return true;
        }
        return false;
    }
    // endregion

    // region 非阻塞结果查询

    @Override
    public final T getNow() {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        return null;
    }

    @Override
    public final T getNow(T valueIfAbsent) {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        return valueIfAbsent;
    }

    @Override
    public final T resultNow() {
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
        @SuppressWarnings("unchecked") T value = (T) r;
        return value;
    }

    @Override
    public final Throwable exceptionNow() {
        return exceptionNow(true); // true以兼容jdk
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
        // 考虑executor可能被重写的情况
        if (executor() instanceof SingleThreadExecutor eventLoop && eventLoop.inEventLoop()) {
            throw new BlockingOperationException();
        }
    }

    @Override
    public final T get() throws InterruptedException, ExecutionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportGet(r);
        }
        await();
        return reportGet(result);
    }

    @Override
    public final T get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
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
    public final T join() throws CompletionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        awaitUninterruptibly();
        return reportJoin(result);
    }

    /** @return null表示future已完成 */
    @Nullable
    private Awaiter tryPushAwaiter() {
        Completion head = stack;
        if (head instanceof Awaiter awaiter) {
            return awaiter; // 阻塞操作不多，而且通常集中在调用链的首尾
        }
        Awaiter awaiter = new Awaiter(this);
        return pushCompletion(awaiter) ? awaiter : null;
    }

    @Override
    public final Promise<T> await() throws InterruptedException {
        if (isDone()) {
            return this;
        }
        checkDeadlock(); // 在执行阻塞操作前检测死锁 -- 这是Future的重要功能之一
        Awaiter awaiter = tryPushAwaiter();
        if (awaiter != null) {
            awaiter.await();
        }
        return this;
    }

    @Override
    public final Promise<T> awaitUninterruptibly() {
        if (isDone()) {
            return this;
        }
        checkDeadlock();
        Awaiter awaiter = tryPushAwaiter();
        if (awaiter != null) {
            awaiter.awaitUninterruptedly();
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
        checkDeadlock();
        Awaiter awaiter = tryPushAwaiter();
        if (awaiter != null) {
            return awaiter.await(timeout, unit);
        }
        return true;
    }

    @Override
    public final boolean awaitUninterruptibly(long timeout, @Nonnull TimeUnit unit) {
        if (timeout <= 0) {
            return isDone();
        }
        if (isDone()) {
            return true;
        }
        checkDeadlock();
        Awaiter awaiter = tryPushAwaiter();
        if (awaiter != null) {
            return awaiter.awaitUninterruptedly(timeout, unit);
        }
        return true;
    }

    // endregion

    // region 链式调用
    // 暂不做已完成情况下的优化--降低代码复杂度；另外向已完成的Future添加监听器的情况不常见(至少比例是低的)

    protected <U> Promise<U> newIncompletePromise(IContext ctx, Executor exe) {
        return new Promise<>(exe, ctx);
    }

    // region compose-apply

    @Override
    public <U> Promise<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return uniComposeApply(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn) {
        return uniComposeApply(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeApply(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeApply(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniComposeApply(Executor executor,
                                           BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn,
                                           @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniComposeApply<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region compose-call

    @Override
    public <U> Promise<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn,
                                      @Nullable IContext ctx, int options) {
        return uniComposeCall(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        return uniComposeCall(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeCall(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn,
                                           @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeCall(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniComposeCall(Executor executor,
                                          Function<? super IContext, ? extends ICompletionStage<U>> fn,
                                          @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniComposeCall<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region compose-catching

    @Override
    public <X extends Throwable> Promise<T> composeCatching(Class<X> exceptionType,
                                                            BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback,
                                                            @Nullable IContext ctx, int options) {
        return uniComposeCatching(null, exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> Promise<T> composeCatching(Class<X> exceptionType,
                                                            BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback) {
        return uniComposeCatching(null, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> Promise<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                                                 BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeCatching(executor, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> Promise<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                                                 BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback,
                                                                 @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeCatching(executor, exceptionType, fallback, ctx, options);
    }

    private <X extends Throwable> Promise<T> uniComposeCatching(Executor executor, Class<X> exceptionType,
                                                                BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback,
                                                                @Nullable IContext ctx, int options) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);

        if (ctx == null) ctx = this._ctx;
        Promise<T> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniComposeCathing<>(executor, options, this, promise, exceptionType, fallback));
        return promise;
    }

    // endregion

    // region compose-handle

    @Override
    public <U> Promise<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                        @Nullable IContext ctx, int options) {
        return uniComposeHandle(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        return uniComposeHandle(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeHandle(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                             @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniComposeHandle(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniComposeHandle(Executor executor,
                                            TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                            @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniComposeHandle<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region uni-apply

    @Override
    public <U> Promise<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniApply(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn) {
        return uniApply(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends U> fn) {
        Objects.requireNonNull(executor, "executor");
        return uniApply(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniApply(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniApply(Executor executor, BiFunction<? super IContext, ? super T, ? extends U> fn,
                                    @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniApply<>(executor, options, this, promise, fn));
        return promise;
    }
    // endregion

    // region uni-accept

    @Override
    public Promise<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        return uniAccept(null, action, ctx, options);
    }

    @Override
    public Promise<Void> thenAccept(BiConsumer<? super IContext, ? super T> action) {
        return uniAccept(null, action, null, 0);
    }

    @Override
    public Promise<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super T> action) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept(executor, action, null, 0);
    }

    @Override
    public Promise<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniAccept(executor, action, ctx, options);
    }

    private Promise<Void> uniAccept(Executor executor, BiConsumer<? super IContext, ? super T> action,
                                    @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        Promise<Void> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniAccept<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion


    // region uni-call

    @Override
    public <U> Promise<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniCall(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> thenCall(Function<? super IContext, ? extends U> fn) {
        return uniCall(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn) {
        Objects.requireNonNull(executor, "executor");
        return uniCall(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniCall(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniCall(Executor executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniCall<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region uni-run

    @Override
    public Promise<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return uniRun(null, action, ctx, options);
    }

    @Override
    public Promise<Void> thenRun(Consumer<? super IContext> action) {
        return uniRun(null, action, null, 0);
    }

    @Override
    public Promise<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action) {
        Objects.requireNonNull(executor, "executor");
        return uniRun(executor, action, null, 0);
    }

    @Override
    public Promise<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniRun(executor, action, ctx, options);
    }

    private Promise<Void> uniRun(Executor executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        Promise<Void> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniRun<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion

    // region uni-catching

    @Override
    public <X extends Throwable> Promise<T> catching(Class<X> exceptionType,
                                                     BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                     @Nullable IContext ctx, int options) {
        return uniCatching(null, exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> Promise<T> catching(Class<X> exceptionType,
                                                     BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        return uniCatching(null, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> Promise<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                                          BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        Objects.requireNonNull(executor, "executor");
        return uniCatching(executor, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> Promise<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                                          BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                          @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniCatching(executor, exceptionType, fallback, ctx, options);
    }

    private <X extends Throwable> Promise<T> uniCatching(Executor executor, Class<X> exceptionType,
                                                         BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                         @Nullable IContext ctx, int options) {
        Objects.requireNonNull(exceptionType, "exceptionType");
        Objects.requireNonNull(fallback, "fallback");

        if (ctx == null) ctx = this._ctx;
        Promise<T> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniCathing<>(executor, options, this, promise, exceptionType, fallback));
        return promise;
    }
    // endregion

    // region uni-handle

    @Override
    public <U> Promise<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                 @Nullable IContext ctx, int options) {
        return uniHandle(null, fn, ctx, options);
    }

    @Override
    public <U> Promise<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return uniHandle(null, fn, null, 0);
    }

    @Override
    public <U> Promise<U> handleAsync(Executor executor, TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return uniHandle(executor, fn, null, 0);
    }

    @Override
    public <U> Promise<U> handleAsync(Executor executor, TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                      @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniHandle(executor, fn, ctx, options);
    }

    private <U> Promise<U> uniHandle(Executor executor,
                                     TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                     @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        Promise<U> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniHandle<>(executor, options, this, promise, fn));
        return promise;
    }
    // endregion

    // region uni-when

    @Override
    public Promise<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return uniWhenComplete(null, action, ctx, options);
    }

    @Override
    public Promise<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return uniWhenComplete(null, action, null, 0);
    }

    @Override
    public Promise<T> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return uniWhenComplete(executor, action, null, 0);
    }

    @Override
    public Promise<T> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(executor, "executor");
        return uniWhenComplete(executor, action, ctx, options);
    }

    private Promise<T> uniWhenComplete(Executor executor,
                                       TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                       @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        Promise<T> promise = newIncompletePromise(ctx, executor == null ? this.executor() : executor);
        pushCompletion(new UniWhenComplete<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion

    // region onComplete

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context, int options) {
        uniOnCompletedFuture1(null, action, context, options);
    }

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context) {
        uniOnCompletedFuture1(null, action, context, 0);
    }

    @Override
    public void onCompletedAsync(Executor executor, BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context) {
        Objects.requireNonNull(executor, "executor");
        uniOnCompletedFuture1(executor, action, context, 0);
    }


    @Override
    public void onCompletedAsync(Executor executor, BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context, int options) {
        Objects.requireNonNull(executor, "executor");
        uniOnCompletedFuture1(executor, action, context, options);
    }

    private void uniOnCompletedFuture1(Executor executor, BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context, int options) {
        Objects.requireNonNull(action, "action");
        Objects.requireNonNull(context, "context");
        if (action instanceof Completion completion) { // 主要是Relay
            pushCompletion(completion);
            return;
        }
        if (this.isDone() && executor == null) { // listener避免不必要的插入
            UniOnCompleteFuture1.fireNow(context, this, action, null);
        } else {
            pushCompletion(new UniOnCompleteFuture1<>(executor, options, this, context, action));
        }
    }

    @Override
    public void onCompleted(Consumer<? super IFuture<T>> action, int options) {
        uniOnCompletedFuture2(null, action, options);
    }

    @Override
    public void onCompleted(Consumer<? super IFuture<T>> action) {
        uniOnCompletedFuture2(null, action, 0);
    }

    @Override
    public void onCompletedAsync(Executor executor, Consumer<? super IFuture<T>> action) {
        Objects.requireNonNull(executor);
        uniOnCompletedFuture2(executor, action, 0);
    }

    @Override
    public void onCompletedAsync(Executor executor, Consumer<? super IFuture<T>> action, int options) {
        Objects.requireNonNull(executor);
        uniOnCompletedFuture2(executor, action, options);
    }

    private void uniOnCompletedFuture2(Executor executor, Consumer<? super IFuture<T>> action, int options) {
        Objects.requireNonNull(action, "action");
        if (action instanceof Completion completion) { // 主要是Relay
            pushCompletion(completion);
            return;
        }
        if (this.isDone() && executor == null) { // listener避免不必要的插入
            UniOnCompleteFuture2.fireNow(this, action, null);
        } else {
            pushCompletion(new UniOnCompleteFuture2<>(executor, options, this, action));
        }
    }

    // endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Modes for Completion.tryFire. Signedness matters.
    /**
     * 同步调用模式，表示压栈过程中发现{@code Future}已进入完成状态，从而调用{@link Completion#tryFire(int)}。
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

    /** 用于表示任务已申领权限 */
    private static final Executor CLAIMED = Runnable::run;

    /** @return 是否压栈成功 */
    private boolean pushCompletion(Completion newHead) {
        if (isDone()) {
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

    /**
     * 推送future的完成事件。
     * - 声明为静态会更清晰易懂
     */
    private static void postComplete(Promise<?> future) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = clearListeners(future, next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                curr.next = null; // help gc

                future = curr.tryFire(NESTED);
                if (future != null) {
                    continue outer;
                }
            }
            break;
        }
    }

    /**
     * 清空当前{@code Future}上的监听器，并将当前{@code Future}上的监听器逆序方式插入到{@code onto}前面。
     * <p>
     * Q: 这步操作是要干什么？<br>
     * A: Future的监听器构成了一棵树，在不进行优化的情况下，遍历监听器是一个【前序遍历】过程，这会产生很深的方法栈，从而影响性能。
     * 该操作将子节点的监听器提升为当前节点的兄弟节点(插在前方)，从而将树形遍历优化为【线性遍历】，从而降低了栈深度，提高了性能。
     * <p>
     * ps:参考自Guava中的Future实现 -- JDK的实现太复杂，看不懂...
     */
    private static <T> Completion clearListeners(Promise<T> promise, Completion onto) {
        // 我们需要进行三件事
        // 1. 原子方式将当前Listeners赋值为TOMBSTONE，因为pushCompletion添加的监听器的可见性是由CAS提供的。
        // 2. 将当前栈内元素逆序，因为即使在接口层进行了说明（不提供监听器执行时序保证），但仍然有人依赖于监听器的执行时序(期望先添加的先执行)
        // 3. 将逆序后的元素插入到'onto'前面，即插入到原本要被通知的下一个监听器的前面
        Completion head;
        do {
            head = promise.stack;
            if (head == TOMBSTONE) {
                return onto;
            }
        } while (!VH_STACK.compareAndSet(promise, head, TOMBSTONE));

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            if (tmpHead instanceof Awaiter awaiter) {
                awaiter.releaseWaiters(); // 唤醒等待线程
                continue;
            }

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static final VarHandle VH_RESULT;
    private static final VarHandle VH_STACK;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_RESULT = l.findVarHandle(Promise.class, "result", Object.class);
            VH_STACK = l.findVarHandle(Promise.class, "stack", Completion.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    // 开放给Completion的方法

    private boolean completeNull() {
        return internalComplete(NIL);
    }

    private boolean completeValue(T value) {
        return internalComplete(encodeValue(value));
    }

    private boolean completeCancelled() {
        return internalComplete(new AltResult(StacklessCancellationException.INSTANCE));
    }

    /**
     * 如果一个{@link Completion}在计算中出现异常，则使用该方法使目标进入完成状态。
     * (出现新的异常)
     */
    private boolean completeThrowable(@Nonnull Throwable x) {
        FutureLogger.logCause(x);
        return internalComplete(encodeThrowable(x));
    }

    /**
     * 使用依赖项的结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     */
    private boolean completeRelay(Object r) {
        return internalComplete(r);
    }

    /**
     * 使用依赖项的异常结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     * 在已知依赖项异常完成的时候可以调用该方法，减少开销。
     * 这里实现和{@link CompletableFuture}不同，这里保留原始结果，不强制将异常转换为{@link CompletionException}。
     * 这样有助与用户捕获正确的异常类型，而不是一个奇怪的CompletionException
     */
    private boolean completeRelayThrowable(AltResult r) {
        return internalComplete(r);
    }

    /**
     * 实现{@link Runnable}接口是因为可能需要在另一个线程执行。
     */
    private static abstract class Completion implements Runnable {

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
         *
         * @implNote tryFire不可以抛出异常，否则会导致其它监听器也丢失信号
         */
        abstract Promise<?> tryFire(int mode);

    }

    /** 表示stack已被清理 */
    private static final Completion TOMBSTONE = new Completion() {
        @Override
        Promise<Object> tryFire(int mode) {
            return null;
        }
    };

    /** 用于实现阻塞等待future完成 */
    private static class Awaiter extends Completion {

        /** 所有线程都在future上等待，可一次唤醒 */
        final IFuture<?> future;
        /** 用于避免不必要的notifyAll */
        @GuardedBy("future")
        private int waiterCount;

        public Awaiter(IFuture<?> future) {
            this.future = future;
        }

        @Override
        Promise<?> tryFire(int mode) {
            releaseWaiters();
            return null;
        }

        void releaseWaiters() {
            synchronized (future) {
                if (waiterCount > 0) {
                    future.notifyAll();
                }
            }
        }

        private void incWaiter() {
            waiterCount++;
        }

        private void decWaiter() {
            waiterCount--;
        }

        void await() throws InterruptedException {
            ThreadUtils.checkInterrupted();
            synchronized (future) {
                incWaiter();
                try {
                    while (!future.isDone()) {
                        future.wait();
                    }
                } finally {
                    decWaiter();
                }
            }
        }

        void awaitUninterruptedly() {
            boolean interrupted = Thread.interrupted();
            synchronized (future) {
                incWaiter();
                try {
                    while (!future.isDone()) {
                        try {
                            future.wait();
                        } catch (InterruptedException ignore) {
                            interrupted = true;
                        }
                    }
                } finally {
                    decWaiter();
                    if (interrupted) {
                        ThreadUtils.recoveryInterrupted();
                    }
                }
            }
        }

        boolean await(long timeout, TimeUnit timeUnit) throws InterruptedException {
            // 在执行一个耗时操作之前检查中断是有必要的
            ThreadUtils.checkInterrupted();
            final long deadline = System.nanoTime() + timeUnit.toNanos(timeout);
            synchronized (future) {
                incWaiter();
                try {
                    while (!future.isDone()) {
                        // 获取锁需要时间，因此应该在获取锁之后计算剩余时间
                        final long remainNano = deadline - System.nanoTime();
                        if (remainNano <= 0) {
                            return false;
                        }
                        future.wait(remainNano / NANO_PER_MILLISECOND, (int) (remainNano % NANO_PER_MILLISECOND));
                    }
                } finally {
                    decWaiter();
                }
            }
            return true;
        }

        boolean awaitUninterruptedly(long timeout, TimeUnit timeUnit) {
            boolean interrupted = Thread.interrupted();
            final long deadline = System.nanoTime() + timeUnit.toNanos(timeout);
            synchronized (future) {
                incWaiter();
                try {
                    while (!future.isDone()) {
                        long remainNano = deadline - System.nanoTime();
                        if (remainNano <= 0) {
                            return false;
                        }
                        try {
                            future.wait(remainNano / NANO_PER_MILLISECOND, (int) (remainNano % NANO_PER_MILLISECOND));
                        } catch (InterruptedException e) {
                            interrupted = true;
                        }
                    }
                } finally {
                    decWaiter();
                    if (interrupted) {
                        ThreadUtils.recoveryInterrupted();
                    }
                }
                return true;
            }
        }

    }

    /**
     * {@link UniCompletion}接收一个输入的计算
     *
     * @param <V> 输入值类型
     * @param <U> 输入值类型
     */
    private static abstract class UniCompletion<V, U> extends Completion {

        Executor executor;
        int options;
        Promise<V> input;
        Promise<U> output;

        public UniCompletion(Executor executor, int options, Promise<V> input, Promise<U> output) {
            this.executor = executor;
            this.input = input;
            this.output = output;
            this.options = options;
        }

        /**
         * 当{@link Completion}满足触发条件时，如果是{@link #SYNC}和{@link #NESTED}模式，则调用该方法抢占执行权限。
         * 如果{@link Completion}有多个触发条件，则可能并发调用{@link #tryFire(int)}，而只有一个线程应该执行特定逻辑。
         * {@link #ASYNC}模式表示已抢得执行权限，但是不能在当前线程执行。
         * <p>
         * 注意：
         * 1. 这里和{@link CompletableFuture}不同，在我们的实现中{@link Completion#tryFire(int)}不会被并发调用，而JDK任务会进入ForkJoin池。
         * 2. 虽然我们的任务不会被并发调用，但异步任务的tryFire可能被调用两次，因此还是需要一个标记。
         * 3. 需要在try-catch块调用，因为可能提交任务失败，或者任务被取消。
         *
         * @return 如果成功抢占权限且可以立即执行则返回true，否则返回false
         * @throws CancellationException      如果任务被取消
         * @throws RejectedExecutionException 如果Executor已关闭
         */
        final boolean claim() {
            final Executor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            if (!output.trySetComputing()) { // 被用户取消
                throw StacklessCancellationException.INSTANCE;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return submit(this, e, options);
            }
            return true;
        }

    }

    // region compose-x

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

    private static <U> Promise<U> postFire(Promise<U> output, int mode, boolean setCompleted) {
        if (!setCompleted) { // 未竞争成功
            return null;
        }
        if (mode < 0) { // 嵌套模式
            return output;
        }
        postComplete(output);
        return null;
    }

    private static <U> boolean tryTransferTo(final IFuture<? extends U> input, final Promise<U> output) {
        if (input instanceof Promise<? extends U> promise) {
            Object r = promise.result;
            if (isDone0(r)) {
                return output.completeRelay(r);
            }
            return false;
        }
        // 有可能是Readonly或其它实现
        TaskStatus state = input.status();
        switch (state) {
            case PENDING, COMPUTING -> {
                return false;
            }
            case SUCCESS -> {
                return output.completeValue(input.getNow());
            }
            case FAILED, CANCELLED -> {
                Throwable ex = input.exceptionNow(false);
                return output.completeRelayThrowable(new AltResult(ex));
            }
            default -> throw new AssertionError();
        }
    }

    private static class UniRelay<V> extends Completion implements Consumer<IFuture<? extends V>> {

        IFuture<? extends V> input;
        Promise<V> output;

        public UniRelay(IFuture<? extends V> input, Promise<V> output) {
            this.input = input;
            this.output = output;
        }

        @Override
        public void accept(IFuture<? extends V> iFuture) {
            tryFire(SYNC);
        }

        @Override
        Promise<?> tryFire(int mode) {
            final IFuture<? extends V> input = this.input;
            final Promise<V> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                setCompleted = tryTransferTo(input, output);
            }
            // help gc
            this.input = null;
            this.output = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniComposeApply<V, U> extends UniCompletion<V, U> {

        BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn;

        public UniComposeApply(Executor executor, int options, Promise<V> input, Promise<U> output,
                               BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<U> relay = fn.apply(output._ctx, input.decodeValue(r)).toFuture();
                    setCompleted = tryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        relay.onCompleted(new UniRelay<>(relay, output), 0);
                    }
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniComposeCall<V, U> extends UniCompletion<V, U> {

        Function<? super IContext, ? extends ICompletionStage<U>> fn;

        public UniComposeCall(Executor executor, int options, Promise<V> input, Promise<U> output,
                              Function<? super IContext, ? extends ICompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<U> relay = fn.apply(output._ctx).toFuture();
                    setCompleted = tryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        relay.onCompleted(new UniRelay<>(relay, output), 0);
                    }
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniComposeCathing<X extends Throwable, V> extends UniCompletion<V, V> {

        Class<X> exceptionType;
        BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback;

        public UniComposeCathing(Executor executor, int options, Promise<V> input, Promise<V> output,
                                 Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback) {
            super(executor, options, input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<V> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (!(r instanceof AltResult altResult) || !exceptionType.isInstance(altResult.cause)) {
                    setCompleted = output.completeRelay(r);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<V> relay = fallback.apply(output._ctx, exceptionType.cast(altResult.cause)).toFuture();
                    setCompleted = tryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        relay.onCompleted(new UniRelay<>(relay, output), 0);
                    }
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.exceptionType = null;
            this.fallback = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniComposeHandle<V, U> extends UniCompletion<V, U> {

        TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn;

        public UniComposeHandle(Executor executor, int options, Promise<V> input, Promise<U> output,
                                TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    Object r = input.result;
                    IFuture<U> relay;
                    if (r instanceof AltResult altResult) {
                        relay = fn.apply(output._ctx, null, altResult.cause).toFuture();
                    } else {
                        relay = fn.apply(output._ctx, input.decodeValue(r), null).toFuture();
                    }
                    setCompleted = tryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        relay.onCompleted(new UniRelay<>(relay, output), 0);
                    }
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    // endregion

    // region uni-x

    private static class UniApply<V, U> extends UniCompletion<V, U> {

        BiFunction<? super IContext, ? super V, ? extends U> fn;

        public UniApply(Executor executor, int options, Promise<V> input, Promise<U> output,
                        BiFunction<? super IContext, ? super V, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.completeValue(fn.apply(output._ctx, input.decodeValue(r)));
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniAccept<V> extends UniCompletion<V, Void> {

        BiConsumer<? super IContext, ? super V> action;

        public UniAccept(Executor executor, int options, Promise<V> input, Promise<Void> output,
                         BiConsumer<? super IContext, ? super V> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<Void> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    action.accept(output._ctx, input.decodeValue(r));
                    setCompleted = output.completeNull();
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.action = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniCall<V, U> extends UniCompletion<V, U> {

        Function<? super IContext, ? extends U> fn;

        public UniCall(Executor executor, int options, Promise<V> input, Promise<U> output,
                       Function<? super IContext, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.completeValue(fn.apply(output._ctx));
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniRun<V> extends UniCompletion<V, Void> {

        Consumer<? super IContext> action;

        public UniRun(Executor executor, int options, Promise<V> input, Promise<Void> output,
                      Consumer<? super IContext> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<Void> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (r instanceof AltResult altResult) {
                    setCompleted = output.completeRelayThrowable(altResult);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    action.accept(output._ctx);
                    setCompleted = output.completeNull();
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.action = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniCathing<X extends Throwable, V> extends UniCompletion<V, V> {

        Class<X> exceptionType;
        BiFunction<? super IContext, ? super X, ? extends V> fallback;

        public UniCathing(Executor executor, int options, Promise<V> input, Promise<V> output,
                          Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
            super(executor, options, input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<V> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                Object r = input.result;
                if (!(r instanceof AltResult altResult) || !exceptionType.isInstance(altResult.cause)) {
                    setCompleted = output.completeRelay(r);
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    V fr = fallback.apply(output._ctx, exceptionType.cast(altResult.cause));
                    setCompleted = output.completeValue(fr);
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.exceptionType = null;
            this.fallback = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniHandle<V, U> extends UniCompletion<V, U> {

        TriFunction<? super IContext, ? super V, ? super Throwable, ? extends U> fn;

        public UniHandle(Executor executor, int options, Promise<V> input, Promise<U> output,
                         TriFunction<? super IContext, ? super V, ? super Throwable, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<U> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    Object r = input.result;
                    U relay;
                    if (r instanceof AltResult altResult) {
                        relay = fn.apply(output._ctx, null, altResult.cause);
                    } else {
                        relay = fn.apply(output._ctx, input.decodeValue(r), null);
                    }
                    setCompleted = output.completeValue(relay);
                } catch (Throwable e) {
                    setCompleted = output.completeThrowable(e);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.fn = null;
            return postFire(output, mode, setCompleted);
        }
    }

    private static class UniWhenComplete<V> extends UniCompletion<V, V> {

        TriConsumer<? super IContext, ? super V, ? super Throwable> action;

        public UniWhenComplete(Executor executor, int options, Promise<V> input, Promise<V> output,
                               TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            final Promise<V> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                // 用户取消或目标executor已关闭，可能导致与上游结果不同
                if (output.isDone()) {
                    setCompleted = false;
                    break tryComplete;
                }
                if (output._ctx.cancelToken().isCancelling()) {
                    setCompleted = output.completeCancelled();
                    break tryComplete;
                }
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                } catch (Throwable ex) {
                    setCompleted = output.trySetException(ex);
                    break tryComplete;
                }
                Object r = input.result;
                try {
                    if (r instanceof AltResult altResult) {
                        action.accept(output._ctx, null, altResult.cause);
                    } else {
                        action.accept(output._ctx, input.decodeValue(r), null);
                    }
                    setCompleted = output.completeRelay(r);
                } catch (Throwable e) {
                    FutureLogger.logCause(e, "UniWhenComplete caught an exception");
                    setCompleted = output.completeRelay(r);
                }
            }
            // help gc
            this.input = null;
            this.output = null;
            this.action = null;
            return postFire(output, mode, setCompleted);
        }
    }
    // endregion

    // region UniOnComplete

    /** 普通回调式计算的超类 */
    private static abstract class UniOnComplete<V> extends Completion {

        Executor executor;
        int options;
        Promise<V> input;

        public UniOnComplete(Executor executor, int options, Promise<V> input) {
            this.options = options;
            this.executor = executor;
            this.input = input;
        }

        final boolean claim() {
            Executor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return submit(this, e, options);
            }
            return true;
        }
    }

    private static class UniOnCompleteFuture1<V> extends UniOnComplete<V> {

        IContext ctx;
        BiConsumer<? super IContext, ? super IFuture<V>> action;

        public UniOnCompleteFuture1(Executor executor, int options, Promise<V> input, IContext ctx,
                                    BiConsumer<? super IContext, ? super IFuture<V>> action) {
            super(executor, options, input);
            this.ctx = ctx;
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            tryComplete:
            {
                if (ctx.cancelToken().isCancelling()) {
                    break tryComplete;
                }
                // 异步模式下已经claim
                if (!fireNow(ctx, input, action, mode > 0 ? null : this)) {
                    return null;
                }
            }
            // help gc
            this.ctx = null;
            this.executor = null;
            this.input = null;
            this.action = null;
            return null;
        }

        static <V> boolean fireNow(IContext ctx, Promise<V> input,
                                   BiConsumer<? super IContext, ? super IFuture<V>> action,
                                   UniOnCompleteFuture1<V> c) {
            try {
                if (c != null && !c.claim()) {
                    return false;
                }
                action.accept(ctx, input);
            } catch (Throwable e) {
                FutureLogger.logCause(e, "UniOnCompleteFuture1 caught an exception");
            }
            return true;
        }
    }

    private static class UniOnCompleteFuture2<V> extends UniOnComplete<V> {

        Consumer<? super IFuture<V>> action;

        public UniOnCompleteFuture2(Executor executor, int options, Promise<V> input,
                                    Consumer<? super IFuture<V>> action) {
            super(executor, options, input);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            final Promise<V> input = this.input;
            // 异步模式下已经claim
            if (!fireNow(input, action, mode > 0 ? null : this)) {
                return null;
            }
            // help gc
            this.executor = null;
            this.input = null;
            this.action = null;
            return null;
        }

        static <V> boolean fireNow(Promise<V> input,
                                   Consumer<? super IFuture<V>> action,
                                   UniOnCompleteFuture2<V> c) {
            try {
                if (c != null && !c.claim()) {
                    return false;
                }
                action.accept(input);
            } catch (Throwable e) {
                FutureLogger.logCause(e, "UniOnCompleteFuture2 caught an exception");
            }
            return true;
        }
    }
    // endregion

}
