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

package cn.wjybxx.common.concurrent2;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 实现参考{@link CompletableFuture}
 * <p>
 * 功力有限，为了降低难度和复杂度，实现上进行了一定的简化，缺少{@link CompletableFuture}中许多优化。
 * 不然过于复杂，难以保证正确性，且难以维护。
 *
 * @author wjybxx
 * @version 1.0
 * date - 2020/4/30
 * github - https://github.com/hl845740757
 */
public class AbstractFluentPromise<V> extends Promise<V> {


    AbstractFluentPromise() {
    }


    @Override
    public <U> Promise<U> thenCompose(@Nonnull Function<? super V, ? extends ListenableFuture<U>> fn) {
        return unitComposeApplyStage(null, fn);
    }

    @Override
    public <U> Promise<U> thenComposeAsync(@Nonnull Function<? super V, ? extends ListenableFuture<U>> fn, Executor executor) {
        return unitComposeApplyStage(Objects.requireNonNull(executor, "executor"), fn);
    }

    private <U> Promise<U> unitComposeApplyStage(Executor executor, @Nonnull Function<? super V, ? extends ListenableFuture<U>> fn) {
        final Promise<U> promise = newIncompletePromise();
        pushCompletion(new UniComposeApply<>(executor, this, promise, fn));
        return promise;
    }

    @Override
    public <U> Promise<U> thenCompose(@Nonnull Callable<? extends ListenableFuture<U>> fn) {
        return uniComposeCallStage(null, fn);
    }

    @Override
    public <U> Promise<U> thenComposeAsync(@Nonnull Callable<? extends ListenableFuture<U>> fn, Executor executor) {
        return uniComposeCallStage(Objects.requireNonNull(executor, "executor"), fn);
    }

    private <U> Promise<U> uniComposeCallStage(Executor executor, @Nonnull Callable<? extends ListenableFuture<U>> fn) {
        final Promise<U> promise = newIncompletePromise();
        pushCompletion(new UniComposeCall<>(executor, this, promise, fn));
        return promise;
    }

    @Override
    public Promise<Void> thenRun(@Nonnull Runnable action) {
        return uniRunStage(null, action);
    }

    @Override
    public Promise<Void> thenRunAsync(@Nonnull Runnable action, Executor executor) {
        return uniRunStage(Objects.requireNonNull(executor, "executor"), action);
    }

    private Promise<Void> uniRunStage(Executor executor, @Nonnull Runnable action) {
        final Promise<Void> promise = newIncompletePromise();
        pushCompletion(new UniRun<>(executor, this, promise, action));
        return promise;
    }

    @Override
    public <U> Promise<U> thenCall(@Nonnull Callable<U> fn) {
        return uniCallStage(null, fn);
    }

    @Override
    public <U> Promise<U> thenCallAsync(@Nonnull Callable<U> fn, Executor executor) {
        return uniCallStage(Objects.requireNonNull(executor, "executor"), fn);
    }

    private <U> Promise<U> uniCallStage(Executor executor, @Nonnull Callable<U> fn) {
        final Promise<U> promise = newIncompletePromise();
        pushCompletion(new UniCall<>(executor, this, promise, fn));
        return promise;
    }

    @Override
    public Promise<Void> thenAccept(@Nonnull Consumer<? super V> action) {
        return uniAcceptStage(null, action);
    }

    @Override
    public Promise<Void> thenAcceptAsync(@Nonnull Consumer<? super V> action, Executor executor) {
        return uniAcceptStage(Objects.requireNonNull(executor, "executor"), action);
    }

    private Promise<Void> uniAcceptStage(Executor executor, @Nonnull Consumer<? super V> action) {
        final Promise<Void> promise = newIncompletePromise();
        pushCompletion(new UniAccept<>(executor, this, promise, action));
        return promise;
    }

    @Override
    public <U> Promise<U> thenApply(@Nonnull Function<? super V, ? extends U> fn) {
        return uniApplyStage(null, fn);
    }

    @Override
    public <U> Promise<U> thenApplyAsync(@Nonnull Function<? super V, ? extends U> fn, Executor executor) {
        return uniApplyStage(Objects.requireNonNull(executor, "executor"), fn);
    }

    private <U> Promise<U> uniApplyStage(Executor executor, @Nonnull Function<? super V, ? extends U> fn) {
        final Promise<U> promise = newIncompletePromise();
        pushCompletion(new UniApply<>(executor, this, promise, fn));
        return promise;
    }

    @Override
    public <X extends Throwable> Promise<V> catching(@Nonnull Class<X> exceptionType, @Nonnull Function<? super X, ? extends V> fallback) {
        return uniCatchingStage(null, exceptionType, fallback);
    }

    @Override
    public <X extends Throwable>
    Promise<V> catchingAsync(@Nonnull Class<X> exceptionType, @Nonnull Function<? super X, ? extends V> fallback, Executor executor) {
        return uniCatchingStage(Objects.requireNonNull(executor, "executor"), exceptionType, fallback);
    }

    private <X extends Throwable> Promise<V> uniCatchingStage(Executor executor, @Nonnull Class<X> exceptionType, @Nonnull Function<? super X, ? extends V> fallback) {
        final Promise<V> promise = newIncompletePromise();
        pushCompletion(new UniCaching<>(executor, this, promise, exceptionType, fallback));
        return promise;
    }

    @Override
    public <U> Promise<U> thenHandle(@Nonnull BiFunction<? super V, ? super Throwable, ? extends U> fn) {
        return uniHandleStage(null, fn);
    }

    @Override
    public <U> Promise<U> thenHandleAsync(@Nonnull BiFunction<? super V, ? super Throwable, ? extends U> fn, Executor executor) {
        return uniHandleStage(Objects.requireNonNull(executor, "executor"), fn);
    }

    private <U> Promise<U> uniHandleStage(Executor executor, @Nonnull BiFunction<? super V, ? super Throwable, ? extends U> fn) {
        final Promise<U> promise = newIncompletePromise();
        pushCompletion(new UniHandle<>(executor, this, promise, fn));
        return promise;
    }

    @Override
    public Promise<V> whenComplete(@Nonnull BiConsumer<? super V, ? super Throwable> action) {
        return uniWhenCompleteStage(null, action);
    }

    @Override
    public Promise<V> whenCompleteAsync(@Nonnull BiConsumer<? super V, ? super Throwable> action, Executor executor) {
        return uniWhenCompleteStage(Objects.requireNonNull(executor, "executor"), action);
    }

    private Promise<V> uniWhenCompleteStage(Executor executor, @Nonnull BiConsumer<? super V, ? super Throwable> action) {
        final Promise<V> promise = newIncompletePromise();
        pushCompletion(new UniWhenComplete<>(executor, this, promise, action));
        return promise;
    }

    @Override
    public Promise<V> whenExceptionally(@Nonnull Consumer<? super Throwable> action) {
        return uniWhenExceptionallyStage(null, action);
    }

    @Override
    public Promise<V> whenExceptionallyAsync(@Nonnull Consumer<? super Throwable> action, Executor executor) {
        return uniWhenExceptionallyStage(Objects.requireNonNull(executor, "executor"), action);
    }

    private Promise<V> uniWhenExceptionallyStage(Executor executor, @Nonnull Consumer<? super Throwable> action) {
        final Promise<V> promise = newIncompletePromise();
        pushCompletion(new UniWhenExceptionally<>(executor, this, promise, action));
        return promise;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    @Override
    public Promise<V> addListener(@Nonnull FutureListener<? super V> listener) {
        return addListener0(null, Objects.requireNonNull(listener, "listener"));
    }

    @Override
    public Promise<V> addListener(@Nonnull FutureListener<? super V> listener, @Nonnull Executor executor) {
        return addListener0(Objects.requireNonNull(executor, "executor"), Objects.requireNonNull(listener, "listener"));
    }

    private Promise<V> addListener0(Executor executor, FutureListener<? super V> listener) {
        if (listener instanceof Completion) {
            assert executor == null;
            pushCompletion((Completion) listener);
        } else {
            pushCompletion(new ListenWhenComplete<>(executor, this, listener));
        }
        return this;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /**
     * 创建一个具体的子类型对象，用于作为各个方法的返回值。
     */
    protected abstract <U> Promise<U> newIncompletePromise();

    // -------------------------------------------------- UniCompletion ---------------------------------------------------------------

    /**
     * {@link UniCompletion}表示一元函数计算，因此它持有一个输入，一个动作，和一个输出。
     *
     * @param <V> 输入值类型
     * @param <U> 输入值类型
     */
    static abstract class UniCompletion<V, U> extends Completion {

        Executor executor;
        Promise<V> input;
        Promise<U> output;

        UniCompletion(Executor executor, Promise<V> input, Promise<U> output) {
            this.executor = executor;
            this.input = input;
            this.output = output;
        }

        /**
         * 当{@link Completion}满足触发条件时，如果是{@link #SYNC}和{@link #NESTED}模式，则调用该方法抢占执行权限。
         * 如果{@link Completion}有多个触发条件，则可能并发调用{@link #tryFire(int)}，而只有一个线程应该执行特定逻辑。
         * {@link #ASYNC}模式表示已抢得执行权限，但是不能在当前线程执行。
         * <p>
         * 这个名字很难搞....
         * <p>
         * 注意：这里之所以和{@link CompletableFuture}不同，是因为在目前的设计中，{@link Completion#tryFire(int)}不会被并发调用。
         *
         * @return 如果成功抢占权限且可以立即执行则返回true，否则返回false
         */
        final boolean claim() {
            Executor e = executor;
            if (e == null || EventLoopUtils.inEventLoop(e)) {
                executor = null;
                return true;
            }
            // disable and help gc
            executor = null;
            e.execute(this);
            return false;
        }
    }

    private static <U> Promise<U> postFire(@Nonnull Promise<U> output, int mode) {
        if (isNestedMode(mode)) {
            return output;
        } else {
            postComplete(output);
            return null;
        }
    }

    static class UniComposeApply<V, U> extends UniCompletion<V, U> {

        Function<? super V, ? extends ListenableFuture<U>> fn;

        UniComposeApply(Executor executor, Promise<V> input, Promise<U> output,
                        Function<? super V, ? extends ListenableFuture<U>> fn) {
            super(executor, input, output);
            this.fn = fn;
        }

        /**
         * {@link #input}执行成功的情况下才执行
         */
        @Override
        Promise<?> tryFire(int mode) {
            Promise<U> out = output;

            // 一直以为循环才能带标签...
            tryComplete:
            if (!isDone0(out.result)) {
                Promise<V> in = input;
                Object inResult = in.result;

                if (inResult instanceof AltResult) {
                    // 上一步异常完成，不执行给定动作，直接完成(当前completion只是简单中继)
                    out.completeRelayThrowable((AltResult) inResult);
                    break tryComplete;
                }

                try {
                    if (isSyncOrNestedMode(mode) && !claim()) {
                        return null;
                    }

                    if (!out.setUncancellable()) {
                        // 设置为不可取消状态失败，意味着future已进入完成状态
                        break tryComplete;
                    }

                    V value = in.decodeValue(inResult);
                    ListenableFuture<U> relay = fn.apply(value);

                    if (relay.isDone()) {
                        // 返回的是已完成的Future
                        completeRelay(out, relay);
                    } else {
                        relay.addListener(new UniRelay<>(relay, out));
                        return null;
                    }
                } catch (Throwable ex) {
                    out.completeThrowable(ex);
                }
            }

            // 走到这里表示，表示该completion已完成，释放内存
            // help gc
            input = null;
            output = null;
            fn = null;

            // 这里表示future一定进入了完成状态，但不一定是当前completion使其完成的，
            // 因此这里可能抢占它的监听器，会导致A线程使其完成，B线程对它通知，
            // 如果客户端未指定Executor，其执行过程可能和所想并不一致
            return postFire(out, mode);
        }
    }

    private static <U> void completeRelay(Promise<U> out, ListenableFuture<U> relay) {
        if (relay instanceof Promise) {
            Object localResult = ((Promise<U>) relay).result;
            out.completeRelay(localResult);
            return;
        }

        // 万一有人实现了别的子类
        try {
            Throwable cause = relay.cause();
            if (cause != null) {
                out.completeRelayThrowable(new AltResult(cause));
            } else {
                out.completeValue(relay.getNow());
            }
        } catch (Throwable ex) {
            out.completeThrowable(ex);
        }
    }

    /**
     * 它较为特殊，并未继承{@link UniCompletion}
     */
    static class UniRelay<V> extends Completion implements FutureListener<V> {

        ListenableFuture<V> input;
        Promise<V> output;

        UniRelay(ListenableFuture<V> input, Promise<V> output) {
            this.input = input;
            this.output = output;
        }

        @Override
        public void onComplete(ListenableFuture<V> future) {
            tryFire(SYNC);
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<V> out = this.output;

            if (out.setUncancellable()) {
                completeRelay(out, this.input);
            }

            // help gc
            output = null;
            input = null;

            return postFire(out, mode);
        }
    }

    static class UniComposeCall<V, U> extends UniCompletion<V, U> {

        Callable<? extends ListenableFuture<U>> fn;

        UniComposeCall(Executor executor, Promise<V> input, Promise<U> output,
                       Callable<? extends ListenableFuture<U>> fn) {
            super(executor, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<U> out = output;

            tryComplete:
            if (!isDone0(out.result)) {
                Object inResult = input.result;

                if (inResult instanceof AltResult) {
                    out.completeRelayThrowable((AltResult) inResult);
                    break tryComplete;
                }

                try {
                    if (isSyncOrNestedMode(mode) && !claim()) {
                        return null;
                    }

                    if (!out.setUncancellable()) {
                        break tryComplete;
                    }

                    ListenableFuture<U> relay = fn.call();

                    if (relay.isDone()) {
                        completeRelay(out, relay);
                    } else {
                        relay.addListener(new UniRelay<>(relay, out));
                        return null;
                    }
                } catch (Throwable ex) {
                    out.completeThrowable(ex);
                }
            }

            // help gc
            input = null;
            output = null;
            fn = null;

            return postFire(out, mode);
        }

    }

    static class UniRun<V> extends UniCompletion<V, Void> {

        Runnable action;

        UniRun(Executor executor, Promise<V> input, Promise<Void> output,
               Runnable action) {
            super(executor, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<Void> out = output;

            if (!isDone0(out.result)) {
                Object inResult = input.result;

                if (inResult instanceof AltResult) {
                    out.completeRelayThrowable((AltResult) inResult);
                } else {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            action.run();
                            out.completeNull();
                        }
                    } catch (Throwable ex) {
                        out.completeThrowable(ex);
                    }
                }
            }

            input = null;
            output = null;
            action = null;

            return postFire(out, mode);
        }

    }

    static class UniCall<V, U> extends UniCompletion<V, U> {

        Callable<U> fn;

        UniCall(Executor executor, Promise<V> input, Promise<U> output,
                Callable<U> fn) {
            super(executor, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<U> out = output;

            if (!isDone0(out.result)) {
                Object inResult = input.result;

                if (inResult instanceof AltResult) {
                    out.completeRelayThrowable((AltResult) inResult);
                } else {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            out.completeValue(fn.call());
                        }
                    } catch (Throwable ex) {
                        out.completeThrowable(ex);
                    }
                }
            }

            input = null;
            output = null;
            fn = null;

            return postFire(out, mode);
        }

    }

    static class UniAccept<V> extends UniCompletion<V, Void> {

        Consumer<? super V> action;

        UniAccept(Executor executor, Promise<V> input, Promise<Void> output,
                  Consumer<? super V> action) {
            super(executor, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<Void> out = output;

            if (!isDone0(out.result)) {
                Promise<V> in = input;
                Object inResult = in.result;

                if (inResult instanceof AltResult) {
                    out.completeRelayThrowable((AltResult) inResult);
                } else {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            V value = in.decodeValue(inResult);
                            action.accept(value);
                            out.completeNull();
                        }
                    } catch (Throwable ex) {
                        out.completeThrowable(ex);
                    }
                }
            }

            input = null;
            output = null;
            action = null;

            return postFire(out, mode);
        }

    }

    static class UniApply<V, U> extends UniCompletion<V, U> {

        Function<? super V, ? extends U> fn;

        UniApply(Executor executor, Promise<V> input, Promise<U> output,
                 Function<? super V, ? extends U> fn) {
            super(executor, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<U> out = output;

            if (!isDone0(out.result)) {
                Promise<V> in = input;
                Object inResult = in.result;

                if (inResult instanceof AltResult) {
                    out.completeRelayThrowable((AltResult) inResult);
                } else {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            V value = in.decodeValue(inResult);
                            out.completeValue(fn.apply(value));
                        }
                    } catch (Throwable ex) {
                        out.completeThrowable(ex);
                    }
                }
            }

            input = null;
            output = null;
            fn = null;

            return postFire(out, mode);
        }
    }

    static class UniCaching<V, X> extends UniCompletion<V, V> {

        Class<X> exceptionType;
        Function<? super X, ? extends V> fallback;

        UniCaching(Executor executor, Promise<V> input, Promise<V> output,
                   Class<X> exceptionType, Function<? super X, ? extends V> fallback) {
            super(executor, input, output);
            this.exceptionType = exceptionType;
            this.fallback = fallback;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<V> out = output;

            if (!isDone0(out.result)) {
                Object inResult = input.result;
                Throwable cause;

                if (inResult instanceof AltResult
                        && exceptionType.isInstance((cause = ((AltResult) inResult).cause))) {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            @SuppressWarnings("unchecked") final X castException = (X) cause;
                            out.completeValue(fallback.apply(castException));
                        }
                    } catch (Throwable ex) {
                        out.completeThrowable(ex);
                    }
                } else {
                    out.completeRelay(inResult);
                }
            }

            input = null;
            output = null;
            exceptionType = null;
            fallback = null;

            return postFire(out, mode);
        }
    }

    static class UniHandle<V, U> extends UniCompletion<V, U> {

        BiFunction<? super V, ? super Throwable, ? extends U> fn;

        UniHandle(Executor executor, Promise<V> input, Promise<U> output,
                  BiFunction<? super V, ? super Throwable, ? extends U> fn) {
            super(executor, input, output);
            this.fn = fn;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<U> out = output;

            if (!isDone0(out.result)) {
                try {
                    if (isSyncOrNestedMode(mode) && !claim()) {
                        return null;
                    }

                    if (out.setUncancellable()) {
                        Promise<V> in = input;
                        Object inResult = in.result;
                        Throwable cause;
                        V value;

                        if (inResult instanceof AltResult) {
                            value = null;
                            cause = ((AltResult) inResult).cause;
                        } else {
                            value = in.decodeValue(inResult);
                            cause = null;
                        }

                        out.completeValue(fn.apply(value, cause));
                    }

                } catch (Throwable ex) {
                    out.completeThrowable(ex);
                }
            }

            input = null;
            output = null;
            fn = null;

            return postFire(out, mode);
        }

    }

    static class UniWhenComplete<V> extends UniCompletion<V, V> {

        BiConsumer<? super V, ? super Throwable> action;

        UniWhenComplete(Executor executor, Promise<V> input, Promise<V> output,
                        BiConsumer<? super V, ? super Throwable> action) {
            super(executor, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<V> out = output;

            if (!isDone0(out.result)) {
                try {
                    if (isSyncOrNestedMode(mode) && !claim()) {
                        return null;
                    }

                    if (out.setUncancellable()) {
                        Promise<V> in = input;
                        Object inResult = in.result;
                        Throwable cause;
                        V value;

                        if (inResult instanceof AltResult) {
                            value = null;
                            cause = ((AltResult) inResult).cause;
                        } else {
                            value = in.decodeValue(inResult);
                            cause = null;
                        }

                        action.accept(value, cause);

                        out.completeRelay(inResult);
                    }
                } catch (Throwable ex) {
                    // 这里的实现与JDK不同，这里仅仅是记录一个异常，不会传递给下一个Future
                    logger.warn("UniWhenComplete.action.accept caught exception", ex);

                    out.completeRelay(input.result);
                }
            }

            input = null;
            output = null;
            action = null;

            return postFire(out, mode);
        }

    }

    static class UniWhenExceptionally<V> extends UniCompletion<V, V> {

        Consumer<? super Throwable> action;

        UniWhenExceptionally(Executor executor, Promise<V> input, Promise<V> output,
                             Consumer<? super Throwable> action) {
            super(executor, input, output);
            this.action = action;
        }

        @Override
        Promise<?> tryFire(int mode) {
            Promise<V> out = output;

            if (!isDone0(out.result)) {
                Object inResult = input.result;

                if (inResult instanceof AltResult) {
                    try {
                        if (isSyncOrNestedMode(mode) && !claim()) {
                            return null;
                        }

                        if (out.setUncancellable()) {
                            action.accept(((AltResult) inResult).cause);
                            out.completeRelay(inResult);
                        }

                    } catch (Throwable ex) {
                        // 这里仅仅是记录一个异常，不会传递给下一个Future
                        logger.warn("UniWhenExceptionally.action.accept caught exception", ex);
                        out.completeRelay(inResult);
                    }
                } else {
                    out.completeRelay(inResult);
                }
            }

            input = null;
            output = null;
            action = null;

            return postFire(out, mode);
        }

    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////

    static abstract class ListenCompletion<V> extends Completion {

        Executor executor;
        Promise<V> input;

        ListenCompletion(Executor executor, Promise<V> input) {
            this.executor = executor;
            this.input = input;
        }

        /**
         * @see UniCompletion#claim()
         */
        final boolean claim() {
            Executor e = executor;
            if (e == null || EventLoopUtils.inEventLoop(e)) {
                executor = null;
                return true;
            }
            // disable and help gc
            executor = null;
            e.execute(this);
            return false;
        }

    }

    static class ListenWhenComplete<V> extends ListenCompletion<V> {

        FutureListener<? super V> listener;

        ListenWhenComplete(Executor executor, Promise<V> input,
                           FutureListener<? super V> listener) {
            super(executor, input);
            this.listener = listener;
        }

        @SuppressWarnings("unchecked")
        @Override
        Promise<?> tryFire(int mode) {
            try {
                if (isSyncOrNestedMode(mode) && !claim()) {
                    return null;
                }

                listener.onComplete((ListenableFuture) input);
            } catch (Throwable ex) {
                logger.warn("ListenWhenComplete.listener.onComplete caught exception", ex);
            }

            input = null;
            listener = null;

            return null;
        }
    }

}
