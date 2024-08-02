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

package cn.wjybxx.sequential;

import cn.wjybxx.concurrent.*;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.function.Consumer;

/**
 * 单线程化改动：
 * 1.计数变量改为普通变量
 * 2.Promise的默认实例为{@link UniPromise}
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public final class UniFutureCombiner {

    private ChildListener childrenListener = new ChildListener();
    private final Executor executor;
    private IPromise<Object> aggregatePromise;
    private int futureCount;

    public UniFutureCombiner(Executor executor) {
        this.executor = Objects.requireNonNull(executor);
    }

    //region
    public UniFutureCombiner add(IFuture<?> future) {
        Objects.requireNonNull(future);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
        ++futureCount;
        future.onCompleted(childrenListener, 0);
        return this;
    }

    /**
     * 获取监听的future数量
     * 注意：future计数是不去重的，一个future反复添加会反复计数
     */
    public int futureCount() {
        return futureCount;
    }

    /**
     * 设置接收结果的Promise
     * 如果在执行操作前没有指定Promise，将创建{@link Promise}实例。
     *
     * @return this
     */
    public UniFutureCombiner setAggregatePromise(IPromise<Object> aggregatePromise) {
        this.aggregatePromise = aggregatePromise;
        return this;
    }

    /**
     * 重置状态，使得可以重新添加future和选择
     */
    public void clear() {
        futureCount = 0;
        childrenListener = new ChildListener();
    }

    // endregion

    // region

    /**
     * 返回的promise在任意future进入完成状态时进入完成状态
     * 返回的promise与首个future的结果相同
     */
    public IPromise<Object> anyOf() {
        return finish(AggregateOptions.anyOf());
    }

    /**
     * 要求所有的future都成功时才进入成功状态；
     * 任意任务失败，最终结果都表现为失败
     *
     * @param failFast 是否在不满足条件时立即失败
     */
    public IPromise<Object> selectN(int successRequire, boolean failFast) {
        return finish(AggregateOptions.selectN(successRequire, failFast));
    }

    /**
     * 要求所有的future都成功时才进入成功状态
     * 一旦有任务失败则立即失败
     */
    public IPromise<Object> selectAll() {
        return selectN(futureCount(), true);
    }

    /**
     * 要求所有的future都成功时才进入成功状态；
     * 任意任务失败，最终结果都表现为失败
     *
     * @param failFast 是否在不满足条件时立即失败
     */
    public IPromise<Object> selectAll(boolean failFast) {
        return selectN(futureCount(), failFast);
    }

    // endregion

    // region 内部实现

    private IPromise<Object> finish(AggregateOptions options) {
        Objects.requireNonNull(options);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Already finished");
        }
        this.childrenListener = null;

        IPromise<Object> aggregatePromise = this.aggregatePromise;
        if (aggregatePromise == null) {
            aggregatePromise = new UniPromise<>(executor);
        } else {
            this.aggregatePromise = null;
        }

        childrenListener.futureCount = futureCount;
        childrenListener.options = options;
        childrenListener.aggregatePromise = aggregatePromise;
        childrenListener.checkComplete();
        return aggregatePromise;
    }

    public UniFutureCombiner addAll(IFuture<?>... futures) {
        for (IFuture<?> future : futures) {
            this.add(future);
        }
        return this;
    }

    public UniFutureCombiner addAll(Collection<? extends IFuture<?>> futures) {
        for (IFuture<?> future : futures) {
            this.add(future);
        }
        return this;
    }

    private static class ChildListener implements Consumer<IFuture<?>> {

        private int succeedCount;
        private int doneCount;

        private Object result;
        private Throwable cause;

        private int futureCount;
        private AggregateOptions options;
        private IPromise<Object> aggregatePromise;

        @Override
        public void accept(IFuture<?> future) {
            if (future.isFailed()) {
                accept(null, future.exceptionNow(false));
            } else {
                accept(future.resultNow(), null);
            }
        }

        public void accept(Object r, Throwable throwable) {
            if (throwable == null) {
                result = encodeValue(r);
                succeedCount++;
            } else if (cause == null) { // 暂时保留第一个异常
                cause = throwable;
            }
            doneCount++;

            IPromise<Object> aggregatePromise = this.aggregatePromise;
            if (aggregatePromise != null && !aggregatePromise.isDone() && checkComplete()) {
                result = null;
                cause = null;
            }
        }

        boolean checkComplete() {
            int doneCount = this.doneCount;
            int succeedCount = this.succeedCount;

            // 没有任务，立即完成
            if (futureCount == 0) {
                return aggregatePromise.trySetResult(null);
            }
            if (options.isAnyOf()) {
                if (doneCount == 0) {
                    return false;
                }
                if (result != null) { // anyOf下尽量返回成功
                    return aggregatePromise.trySetResult(decodeValue(result));
                } else {
                    return aggregatePromise.trySetException(cause);
                }
            }

            // 懒模式需要等待所有任务完成
            if (!options.failFast && doneCount < futureCount) {
                return false;
            }
            // 包含了require小于等于0的情况
            final int successRequire = options.successRequire;
            if (succeedCount >= successRequire) {
                return aggregatePromise.trySetResult(null);
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                if (cause == null) {
                    cause = TaskInsufficientException.create(futureCount, doneCount, succeedCount, successRequire);
                }
                return aggregatePromise.trySetException(cause);
            }
            return false;
        }
    }

    private static final Object NIL = new Object();

    private static Object encodeValue(Object val) {
        return val == null ? NIL : val;
    }

    private static Object decodeValue(Object r) {
        return r == NIL ? null : r;
    }

    // endregion
}