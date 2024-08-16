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

import cn.wjybxx.base.IRegistration;
import cn.wjybxx.base.concurrent.BetterCancellationException;
import cn.wjybxx.base.concurrent.CancelCodes;
import cn.wjybxx.base.concurrent.StacklessCancellationException;

import java.util.concurrent.CancellationException;
import java.util.concurrent.Executor;
import java.util.concurrent.Future;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * 取消令牌
 * <p>
 * 取消令牌由任务持有，任务在执行期间可主动检测取消，并在检测到取消后抛出{@link CancellationException}，
 * 如果不想打印堆栈，可抛出{@link StacklessCancellationException}。
 * <p>
 * 首先，我们要明白为什么需要支持取消，<b>当不再需要任务的结果时，及时取消任务的执行，以避免不必要的资源浪费。</b><br>
 * 在多年以前，多线程编程和异步编程尚不普遍，因此Future通常只与单个任务绑定，因此取消任务的最佳方式就是通过Future取消 —— 既清晰，又可以避免额外开销。
 * 但在异步编程如火如荼的今天，通过Future取消任务越来越力不从心。
 * <p>
 * <h3>Future.cancel 的缺陷</h3>
 * 1. {@link Future#cancel(boolean)}的接口约定是强制的，要求方法返回前Future必须进入完成状态，这是个错误的约定。
 * <b>取消是协作式的，并不能保证立即成功</b>  -- 取消一个任务和终止一个线程没有本质区别。<br>
 * 2. 通过Future取消只能取消Future关联的任务，而不能取消一组相关的任务。要取消一组相关的任务，必须让这些任务共享同一个上下文 -- 即取消上下文。
 * <p>
 * <h3>如何中断线程</h3>
 * 注意！取消令牌只是一个共享上下文，不具备任何其它功能。一个任务如果要响应中断信号，必须注册监听器，然后中断自身所在的线程。
 * <pre>{@code
 *   public void run() {
 *      // 在执行耗时操作前检查取消信号
 *      cancelToken.checkCancel();
 *      // 在执行阻塞操作前监听取消信号以唤醒线程
 *      Thread thread = Thread.currentThread();
 *      var handle = cancelToken.thenAccept(token -> {
 *          thread.interrupt();
 *      })
 *      // 如果handle已被通知，那么线程已处于中断状态，阻塞操作会立即被中断
 *      try (handle) {
 *          blockingOp();
 *      }
 *   }
 * }</pre>
 *
 * <h3>监听器</h3>
 * 1. accept系列方法表示接收token参数；run方法表示不接收token参数；
 * 2. async表示目标action需要异步执行，方法的首个参数为executor；
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public interface ICancelToken {

    /** 永不取消的Token */
    ICancelToken NONE = UncancellableToken.INST;

    /**
     * 返回一个只读的{@link ICancelToken}试图，返回的实例会在当前Token被取消时取消。
     * 其作用类似{@link IFuture#asReadonly()}
     */
    ICancelToken asReadonly();

    /**
     * 当前token是否可以进入取消状态
     *
     * @return 如果当前token可以进入取消状态则返回true
     */
    boolean canBeCancelled();

    // region code

    /**
     * 取消码
     * 1. 按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
     * 2. 低20位为取消原因；高12位为特殊信息 {@link CancelCodes#MASK_REASON}
     * 3. 不为0表示已发起取消请求
     * 4. 取消时至少赋值一个信息，reason通常应该赋值
     */
    int cancelCode();

    /**
     * 是否已收到取消信号
     * 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
     */
    default boolean isCancelling() {
        return cancelCode() != 0;
    }

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)T
     */
    default int reason() {
        return CancelCodes.getReason(cancelCode());
    }

    /** 取消的紧急程度 */
    default int degree() {
        return CancelCodes.getDegree(cancelCode());
    }

    /** 取消指令中是否要求了中断线程 */
    default boolean isInterruptible() {
        return CancelCodes.isInterruptible(cancelCode());
    }

    /** 取消指令中是否要求了无需删除 */
    default boolean isWithoutRemove() {
        return CancelCodes.isWithoutRemove(cancelCode());
    }

    /**
     * 检测取消信号
     * 如果收到取消信号，则抛出{@link CancellationException}
     */
    default void checkCancel() {
        int code = cancelCode();
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }
    // endregion

    // region 监听器

    // region accept

    /**
     * 添加的action将在Token收到取消信号时执行
     * 1.如果已收到取消请求，则给定的action会立即执行。
     * 2.如果尚未收到取消请求，则给定action会在收到请求时执行。
     */
    IRegistration thenAccept(Consumer<? super ICancelToken> action, int options);

    IRegistration thenAccept(Consumer<? super ICancelToken> action);

    IRegistration thenAcceptAsync(Executor executor,
                                  Consumer<? super ICancelToken> action);

    IRegistration thenAcceptAsync(Executor executor,
                                  Consumer<? super ICancelToken> action, int options);

    // endregion

    // region accept-ctx

    /**
     * 添加的action将在Token收到取消信号时执行
     * 1.如果已收到取消请求，则给定的action会立即执行。
     * 2.如果尚未收到取消请求，则给定action会在收到请求时执行。
     * 3.如果不期望检测ctx中潜在的取消信号，可通过{@link TaskOption#STAGE_UNCANCELLABLE_CTX}关闭。
     *
     * @param action  回调任务
     * @param ctx     上下文
     * @param options 调度选项
     * @return 取消句柄
     */
    IRegistration thenAccept(BiConsumer<? super ICancelToken, Object> action, Object ctx, int options);

    IRegistration thenAccept(BiConsumer<? super ICancelToken, Object> action, Object ctx);

    IRegistration thenAcceptAsync(Executor executor,
                                  BiConsumer<? super ICancelToken, Object> action, Object ctx);

    IRegistration thenAcceptAsync(Executor executor,
                                  BiConsumer<? super ICancelToken, Object> action, Object ctx, int options);

    // endregion

    // region run

    IRegistration thenRun(Runnable action, int options);

    IRegistration thenRun(Runnable action);

    IRegistration thenRunAsync(Executor executor, Runnable action);

    IRegistration thenRunAsync(Executor executor, Runnable action, int options);

    // endregion

    // region run-ctx

    IRegistration thenRun(Consumer<Object> action, Object ctx, int options);

    IRegistration thenRun(Consumer<Object> action, Object ctx);

    IRegistration thenRunAsync(Executor executor,
                               Consumer<Object> action, Object ctx);

    IRegistration thenRunAsync(Executor executor,
                               Consumer<Object> action, Object ctx, int options);

    // endregion

    // region notify

    /**
     * 添加一个特定类型的监听器
     * (用于特殊需求时避免额外的闭包 - task经常需要监听取消令牌)
     */
    IRegistration thenNotify(CancelTokenListener action, int options);

    IRegistration thenNotify(CancelTokenListener action);

    IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action);

    IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action, int options);

    // endregion

    // region transferTo

    /**
     * 该接口用于方便构建子上下文
     * 1.子token会在当前token进入取消状态时被取消
     * 2.该接口本质是一个快捷方法，但允许子类优化
     * <p>
     * 注意：在Future体系下，child是上游任务；而在行为树这类体系下，child是下游任务。
     *
     * @param child   接收结果的子token
     * @param options 调度选项
     */
    IRegistration thenTransferTo(ICancelTokenSource child, int options);

    IRegistration thenTransferTo(ICancelTokenSource child);

    IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child);

    IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child, int options);

    // endregion

    // endregion

}