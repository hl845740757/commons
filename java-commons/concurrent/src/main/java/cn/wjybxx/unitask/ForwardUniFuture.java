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

package cn.wjybxx.unitask;

import cn.wjybxx.base.function.TriConsumer;
import cn.wjybxx.base.function.TriFunction;
import cn.wjybxx.concurrent.FutureState;
import cn.wjybxx.concurrent.IContext;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date - 2024/1/13
 */
@NotThreadSafe
public class ForwardUniFuture<T> implements UniFuture<T> {

    protected final UniFuture<T> future;

    public ForwardUniFuture(UniFuture<T> future) {
        this.future = Objects.requireNonNull(future);
    }

    @Nonnull
    @Override
    public UniFuture<T> toFuture() {
        return this; // 不能转发
    }

    // region future
    @Override
    @Nonnull
    public IContext ctx() {
        return future.ctx();
    }

    @Override
    @Nonnull
    public Executor executor() {
        return future.executor();
    }

    @Override
    public UniFuture<T> asReadonly() {
        return future.asReadonly();
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
    public T getNow() {
        return future.getNow();
    }

    @Override
    public T getNow(T valueIfAbsent) {
        return future.getNow(valueIfAbsent);
    }

    @Override
    public T resultNow() {
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
    public T get() throws ExecutionException {
        return future.get();
    }

    @Override
    public T join() {
        return future.join();
    }

    @Override
    public boolean tryTransferTo(UniPromise<T> output) {
        return future.tryTransferTo(output);
    }

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options) {
        future.onCompleted(action, context, options);
    }

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context) {
        future.onCompleted(action, context);
    }

    @Override
    public void onCompletedAsync(Executor executor, BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context) {
        future.onCompletedAsync(executor, action, context);
    }

    @Override
    public void onCompletedAsync(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options) {
        future.onCompletedAsync(action, context, options);
    }

    @Override
    public void onCompleted(Consumer<? super UniFuture<T>> action, int options) {
        future.onCompleted(action, options);
    }

    @Override
    public void onCompleted(Consumer<? super UniFuture<T>> action) {
        future.onCompleted(action);
    }

    @Override
    public void onCompletedAsync(Executor executor, Consumer<? super UniFuture<T>> action) {
        future.onCompletedAsync(executor, action);
    }

    @Override
    public void onCompletedAsync(Consumer<? super UniFuture<T>> action, int options) {
        future.onCompletedAsync(action, options);
    }

    // endregion

    // region stage

    @Override
    public <U> UniFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeApply(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn) {
        return future.composeApply(fn);
    }

    @Override
    public <U> UniFuture<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn) {
        return future.composeApplyAsync(fn);
    }

    @Override
    public <U> UniFuture<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeApplyAsync(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeCall(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn) {
        return future.composeCall(fn);
    }

    @Override
    public <U> UniFuture<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn) {
        return future.composeCallAsync(fn);
    }

    @Override
    public <U> UniFuture<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeCallAsync(fn, ctx, options);
    }

    @Override
    public <X extends Throwable> UniFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback, @Nullable IContext ctx, int options) {
        return future.composeCatching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> UniFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback) {
        return future.composeCatching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> UniFuture<T> composeCatchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback) {
        return future.composeCatchingAsync(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> UniFuture<T> composeCatchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback, @Nullable IContext ctx, int options) {
        return future.composeCatchingAsync(exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> UniFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeHandle(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn) {
        return future.composeHandle(fn);
    }

    @Override
    public <U> UniFuture<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn) {
        return future.composeHandleAsync(fn);
    }

    @Override
    public <U> UniFuture<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return future.composeHandleAsync(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenApply(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn) {
        return future.thenApply(fn);
    }

    @Override
    public <U> UniFuture<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn) {
        return future.thenApplyAsync(fn);
    }

    @Override
    public <U> UniFuture<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenApplyAsync(fn, ctx, options);
    }

    @Override
    public UniFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        return future.thenAccept(action, ctx, options);
    }

    @Override
    public UniFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action) {
        return future.thenAccept(action);
    }

    @Override
    public UniFuture<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action) {
        return future.thenAcceptAsync(action);
    }

    @Override
    public UniFuture<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options) {
        return future.thenAcceptAsync(action, ctx, options);
    }

    @Override
    public <U> UniFuture<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenCall(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> thenCall(Function<? super IContext, ? extends U> fn) {
        return future.thenCall(fn);
    }

    @Override
    public <U> UniFuture<U> thenCallAsync(Function<? super IContext, ? extends U> fn) {
        return future.thenCallAsync(fn);
    }

    @Override
    public <U> UniFuture<U> thenCallAsync(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.thenCallAsync(fn, ctx, options);
    }

    @Override
    public UniFuture<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return future.thenRun(action, ctx, options);
    }

    @Override
    public UniFuture<Void> thenRun(Consumer<? super IContext> action) {
        return future.thenRun(action);
    }

    @Override
    public UniFuture<Void> thenRunAsync(Consumer<? super IContext> action) {
        return future.thenRunAsync(action);
    }

    @Override
    public UniFuture<Void> thenRunAsync(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return future.thenRunAsync(action, ctx, options);
    }

    @Override
    public <X extends Throwable> UniFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options) {
        return future.catching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> UniFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        return future.catching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> UniFuture<T> catchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback) {
        return future.catchingAsync(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> UniFuture<T> catchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options) {
        return future.catchingAsync(exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> UniFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.handle(fn, ctx, options);
    }

    @Override
    public <U> UniFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return future.handle(fn);
    }

    @Override
    public <U> UniFuture<U> handleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn) {
        return future.handleAsync(fn);
    }

    @Override
    public <U> UniFuture<U> handleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return future.handleAsync(fn, ctx, options);
    }

    @Override
    public UniFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return future.whenComplete(action, ctx, options);
    }

    @Override
    public UniFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return future.whenComplete(action);
    }

    @Override
    public UniFuture<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action) {
        return future.whenCompleteAsync(action);
    }

    @Override
    public UniFuture<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return future.whenCompleteAsync(action, ctx, options);
    }


    // endregion
}