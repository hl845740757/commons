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

/**
 * 任务调度选项
 *
 * @author wjybxx
 * date 2023/4/14
 */
public final class TaskOptions {

    /**
     * 延时任务的优先级，取值[0, 15]
     * 1. 当任务的触发时间相同时，按照优先级排序，值越低优先级越高。
     * 2. 由于0需要表示未设置优先级，因此Executor会对值进行偏移，通常而言是减1。
     * 3. 优先级值的约定取决于各自的实现。
     */
    @Beta
    public static final int MASK_PRIORITY = 0x0F;
    /**
     * 低位用于存储任务的调度阶段，取值[0, 15]，使用低位可以避免位移。
     * 1. 用于指定异步任务的调度时机。
     * 2. 主要用于{@link EventLoop}这类单线程的Executor -- 尤其是游戏这类分阶段的事件循环。
     */
    public static final int MASK_SCHEDULE_PHASE = 0xF0;

    /**
     * 事件循环在执行该任务前必须先处理一次定时任务队列。
     * 1. EventLoop收到具有该特征的任务时，需要更新时间戳，尝试执行该任务之前的所有定时任务。
     * 2. 该选项不一定能保证时序，因为存在时序依赖的任务可能同时提交成功。
     */
    public static final int SCHEDULE_BARRIER = 1 << 12;

    /**
     * 本地序（可以与其它线程无序）
     * 对于EventLoop内部的任务，启用该特征值可跳过全局队列，这在EventLoop是有界的情况下可以避免死锁或阻塞。
     */
    public static final int LOCAL_ORDER = 1 << 13;

    /**
     * 唤醒事件循环线程
     * 事件循环线程可能阻塞某些操作上，如果一个任务需要EventLoop及时处理，则可以启用该选项唤醒线程。
     */
    public static final int WAKEUP_THREAD = 1 << 14;

    /**
     * 延时任务：在出现异常后继续执行。
     * 1.只适用无需结果的周期性任务 -- 分时任务会失败。
     * 2.如果需要取消任务，需通过取消令牌实现。
     */
    public static final int CAUGHT_EXCEPTION = 1 << 15;
    /**
     * 延时任务：在执行任务前检测超时
     * 1. 也就是说在已经超时的情况下不执行任务。
     * 2. 在执行后一定会检测一次超时。
     */
    public static final int TIMEOUT_BEFORE_RUN = 1 << 16;
    /**
     * 延时任务：忽略来自future的取消
     * 1. 由于要和jdk保持兼容，默认是需要监听来自Future的取消信号的。
     * 2. 同时我们需要监听来自{@link ICancelToken}的取消，两端都监听有冗余开销。
     * 3. 用户可以启用该选项以避免监听来自Future的取消。
     * <p>
     * ps:监听取消信号的目的在于及时从队列中删除任务。
     */
    public static final int IGNORE_FUTURE_CANCEL = 1 << 17;

    /**
     * 如果一个异步任务当前已在目标{@link SingleThreadExecutor}线程，则立即执行，而不提交任务。
     * 仅用于{@link ICompletionStage}
     */
    public static final int STAGE_TRY_INLINE = 1 << 19;
    /**
     * 默认情况下，stage的options仅用于自身，
     * 如果一个异步任务的Executor是{@link IExecutor}类型，且期望将options传递给Executor时，可以启用该选项。
     *
     * @deprecated 不冲突，总是传递；另外，不传递我们还需要存储两份options。
     */
    @Deprecated
    public static final int STAGE_PROPAGATE_OPTIONS = 1 << 20;
    /**
     * 默认情况下，Stage会在触发回调之前检测ctx否为{@link IContext}或{@link ICancelToken}类型，并检测取消信号。
     * 用户如果不期望Stage进行检查，可启用该选项关闭自动检测。
     */
    public static final int STAGE_UNCANCELLABLE_CTX = 1 << 21;

    /**
     * 分帧任务：慢启动。
     * 默认情况下，分帧任务的start方法和update方法是同步执行的；
     * 如果期望首次运行时仅执行start方法，可通过该选项启用。
     */
    public static final int TIMESHARING_SLOW_START = 1 << 22;

    // region util
    /** 优先级的存储偏移量 */
    public static final int OFFSET_PRIORITY = 0;
    /** 优先级的存储偏移量 */
    public static final int OFFSET_SCHEDULE_PHASE = 4;

    /** 优先级的最大值 */
    public static final int MAX_PRIORITY = MASK_PRIORITY;
    /** 调度阶段的最大值 */
    public static final int MAX_SCHEDULE_PHASE = MASK_SCHEDULE_PHASE >> OFFSET_SCHEDULE_PHASE;
    /** 调度阶段和优先级的掩码 */
    public static final int MASK_PRIORITY_AND_SCHEDULE_PHASE = MASK_PRIORITY | MASK_SCHEDULE_PHASE;

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

    /** 获取任务的优先级 */
    public static int getPriority(int options) {
        return options & MASK_PRIORITY;
    }

    /** 设置优先级 */
    public static int setPriority(int options, int priority) {
        if (priority < 0 || priority > MAX_PRIORITY) {
            throw new IllegalArgumentException("priority: " + priority);
        }
        options &= ~MASK_PRIORITY;
        options |= priority;
        return options;
    }


    /** 获取任务的调度阶段 */
    public static int getSchedulePhase(int options) {
        return (options & MASK_SCHEDULE_PHASE) >> OFFSET_SCHEDULE_PHASE;
    }

    /** 设置任务的调度阶段 */
    public static int setSchedulePhase(int options, int phase) {
        if (phase < 0 || phase > MASK_SCHEDULE_PHASE) {
            throw new IllegalArgumentException("phase: " + phase);
        }
        options &= ~MASK_SCHEDULE_PHASE;
        options |= (phase) << OFFSET_SCHEDULE_PHASE;
        return options;
    }


    // endregion
}