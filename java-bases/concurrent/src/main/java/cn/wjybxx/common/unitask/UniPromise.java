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

import cn.wjybxx.base.func.TriConsumer;
import cn.wjybxx.base.func.TriFunction;
import cn.wjybxx.common.concurrent.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 单线程版本的{@link IPromise}
 * 1.省去了接口抽象，单线程版本直接使用该类即可 -- 省去抽象可减少开销。
 * 2.接口说明可参考{@link IPromise}
 *
 * <h3>单线程化做的变动</h3>
 * 1.去除{@link #result}等的volatile操作，变更为普通字段。
 * 2.去除了阻塞操作Awaiter的支持。
 * 3.Executor不可为null，且固定继承；
 * 4.通过int值表示是否是异步操作，以及是否已申领权限。
 *
 * @author wjybxx
 * date - 2024/1/10
 */
@NotThreadSafe
public class UniPromise<T> implements UniFuture<T> {

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
    private Object result;
    /**
     * 当前对象上的所有监听器，使用栈方式存储
     * 如果{@code stack}为{@link #TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
     */
    private Completion stack;

    /** 任务绑定的executor */
    private final Executor _executor;
    /** 任务绑定的上下文 */
    private final IContext _ctx;

    public UniPromise(Executor executor) {
        this._executor = Objects.requireNonNull(executor);
        this._ctx = IContext.NONE;
    }

    public UniPromise(Executor executor, IContext ctx) {
        this._executor = Objects.requireNonNull(executor);
        this._ctx = ctx == null ? IContext.NONE : ctx;
    }

    // region factory

    private UniPromise(Executor executor, IContext ctx, Object result) {
        this._executor = Objects.requireNonNull(executor);
        this._ctx = ctx == null ? IContext.NONE : ctx;
        this.result = result;
    }

    public static <V> UniPromise<V> succeededPromise(V result, IExecutor executor) {
        return new UniPromise<>(executor, IContext.NONE, encodeValue(result));
    }

    public static <V> UniPromise<V> succeededPromise(V result, IExecutor executor, IContext ctx) {
        return new UniPromise<>(executor, ctx, encodeValue(result));
    }

    public static <V> UniPromise<V> failedPromise(Throwable cause, IExecutor executor) {
        Objects.requireNonNull(cause);
        return new UniPromise<>(executor, IContext.NONE, new AltResult(cause));
    }

    public static <V> UniPromise<V> failedPromise(Throwable cause, IExecutor executor, IContext ctx) {
        Objects.requireNonNull(cause);
        return new UniPromise<>(executor, ctx, new AltResult(cause));
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
     * {@code null}或者{@link #COMPUTING} 到指定结果值
     */
    private boolean internalComplete(Object result) {
        Object preResult = this.result;
        if (preResult == null) {
            this.result = result;
            return true;
        }
        if (preResult == COMPUTING) {
            this.result = result;
            return true;
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

    @Nonnull
    @Override
    public Executor executor() {
        return _executor;
    }

    @Override
    public UniFuture<T> asReadonly() {
        return new ReadonlyUniFuture<>(this);
    }

    @Nonnull
    @Override
    public UniFuture<T> toFuture() {
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
    public final FutureState futureState() {
        return futureState(result);
    }

    private static FutureState futureState(Object r) {
        if (r == null) {
            return FutureState.PENDING;
        }
        if (r == COMPUTING) {
            return FutureState.COMPUTING;
        }
        if (r == NIL) {
            return FutureState.SUCCESS;
        }
        if (r instanceof AltResult altResult) {
            if (altResult.cause instanceof CancellationException) {
                return FutureState.CANCELLED;
            } else {
                return FutureState.FAILED;
            }
        }
        return FutureState.SUCCESS;
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

    public final boolean trySetComputing() {
        Object preResult = this.result;
        if (preResult == null) {
            result = COMPUTING;
            return true;
        }
        return false;
    }

    public final FutureState trySetComputing2() {
        Object preResult = this.result;
        if (preResult == null) {
            result = COMPUTING;
        }
        return futureState(preResult);
    }

    public final void setComputing() {
        if (!trySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    public final boolean trySetResult(T result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return true;
        }
        return false;
    }

    public final void setResult(T result) {
        if (!trySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public final boolean trySetException(@Nonnull Throwable cause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            FutureLogger.logCause(cause); // 尝试记录日志
            postComplete(this);
            return true;
        }
        return false;
    }

    public final void setException(@Nonnull Throwable cause) {
        if (!trySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public final boolean trySetCancelled() {
        Throwable cause = StacklessCancellationException.INSTANCE;
        if (internalComplete(new AltResult(cause))) {
            postComplete(this); // 不记录日志
            return true;
        }
        return false;
    }

    public final void setCancelled() {
        if (!trySetCancelled()) {
            throw new IllegalStateException("Already complete");
        }
    }

    public final boolean tryTransferFrom(UniFuture<? extends T> input) {
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

    @Override
    public final T get() throws ExecutionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportGet(r);
        }
        throw new BlockingOperationException("get");
    }

    @Override
    public final T join() throws CompletionException {
        final Object r = result;
        if (isDone0(r)) {
            return reportJoin(r);
        }
        throw new BlockingOperationException("join");
    }

    // endregion

    // region 链式调用
    // 暂不做已完成情况下的优化--降低代码复杂度；另外向已完成的Future添加监听器的情况不常见(至少比例是低的)

    protected <U> UniPromise<U> newIncompletePromise(IContext ctx, Executor exe) {
        return new UniPromise<>(exe, ctx);
    }

    // region compose-apply

    @Override
    public <U> UniCompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return uniComposeApply(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn) {
        return uniComposeApply(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn) {
        return uniComposeApply(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return uniComposeApply(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniComposeApply(int executor,
                                                      BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn,
                                                      @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniComposeApply<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region compose-call

    @Override
    public <U> UniCompletionStage<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn,
                                                 @Nullable IContext ctx, int options) {
        return uniComposeCall(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn) {
        return uniComposeCall(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn) {
        return uniComposeCall(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn,
                                                      @Nullable IContext ctx, int options) {
        return uniComposeCall(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniComposeCall(int executor,
                                                     Function<? super IContext, ? extends UniCompletionStage<U>> fn,
                                                     @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniComposeCall<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region compose-catching

    @Override
    public <X extends Throwable> UniCompletionStage<T> composeCatching(Class<X> exceptionType,
                                                                       BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback,
                                                                       @Nullable IContext ctx, int options) {
        return uniComposeCatching(0, exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> composeCatching(Class<X> exceptionType,
                                                                       BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback) {
        return uniComposeCatching(0, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> composeCatchingAsync(Class<X> exceptionType,
                                                                            BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback) {
        return uniComposeCatching(EXE_ASYNC, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> composeCatchingAsync(Class<X> exceptionType,
                                                                            BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback,
                                                                            @Nullable IContext ctx, int options) {
        return uniComposeCatching(EXE_ASYNC, exceptionType, fallback, ctx, options);
    }

    private <X extends Throwable> UniCompletionStage<T> uniComposeCatching(int executor, Class<X> exceptionType,
                                                                           BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback,
                                                                           @Nullable IContext ctx, int options) {
        Objects.requireNonNull(exceptionType);
        Objects.requireNonNull(fallback);

        if (ctx == null) ctx = this._ctx;
        UniPromise<T> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniComposeCathing<>(executor, options, this, promise, exceptionType, fallback));
        return promise;
    }

    // endregion

    // region compose-handle

    @Override
    public <U> UniCompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn,
                                                   @Nullable IContext ctx, int options) {
        return uniComposeHandle(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn) {
        return uniComposeHandle(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn) {
        return uniComposeHandle(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn,
                                                        @Nullable IContext ctx, int options) {
        return uniComposeHandle(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniComposeHandle(int executor,
                                                       TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn,
                                                       @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniComposeHandle<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region uni-apply

    @Override
    public <U> UniCompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniApply(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn) {
        return uniApply(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn) {
        return uniApply(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniApply(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniApply(int executor, BiFunction<? super IContext, ? super T, ? extends U> fn,
                                               @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniApply<>(executor, options, this, promise, fn));
        return promise;
    }
    // endregion

    // region uni-accept

    @Override
    public UniCompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        return uniAccept(0, action, ctx, options);
    }

    @Override
    public UniCompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action) {
        return uniAccept(0, action, null, 0);
    }

    @Override
    public UniCompletionStage<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action) {
        return uniAccept(EXE_ASYNC, action, null, 0);
    }

    @Override
    public UniCompletionStage<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        return uniAccept(EXE_ASYNC, action, ctx, options);
    }

    private UniCompletionStage<Void> uniAccept(int executor, BiConsumer<? super IContext, ? super T> action,
                                               @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        UniPromise<Void> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniAccept<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion


    // region uni-call

    @Override
    public <U> UniCompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniCall(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn) {
        return uniCall(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenCallAsync(Function<? super IContext, ? extends U> fn) {
        return uniCall(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenCallAsync(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return uniCall(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniCall(int executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniCall<>(executor, options, this, promise, fn));
        return promise;
    }

    // endregion

    // region uni-run

    @Override
    public UniCompletionStage<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return uniRun(0, action, ctx, options);
    }

    @Override
    public UniCompletionStage<Void> thenRun(Consumer<? super IContext> action) {
        return uniRun(0, action, null, 0);
    }

    @Override
    public UniCompletionStage<Void> thenRunAsync(Consumer<? super IContext> action) {
        return uniRun(EXE_ASYNC, action, null, 0);
    }

    @Override
    public UniCompletionStage<Void> thenRunAsync(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return uniRun(EXE_ASYNC, action, ctx, options);
    }

    private UniCompletionStage<Void> uniRun(int executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        UniPromise<Void> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniRun<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion

    // region uni-catching

    @Override
    public <X extends Throwable> UniCompletionStage<T> catching(Class<X> exceptionType,
                                                                BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                                @Nullable IContext ctx, int options) {
        return uniCatching(0, exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> catching(Class<X> exceptionType,
                                                                BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        return uniCatching(0, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> catchingAsync(Class<X> exceptionType,
                                                                     BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        return uniCatching(EXE_ASYNC, exceptionType, fallback, null, 0);
    }

    @Override
    public <X extends Throwable> UniCompletionStage<T> catchingAsync(Class<X> exceptionType,
                                                                     BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                                     @Nullable IContext ctx, int options) {
        return uniCatching(EXE_ASYNC, exceptionType, fallback, ctx, options);
    }

    private <X extends Throwable> UniCompletionStage<T> uniCatching(int executor, Class<X> exceptionType,
                                                                    BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                                                    @Nullable IContext ctx, int options) {
        Objects.requireNonNull(exceptionType, "exceptionType");
        Objects.requireNonNull(fallback, "fallback");

        if (ctx == null) ctx = this._ctx;
        UniPromise<T> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniCathing<>(executor, options, this, promise, exceptionType, fallback));
        return promise;
    }
    // endregion

    // region uni-handle

    @Override
    public <U> UniCompletionStage<U> thenHandle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                                @Nullable IContext ctx, int options) {
        return uniHandle(0, fn, ctx, options);
    }

    @Override
    public <U> UniCompletionStage<U> thenHandle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return uniHandle(0, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenHandleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return uniHandle(EXE_ASYNC, fn, null, 0);
    }

    @Override
    public <U> UniCompletionStage<U> thenHandleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                                     @Nullable IContext ctx, int options) {
        return uniHandle(EXE_ASYNC, fn, ctx, options);
    }

    private <U> UniCompletionStage<U> uniHandle(int executor,
                                                TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                                @Nullable IContext ctx, int options) {
        Objects.requireNonNull(fn);

        if (ctx == null) ctx = this._ctx;
        UniPromise<U> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniHandle<>(executor, options, this, promise, fn));
        return promise;
    }
    // endregion

    // region uni-when

    @Override
    public UniCompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return uniWhenComplete(0, action, ctx, options);
    }

    @Override
    public UniCompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return uniWhenComplete(0, action, null, 0);
    }

    @Override
    public UniCompletionStage<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return uniWhenComplete(EXE_ASYNC, action, null, 0);
    }

    @Override
    public UniCompletionStage<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return uniWhenComplete(EXE_ASYNC, action, ctx, options);
    }

    private UniCompletionStage<T> uniWhenComplete(int executor,
                                                  TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                                  @Nullable IContext ctx, int options) {
        Objects.requireNonNull(action);

        if (ctx == null) ctx = this._ctx;
        UniPromise<T> promise = newIncompletePromise(ctx, _executor);
        pushCompletion(new UniWhenComplete<>(executor, options, this, promise, action));
        return promise;
    }
    // endregion

    // region onComplete

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options) {
        uniOnCompletedFuture1(0, action, context, options);
    }

    @Override
    public void onCompletedAsync(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options) {
        uniOnCompletedFuture1(EXE_ASYNC, action, context, options);
    }

    private void uniOnCompletedFuture1(int executor, BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options) {
        Objects.requireNonNull(action, "action");
        Objects.requireNonNull(context, "context");
        if (action instanceof Completion completion) { // 主要是Relay
            pushCompletion(completion);
            return;
        }
        if (this.isDone() && executor == EXE_SYNC) { // listener避免不必要的插入
            UniOnCompleteFuture1.onCompleted(context, this, action, null);
        } else {
            pushCompletion(new UniOnCompleteFuture1<>(executor, options, this, context, action));
        }
    }

    @Override
    public void onCompleted(Consumer<? super UniFuture<T>> action, int options) {
        uniOnCompletedFuture2(EXE_SYNC, action, options);
    }

    @Override
    public void onCompletedAsync(Consumer<? super UniFuture<T>> action, int options) {
        uniOnCompletedFuture2(EXE_ASYNC, action, options);
    }

    private void uniOnCompletedFuture2(int executor, Consumer<? super UniFuture<T>> action, int options) {
        Objects.requireNonNull(action, "action");
        if (action instanceof Completion completion) { // 主要是Relay
            pushCompletion(completion);
            return;
        }
        if (this.isDone() && executor == EXE_SYNC) { // listener避免不必要的插入
            UniOnCompleteFuture2.onCompleted(this, action, null);
        } else {
            pushCompletion(new UniOnCompleteFuture2<>(executor, options, this, action));
        }
    }

    // endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Modes for Completion.tryFire. Signedness matters.
    /**
     * 同步调用模式，表示压栈过程中发现{@code Future}已进入完成状态，从而调用{@link Completion#tryFire(int)}。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(UniPromise)}。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    static final int SYNC = 0;
    /**
     * 异步调用模式，表示提交到{@link Executor}之后调用{@link Completion#tryFire(int)}
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(UniPromise)}。
     * 2. 在该模式，表示已获得执行权限，可立即执行。
     */
    static final int ASYNC = 1;
    /**
     * 嵌套调用模式，表示由{@link #postComplete(UniPromise)}中触发调用。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，不触发目标{@code Future}的完成事件，而是返回目标{@code Future}，由当前{@code Future}代为推送。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    static final int NESTED = -1;

    // 刻意与上面的Mode一致，即使手滑按错了，逻辑也是对的。
    /** 用于表示任务已申领权限  */
    private static final int EXE_CLAIMED = NESTED;
    /** 表示这是一个同步任务 */
    private static final int EXE_SYNC = SYNC;
    /** 表示这是一个异步任务 */
    private static final int EXE_ASYNC = ASYNC;

    /** @return 是否压栈成功 */
    private boolean pushCompletion(Completion newHead) {
        if (isDone()) {
            newHead.tryFire(SYNC);
            return false;
        }
        newHead.next = this.stack;
        this.stack = newHead;
        return true;
    }

    /**
     * 推送future的完成事件。
     * - 声明为静态会更清晰易懂
     */
    private static void postComplete(UniPromise<?> future) {
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
    private static <T> Completion clearListeners(UniPromise<T> promise, Completion onto) {
        // 我们需要进行三件事
        // 1. 原子方式将当前Listeners赋值为TOMBSTONE，因为pushCompletion添加的监听器的可见性是由CAS提供的。
        // 2. 将当前栈内元素逆序，因为即使在接口层进行了说明（不提供监听器执行时序保证），但仍然有人依赖于监听器的执行时序(期望先添加的先执行)
        // 3. 将逆序后的元素插入到'onto'前面，即插入到原本要被通知的下一个监听器的前面
        Completion head = promise.stack;
        if (head == TOMBSTONE) {
            return onto;
        }
        promise.stack = TOMBSTONE;

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
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

        /** 单线程模式下 - 无需额外保护 */
        Completion next;

        @Override
        public final void run() {
            tryFire(ASYNC);
        }

        /**
         * 当依赖的某个{@code Future}进入完成状态时，该方法会被调用。
         * 如果tryFire使得另一个{@code Future}进入完成状态，分两种情况：
         * 1. mode指示不要调用{@link #postComplete(UniPromise)}方法时，则返回新进入完成状态的{@code Future}。
         * 2. mode指示可以调用{@link #postComplete(UniPromise)}方法时，则直接推送其进入完成状态的事件。
         * <p>
         * Q: 为什么没有{@code Future}参数？
         * A: 因为调用者可能是其它{@link Future}...
         *
         * @implNote tryFire不可以抛出异常，否则会导致其它监听器也丢失信号
         */
        abstract UniPromise<?> tryFire(int mode);

    }

    /** 表示stack已被清理 */
    private static final Completion TOMBSTONE = new Completion() {
        @Override
        UniPromise<Object> tryFire(int mode) {
            return null;
        }
    };

    /**
     * {@link UniCompletion}接收一个输入的计算
     *
     * @param <V> 输入值类型
     * @param <U> 输入值类型
     */
    static abstract class UniCompletion<V, U> extends Completion {

        int executor;
        int options;
        UniPromise<V> input;
        UniPromise<U> output;

        public UniCompletion(int executor, int options, UniPromise<V> input, UniPromise<U> output) {
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
            int preState = this.executor;
            if (preState == EXE_CLAIMED) {
                return true;
            }
            if (!output.trySetComputing()) { // 被用户取消
                throw StacklessCancellationException.INSTANCE;
            }
            this.executor = EXE_CLAIMED;
            if (preState == EXE_ASYNC) { // input和output的executor相同
                return submit(this, input.executor(), options);
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
        if (!TaskOption.isEnabled(options, TaskOption.STAGE_NON_TRANSITIVE)
                && e instanceof IExecutor exe) {
            exe.execute(completion, options);
        } else {
            e.execute(completion);
        }
        return false;
    }

    private static <U> UniPromise<U> postFire(UniPromise<U> output, int mode, boolean setCompleted) {
        if (!setCompleted) { // 未竞争成功
            return null;
        }
        if (mode < 0) { // 嵌套模式
            return output;
        }
        postComplete(output);
        return null;
    }

    private static <U> boolean tryTransferTo(final UniFuture<? extends U> input, final UniPromise<U> output) {
        if (input instanceof UniPromise<? extends U> promise) {
            Object r = promise.result;
            if (isDone0(r)) {
                return output.completeRelay(r);
            }
            return false;
        }
        // 有可能是Readonly或其它实现
        FutureState state = input.futureState();
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

    private static class UniRelay<V> extends Completion implements Consumer<UniFuture<? extends V>> {

        UniFuture<? extends V> input;
        UniPromise<V> output;

        public UniRelay(UniFuture<? extends V> input, UniPromise<V> output) {
            this.input = input;
            this.output = output;
        }

        @Override
        public void accept(UniFuture<? extends V> iFuture) {
            tryFire(SYNC);
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniFuture<? extends V> input = this.input;
            final UniPromise<V> output = this.output;
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

        BiFunction<? super IContext, ? super V, ? extends UniCompletionStage<U>> fn;

        public UniComposeApply(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                               BiFunction<? super IContext, ? super V, ? extends UniCompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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
                    UniFuture<U> relay = fn.apply(output._ctx, input.decodeValue(r)).toFuture();
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

        Function<? super IContext, ? extends UniCompletionStage<U>> fn;

        public UniComposeCall(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                              Function<? super IContext, ? extends UniCompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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
                    UniFuture<U> relay = fn.apply(output._ctx).toFuture();
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
        BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<V>> fallback;

        public UniComposeCathing(int executor, int options, UniPromise<V> input, UniPromise<V> output,
                                 Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<V>> fallback) {
            super(executor, options, input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<V> output = this.output;
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
                    UniFuture<V> relay = fallback.apply(output._ctx, exceptionType.cast(altResult.cause)).toFuture();
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

        TriFunction<? super IContext, ? super V, ? super Throwable, ? extends UniCompletionStage<U>> fn;

        public UniComposeHandle(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                                TriFunction<? super IContext, ? super V, ? super Throwable, ? extends UniCompletionStage<U>> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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
                    UniFuture<U> relay;
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

        public UniApply(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                        BiFunction<? super IContext, ? super V, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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

        public UniAccept(int executor, int options, UniPromise<V> input, UniPromise<Void> output,
                         BiConsumer<? super IContext, ? super V> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<Void> output = this.output;
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

        public UniCall(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                       Function<? super IContext, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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

        public UniRun(int executor, int options, UniPromise<V> input, UniPromise<Void> output,
                      Consumer<? super IContext> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<Void> output = this.output;
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

        public UniCathing(int executor, int options, UniPromise<V> input, UniPromise<V> output,
                          Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
            super(executor, options, input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<V> output = this.output;
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

        public UniHandle(int executor, int options, UniPromise<V> input, UniPromise<U> output,
                         TriFunction<? super IContext, ? super V, ? super Throwable, ? extends U> fn) {
            super(executor, options, input, output);
            this.fn = fn;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<U> output = this.output;
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

        public UniWhenComplete(int executor, int options, UniPromise<V> input, UniPromise<V> output,
                               TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
            super(executor, options, input, output);
            this.action = action;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            final UniPromise<V> output = this.output;
            boolean setCompleted;
            tryComplete:
            {
                if (output.isDone()) { // 用户取消可能导致与上游结果不同
                    setCompleted = false;
                    break tryComplete;
                }
                // UniWhenComplete与其它节点不同，需要保持相同的结果 -- 因此这里不处理取消
                Object r = input.result;
                try {
                    if (mode <= 0 && !claim()) {
                        return null; // 等待下次执行
                    }
                    if (r instanceof AltResult altResult) {
                        action.accept(output._ctx, null, altResult.cause);
                    } else {
                        action.accept(output._ctx, input.decodeValue(r), null);
                    }
                } catch (Throwable e) {
                    FutureLogger.logCause(e, "UniWhenComplete caught an exception");
                } finally {
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

        int executor;
        int options;
        UniPromise<V> input;

        public UniOnComplete(int executor, int options, UniPromise<V> input) {
            this.options = options;
            this.executor = executor;
            this.input = input;
        }

        final boolean claim() {
            int preState = this.executor;
            if (preState == EXE_CLAIMED) {
                return true;
            }
            this.executor = EXE_CLAIMED;
            if (preState == EXE_ASYNC) { // input和output的executor相同
                return submit(this, input.executor(), options);
            }
            return true;
        }
    }

    private static class UniOnCompleteFuture1<V> extends UniOnComplete<V> {

        IContext ctx;
        BiConsumer<? super IContext, ? super UniFuture<V>> action;

        public UniOnCompleteFuture1(int executor, int options, UniPromise<V> input, IContext ctx,
                                    BiConsumer<? super IContext, ? super UniFuture<V>> action) {
            super(executor, options, input);
            this.ctx = ctx;
            this.action = action;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            tryComplete:
            {
                if (ctx.cancelToken().isCancelling()) {
                    break tryComplete;
                }
                // 异步模式下已经claim
                if (!onCompleted(ctx, input, action, mode > 0 ? null : this)) {
                    return null;
                }
            }
            // help gc
            this.input = null;
            this.ctx = null;
            this.action = null;
            return null;
        }

        static <V> boolean onCompleted(IContext ctx, UniPromise<V> input,
                                       BiConsumer<? super IContext, ? super UniFuture<V>> action,
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

        Consumer<? super UniFuture<V>> action;

        public UniOnCompleteFuture2(int executor, int options, UniPromise<V> input,
                                    Consumer<? super UniFuture<V>> action) {
            super(executor, options, input);
            this.action = action;
        }

        @Override
        UniPromise<?> tryFire(int mode) {
            final UniPromise<V> input = this.input;
            // 异步模式下已经claim
            if (!onCompleted(input, action, mode > 0 ? null : this)) {
                return null;
            }
            // help gc
            this.input = null;
            this.action = null;
            return null;
        }

        static <V> boolean onCompleted(UniPromise<V> input,
                                       Consumer<? super UniFuture<V>> action,
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
