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

import java.util.concurrent.CancellationException;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;
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
 *      var handle = cancelToken.register(token -> {
 *          thread.interrupt();
 *      })
 *      // 如果handle已被通知，那么线程已处于中断状态，阻塞操作会立即被中断
 *      try (handle) {
 *          blockingOp();
 *      }
 *   }
 * }</pre>
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

    // region

    /**
     * 取消码
     * 1. 按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
     * 2. 低20位为取消原因；高12位为特殊信息 {@link #MASK_REASON}
     * 3. 不为0表示已发起取消请求
     * 4. 取消时至少赋值一个信息，reason通常应该赋值
     */
    int cancelCode();

    /**
     * 是否已发出取消指令
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
        return reason(cancelCode());
    }

    /** 取消的紧急程度 */
    default int degree() {
        return degree(cancelCode());
    }

    /** 取消指令中是否要求了中断线程 */
    default boolean isInterruptible() {
        return isInterruptible(cancelCode());
    }

    /** 取消指令中是否要求了无需删除 */
    default boolean isWithoutRemove() {
        return isWithoutRemove(cancelCode());
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

    /**
     * 添加的action将在Context收到取消信号时执行
     * 1.如果已收到取消请求，则给定的action会立即执行。
     * 2.如果尚未收到取消请求，则给定action会在收到请求时执行。
     */
    IRegistration register(Consumer<? super ICancelToken> action);

    /**
     * 添加一个完成时固定执行的行为 -- 忽略取消信息。
     */
    IRegistration registerRun(Runnable action);

    /**
     * 添加一个特定类型的监听器
     * (用于特殊需求时避免额外的闭包)
     */
    IRegistration registerTyped(CancelTokenListener action);

    /**
     * 添加子token
     * 1.子token会在当前token进入取消状态时被取消
     * 2.该接口本质是一个快捷方法，但允许子类优化
     */
    IRegistration registerChild(ICancelTokenSource child);

    // endregion

    // region code

    /**
     * 原因的掩码
     * 1.如果cancelCode不包含其它信息，就等于reason
     * 2.设定为20位，可达到100W
     */
    int MASK_REASON = 0xFFFFF;
    /** 紧迫程度的掩码（4it）-- 0表示未指定 */
    int MASK_DEGREE = 0x00F0_0000;
    /** 预留4bit */
    int MASK_REVERSED = 0x0F00_0000;
    /** 中断的掩码 （1bit） */
    int MASK_INTERRUPT = 1 << 28;
    /** 告知任务无需执行删除逻辑 -- 慎用 */
    int MASK_WITHOUT_REMOVE = 1 << 29;

    /** 最大取消原因 */
    int MAX_REASON = MASK_REASON;
    /** 最大紧急程度 */
    int MAX_DEGREE = 15;

    /** 取消原因的偏移量 */
    int OFFSET_REASON = 0;
    /** 紧急度的偏移量 */
    int OFFSET_DEGREE = 20;

    /** 默认原因 */
    int REASON_DEFAULT = 1;
    /** 执行超时 -- {@link ICancelTokenSource#cancelAfter(int, long, TimeUnit)}就可使用 */
    int REASON_TIMEOUT = 2;
    /** Executor关闭 -- Executor关闭不一定会取消任务 */
    int REASON_SHUTDOWN = 3;

    /** 计算取消码中的原因 */
    static int reason(int code) {
        return code & MASK_REASON;
    }

    /** 计算取消码终归的紧急程度 */
    static int degree(int code) {
        return (code & MASK_DEGREE) >>> OFFSET_DEGREE;
    }

    /** 取消指令中是否要求了中断线程 */
    static boolean isInterruptible(int code) {
        return (code & MASK_INTERRUPT) != 0;
    }

    /** 取消指令中是否要求了无需删除 */
    static boolean isWithoutRemove(int code) {
        return (code & MASK_WITHOUT_REMOVE) != 0;
    }

    /**
     * 检查取消码的合法性
     *
     * @return argument
     */
    static int checkCode(int code) {
        if (reason(code) == 0) {
            throw new IllegalArgumentException("reason is absent");
        }
        return code;
    }

    // endregion

}