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

import cn.wjybxx.base.function.TriConsumer;
import cn.wjybxx.base.function.TriFunction;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 提供转发功能的基类
 * 由于Future中存在取消接口，因此该类还不是Readonly的。
 *
 * @author wjybxx
 * date - 2024/1/9
 */
public class ForwardFuture<V> implements IFuture<V> {

    protected final IFuture<V> future;

    public ForwardFuture(IFuture<V> future) {
        this.future = Objects.requireNonNull(future);
    }

    @Override
    @Nonnull
    public final IFuture<V> toFuture() {
        return this; // 不能转发
    }

    // region ctx

    @Nonnull
    @Override
    public IContext ctx() {
        return future.ctx();
    }

    @Override
    @Nullable
    public Executor executor() {
        return future.executor();
    }

    @Override
    public IFuture<V> asReadonly() {
        return future.asReadonly();
    }

    @SuppressWarnings("deprecation")
    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        return future.cancel(mayInterruptIfRunning);
    }

    // endregion

    // region state

    @Override
    public State state() {
        return future.state();
    }

    @Override
    public FutureState futureState() {
        return future.futureState();
    }

    @Override
    public boolean isPending() {
        return future.isPending();
    }

    @Override
    public boolean isComputing() {
        return future.isComputing();
    }

    @Override
    public boolean isDone() {
        return future.isDone();
    }

    @Override
    public boolean isCancelled() {
        return future.isCancelled();
    }

    @Override
    public boolean isSucceeded() {
        return future.isSucceeded();
    }

    @Override
    public boolean isFailed() {
        return future.isFailed();
    }

    @Override
    public boolean isFailedOrCancelled() {
        return future.isFailedOrCancelled();
    }

    @Override
    public V getNow() {
        return future.getNow();
    }

    @Override
    public V getNow(V valueIfAbsent) {
        return future.getNow(valueIfAbsent);
    }

    @Override
    public V resultNow() {
        return future.resultNow();
    }

    @Override
    public Throwable exceptionNow() {
        return future.exceptionNow();
    }

    @Override
    public Throwable exceptionNow(boolean throwIfCancelled) {
        return future.exceptionNow(throwIfCancelled);
    }

    @Override
    public V get() throws InterruptedException, ExecutionException {
        return future.get();
    }

    @Override
    public V get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        return future.get(timeout, unit);
    }

    @Override
    public boolean await(long timeout, TimeUnit unit) throws InterruptedException {
        return future.await(timeout, unit);
    }

    @Override
    public boolean awaitUninterruptibly(long timeout, TimeUnit unit) {
        return future.awaitUninterruptibly(timeout, unit);
    }

    @Override
    public IFuture<V> await() throws InterruptedException {
        return future.await();
    }

    @Override
    public IFuture<V> awaitUninterruptibly() {
        return future.awaitUninterruptibly();
    }

    @Override
    public V join() {
        return future.join();
    }

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super IFuture<V>> action, @Nonnull IContext context, int options) {
        future.onCompleted(action, context, options);
    }

    @Override
    public void onCompletedAsync(Executor executor, BiConsumer<? super IContext, ? super IFuture<V>> action, @Nonnull IContext context, int options) {
        future.onCompletedAsync(executor, action, context, options);
    }

    @Override
    public void onCompleted(Consumer<? super IFuture<V>> action, int options) {
        future.onCompleted(action, options);
    }

    @Override
    public void onCompletedAsync(Executor executor, Consumer<? super IFuture<V>> action, int options) {
        future.onCompletedAsync(executor, action, options);
    }

    // endregion

    // region stage

    @Override
    public <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeApply(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn) {
        return future.composeApply(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn) {
        return future.composeApplyAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeApplyAsync(executor, fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeCall(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        return future.composeCall(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        return future.composeCallAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeCallAsync(executor, fn, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback, @Nullable IContext ctx, int options) {
        return future.composeCatching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback) {
        return future.composeCatching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback) {
        return future.composeCatchingAsync(executor, exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback, @Nullable IContext ctx, int options) {
        return future.composeCatchingAsync(executor, exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeHandle(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        return future.composeHandle(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        return future.composeHandleAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeHandleAsync(executor, fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super V, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenApply(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super V, ? extends U> fn) {
        return future.thenApply(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends U> fn) {
        return future.thenApplyAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenApplyAsync(executor, fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenCall(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn) {
        return future.thenCall(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn) {
        return future.thenCallAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenCallAsync(executor, fn, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super V> action, @Nullable IContext ctx, int options) {
        return future.thenAccept(action, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super V> action) {
        return future.thenAccept(action);
    }

    @Override
    public ICompletionStage<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super V> action) {
        return future.thenAcceptAsync(executor, action);
    }

    @Override
    public ICompletionStage<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super V> action, @Nullable IContext ctx, int options) {
        return future.thenAcceptAsync(executor, action, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return future.thenRun(action, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenRun(Consumer<? super IContext> action) {
        return future.thenRun(action);
    }

    @Override
    public ICompletionStage<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action) {
        return future.thenRunAsync(executor, action);
    }

    @Override
    public ICompletionStage<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return future.thenRunAsync(executor, action, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback, @Nullable IContext ctx, int options) {
        return future.catching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
        return future.catching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
        return future.catchingAsync(executor, exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback, @Nullable IContext ctx, int options) {
        return future.catchingAsync(executor, exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenHandle(TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenHandle(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenHandle(TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn) {
        return future.thenHandle(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn) {
        return future.thenHandleAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenHandleAsync(executor, fn, ctx, options);
    }

    @Override
    public ICompletionStage<V> whenComplete(TriConsumer<? super IContext, ? super V, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return future.whenComplete(action, ctx, options);
    }

    @Override
    public ICompletionStage<V> whenComplete(TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
        return future.whenComplete(action);
    }

    @Override
    public ICompletionStage<V> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
        return future.whenCompleteAsync(executor, action);
    }

    @Override
    public ICompletionStage<V> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super V, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return future.whenCompleteAsync(executor, action, ctx, options);
    }

    // endregion
}