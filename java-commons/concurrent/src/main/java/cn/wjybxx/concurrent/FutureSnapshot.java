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

import cn.wjybxx.base.annotation.Beta;

import java.util.Objects;
import java.util.concurrent.CancellationException;

/**
 * @author wjybxx
 * date - 2024/1/11
 */
@Beta
public final class FutureSnapshot<T> {

    private final FutureState state;
    private final Object result;

    private FutureSnapshot(FutureState state, Object result) {
        this.state = state;
        this.result = result;
    }

    public FutureState state() {
        return state;
    }

    public boolean isDone() {
        return state.isDone();
    }

    public boolean isPending() {
        return state == FutureState.PENDING;
    }

    public boolean isComputing() {
        return state == FutureState.COMPUTING;
    }

    public boolean isSucceeded() {
        return state == FutureState.SUCCESS;
    }

    public boolean isFailed() {
        return state == FutureState.FAILED;
    }

    public boolean isCancelled() {
        return state == FutureState.CANCELLED;
    }

    public boolean isFailedOrCancelled() {
        return state.isFailedOrCancelled();
    }

    /**
     * Q：为什么不抛出异常？
     * A：因为这是快照呀，不要搞那么复杂。{@link IFuture}是为了兼容JDK，才保持JDK的约定的。
     *
     * @return 非成功完成时返回null
     */
    @SuppressWarnings("unchecked")
    public T getResult() {
        if (state == FutureState.SUCCESS) {
            return (T) result;
        }
        return null;
    }

    /** @return 非成功完成时返回null */
    public Throwable getException() {
        if (state.isFailedOrCancelled()) {
            return (Throwable) result;
        }
        return null;
    }

    // region factory
    /** 排队中 */
    private static final FutureSnapshot<?> PENDING = new FutureSnapshot<>(FutureState.PENDING, null);
    /** 计算中 */
    private static final FutureSnapshot<?> COMPUTING = new FutureSnapshot<>(FutureState.COMPUTING, null);
    /** 成功但没有结果 */
    private static final FutureSnapshot<?> NIL = new FutureSnapshot<>(FutureState.SUCCESS, null);

    public static <T> FutureSnapshot<T> of(IFuture<T> future) {
        FutureState state = future.futureState();
        return switch (state) {
            case PENDING -> pending();
            case COMPUTING -> computing();
            case SUCCESS -> succeeded(future.getNow());
            default -> failed(future.exceptionNow(false));
        };
    }

    @SuppressWarnings("unchecked")
    public static <T> FutureSnapshot<T> pending() {
        return (FutureSnapshot<T>) PENDING;
    }

    @SuppressWarnings("unchecked")
    public static <T> FutureSnapshot<T> computing() {
        return (FutureSnapshot<T>) COMPUTING;
    }

    @SuppressWarnings("unchecked")
    public static <T> FutureSnapshot<T> succeeded() {
        return (FutureSnapshot<T>) NIL;
    }

    @SuppressWarnings("unchecked")
    public static <U> FutureSnapshot<U> succeeded(U result) {
        if (result == null) {
            return (FutureSnapshot<U>) NIL;
        }
        return new FutureSnapshot<>(FutureState.SUCCESS, result);
    }

    public static <U> FutureSnapshot<U> failed(Throwable ex) {
        Objects.requireNonNull(ex);
        if (ex instanceof CancellationException) {
            return new FutureSnapshot<>(FutureState.CANCELLED, ex);
        }
        return new FutureSnapshot<>(FutureState.FAILED, ex);
    }
    // endregion

}