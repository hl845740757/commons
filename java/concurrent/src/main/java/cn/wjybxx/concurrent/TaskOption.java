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

/**
 * 任务调度选项
 * 注意：用户只可使用低24位的option，高8位预留给系统，系统可能去除用户的高位bit信息。
 *
 * @author wjybxx
 * date 2023/4/14
 */
public final class TaskOption {

    /**
     * 低6位用于存储任务的调度阶段，取值[0, 63]，使用低6位可以避免位移。
     * 1.用于指定异步任务的调度时机。
     * 2.主要用于{@link EventLoop}这类单线程的Executor -- 尤其是游戏这类分阶段的事件循环。
     */
    public static final int MASK_SCHEDULE_PHASE = (1 << 6) - 1;

    /**
     * 是否是【低优先级】的【延时任务】
     * 1. 定时任务默认是高优先级，普通提交的任务一样；
     * 2. 低优先级任务是指：无需保证和非延时任务之间的执行时序，且在退出前可以不执行的任务；
     * 3. 也就是说：让EventLoop优先级处理一般业务，空闲时再处理这些定时任务。
     * 以代码说明：
     * <pre>{@code
     *      // 该任务可能在后续任务之后执行
     *      executor.schedule(task, 0, timeunit, LOW_PRIORITY);
     *      executor.submit(task);
     * }</pre>
     *
     * @apiNote EventLoop可以不支持该特性，低优先任务延迟是可选优化项
     */
    public static final int LOW_PRIORITY = 1 << 6;

    /**
     * 是否是【中优先级】的【延时任务】
     * 中优先级任务是指：需要保证【首次执行】和非延时任务之间的时序，进入循环阶段后可变为低优先级的任务。
     * 以代码说明：
     * <pre>{@code
     *      // 该任务将在下一个submit之前执行，但进入循环阶段后，优先级低于非延时任务
     *      executor.scheduleWithFixedDelay(task, 0, 1000, timeunit, MIDDLE_PRIORITY);
     *      executor.submit(task);
     * }</pre>
     *
     * @apiNote EventLoop可以不支持该特性，低优先任务延迟是可选优化项
     */
    public static final int MIDDLE_PRIORITY = 1 << 7;

    /**
     * 在执行该任务前必须先处理一次定时任务队列
     * EventLoop收到具有该特征的任务时，需要更新时间戳，尝试执行该任务之前的所有定时任务。
     */
    public static final int SCHEDULE_BARRIER = 1 << 8;

    /**
     * 本地序（可以与其它线程无序）
     * 对于EventLoop内部的任务，启用该特征值可跳过全局队列，这在EventLoop是有界的情况下可以避免死锁或阻塞。
     */
    public static final int LOCAL_ORDER = 1 << 9;

    /**
     * 唤醒事件循环线程
     * 事件循环线程可能阻塞某些操作上，如果一个任务需要EventLoop及时处理，则可以启用该选项唤醒线程。
     */
    public static final int WAKEUP_THREAD = 1 << 10;

    /**
     * 延时任务：在出现异常后继续执行。
     * 注意：只适用无需结果的周期性任务 -- 分时任务会失败。
     */
    public static final int CAUGHT_EXCEPTION = 1 << 11;
    /**
     * 延时任务：在执行任务前检测超时
     * 1. 也就是说在已经超时的情况下不执行任务。
     * 2. 在执行后一定会检测一次超时。
     */
    public static final int TIMEOUT_BEFORE_RUN = 1 << 12;
    /**
     * 延时任务：忽略来自future的取消
     * 1. 由于要和jdk保持兼容，默认是需要监听来自Future的取消信号的。
     * 2. 同时我们需要监听来自{@link ICancelToken}的取消，两端都监听有冗余开销。
     * 3. 用户可以启用该选项以避免监听来自Future的取消。
     * <p>
     * ps:监听取消信号的目的在于及时从队列中删除任务。
     */
    public static final int IGNORE_FUTURE_CANCEL = 1 << 13;

    /**
     * 该选项表示异步任务需要继承上游任务的取消令牌。
     * 注意： 在显式指定了上下文的情况下无效。
     */
    public static final int STAGE_INHERIT_TOKEN = 15;
    /**
     * 如果一个异步任务当前已在目标{@link SingleThreadExecutor}线程，则立即执行，而不提交任务。
     * 仅用于{@link ICompletionStage}
     */
    public static final int STAGE_TRY_INLINE = 1 << 16;
    /**
     * 默认情况下，如果一个异步任务的Executor是{@link IExecutor}类型，options将传递给Executor。
     * 如果期望禁用传递，可设置改选项。
     * 仅用于{@link ICompletionStage}
     */
    public static final int STAGE_NON_TRANSITIVE = 1 << 17;
    /**
     * 当回调接收的是Object类型的ctx，而不是{@link IContext}类型的ctx时，
     * 也尝试检测obj实例是否为{@link IContext}类型，并检测取消信号。
     */
    public static final int STAGE_CHECK_OBJECT_CTX = 1 << 18;

    // region util

    /** 用户可用的选项的掩码 */
    public static final int MASK_USER_OPTIONS = 0x00FF_FFFF;
    /** 保留选项的掩码 */
    public static final int MASK_RESERVED_OPTIONS = 0xFF00_0000;

    /** 是否启用了所有选项 */
    public static boolean isEnabled(int flags, int option) {
        return (flags & option) == option;
    }

    /**
     * 是否未启用选项。
     * 1.禁用任意bit即为未启用；
     * 2.和{@link #isEnabled(int, int)}相反关系
     */
    public static boolean isDisabled(int flags, int option) {
        return (flags & option) != option;
    }

    /** 启用特定调度选项 */
    public static int enable(int flags, int option) {
        return flags | option;
    }

    /** 禁用特定调度选项 */
    public static int disable(int flags, int option) {
        return (flags & ~option);
    }

    /** 启用或关闭特定选项 */
    public static int setEnable(int flags, int option, boolean enable) {
        if (enable) {
            return (flags | option);
        } else {
            return (flags & ~option);
        }
    }

    /** 获取任务的调度阶段 */
    public static int schedulePhase(int options) {
        return options & MASK_SCHEDULE_PHASE;
    }

    /** 设置任务的调度阶段 */
    public static int setSchedulePhase(int options, int phase) {
        assert (phase >= 0 && phase <= MASK_SCHEDULE_PHASE);
        options &= ~MASK_SCHEDULE_PHASE;
        options |= phase;
        return options;
    }

    // endregion
}