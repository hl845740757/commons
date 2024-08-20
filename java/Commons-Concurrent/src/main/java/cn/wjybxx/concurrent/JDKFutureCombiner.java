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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.BiConsumer;

/**
 * 在调用选择方法之前，你可以添加任意的{@link CompletableFuture}以进行监听。
 * 调用任意的选择方法后，当前combiner无法继续选择（理论上可以做到支持，但暂时还无需求 -- 主要还是开销大）。
 *
 * @author wjybxx
 * date 2023/4/12
 */
@NotThreadSafe
public class JDKFutureCombiner {

    private ChildListener childrenListener = new ChildListener();
    private CompletableFuture<Object> aggregatePromise;
    private int futureCount;

    public JDKFutureCombiner() {
    }

    // region add

    public JDKFutureCombiner add(CompletionStage<?> future) {
        Objects.requireNonNull(future);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
        ++futureCount;
        future.whenComplete(childrenListener);
        return this;
    }

    public JDKFutureCombiner addAll(CompletionStage<?>... futures) {
        for (CompletionStage<?> future : futures) {
            this.add(future);
        }
        return this;
    }

    public JDKFutureCombiner addAll(Collection<? extends CompletionStage<?>> futures) {
        for (CompletionStage<?> future : futures) {
            this.add(future);
        }
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
    public JDKFutureCombiner setAggregatePromise(CompletableFuture<Object> aggregatePromise) {
        this.aggregatePromise = aggregatePromise;
        return this;
    }

    /**
     * 重置状态，使得可以重新添加future和选择
     */
    public void clear() {
        childrenListener = new ChildListener();
        aggregatePromise = null;
        futureCount = 0;
    }

    // endregion

    // region 选择

    /**
     * 返回的promise在任意future进入完成状态时进入完成状态
     * 返回的promise与首个完成future的结果相同（不准确）
     * 注意：如果future数量为0，返回的promise将无法进入完成状态。
     */
    public CompletableFuture<Object> anyOf() {
        return finish(AggregateOptions.anyOf());
    }

    /**
     * 成功N个触发成功
     * 如果触发失败，只随机记录一个Future的异常信息，而不记录所有的异常信息
     * <p>
     * 1.如果require等于【0】，则必定会成功。
     * 2.如果require大于监听的future数量，必定会失败。
     * 3.如果require小于监听的future数量，当成功任务数达到期望时触发成功。
     * <p>
     * 如果lazy为false，则满足成功/失败条件时立即触发完成；
     * 如果lazy为true，则等待所有任务完成之后才触发成功或失败。
     *
     * @param successRequire 期望成成功的任务数
     * @param failFast       是否在不满足条件时立即失败
     */
    public CompletableFuture<Object> selectN(int successRequire, boolean failFast) {
        return finish(AggregateOptions.selectN(futureCount, successRequire, failFast));
    }

    /**
     * 要求所有的future都成功时才进入成功状态
     * 一旦有任务失败则立即失败
     */
    public CompletableFuture<Object> selectAll() {
        return finish(AggregateOptions.selectAll(true));
    }

    /**
     * 要求所有的future都成功时才进入成功状态；
     * 任意任务失败，最终结果都表现为失败
     *
     * @param failFast 是否在不满足条件时立即失败
     */
    public CompletableFuture<Object> selectAll(boolean failFast) {
        return finish(AggregateOptions.selectAll(failFast));
    }

    // endregion

    // region 内部实现

    private CompletableFuture<Object> finish(AggregateOptions options) {
        Objects.requireNonNull(options);
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Already finished");
        }
        this.childrenListener = null;

        CompletableFuture<Object> aggregatePromise = this.aggregatePromise;
        if (aggregatePromise == null) {
            aggregatePromise = new CompletableFuture<>();
        } else {
            this.aggregatePromise = null;
        }

        // 数据存储在ChildListener上有助于扩展
        childrenListener.futureCount = this.futureCount;
        childrenListener.options = options;
        childrenListener.aggregatePromise = aggregatePromise;
        childrenListener.checkComplete();
        return aggregatePromise;
    }

    private static class ChildListener implements BiConsumer<Object, Throwable> {

        private final AtomicInteger succeedCount = new AtomicInteger();
        private final AtomicInteger doneCount = new AtomicInteger();

        /** 非volatile，虽然存在竞争，但重复赋值是安全的，通过promise发布到其它线程 */
        private Object result;
        private Throwable cause;

        /** 非volatile，其可见性由{@link #aggregatePromise}保证 */
        private int futureCount;
        private AggregateOptions options;
        private volatile CompletableFuture<Object> aggregatePromise;

        @Override
        public void accept(Object r, Throwable throwable) {
            // 我们先增加succeedCount，再增加doneCount，读取时先读取doneCount，再读取succeedCount，
            // 就可以保证succeedCount是比doneCount更新的值，才可以提前判断是否立即失败
            if (throwable == null) {
                result = encodeValue(r);
                succeedCount.incrementAndGet();
            } else {
                cause = throwable;
            }
            doneCount.incrementAndGet();

            CompletableFuture<Object> aggregatePromise = this.aggregatePromise;
            if (aggregatePromise != null && !aggregatePromise.isDone() && checkComplete()) {
                result = null;
                cause = null;
            }
        }

        boolean checkComplete() {
            // 字段的读取顺序不可以调整
            final int doneCount = this.doneCount.get();
            final int succeedCount = this.succeedCount.get();
            if (doneCount < succeedCount) { // 退出竞争，另一个线程来完成
                return false;
            }
            if (options.isAnyOf()) {
                if (futureCount == 0) { // anyOf不能完成，考虑打印log
                    return false;
                }
                if (doneCount == 0) {
                    return false;
                }
                if (result != null) { // anyOf下尽量返回成功
                    return aggregatePromise.complete(decodeValue(result));
                } else {
                    return aggregatePromise.completeExceptionally(cause);
                }
            }

            // 懒模式需要等待所有任务完成
            if (!options.failFast && doneCount < futureCount) {
                return false;
            }
            // 包含了require小于等于0的情况
            final int successRequire = options.isSelectAll() ? futureCount : options.successRequire;
            if (succeedCount >= successRequire) {
                return aggregatePromise.complete(null);
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                if (cause == null) {
                    cause = TaskInsufficientException.create(futureCount, doneCount, succeedCount, successRequire);
                }
                return aggregatePromise.completeExceptionally(cause);
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