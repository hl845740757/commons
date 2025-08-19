/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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
     * 调度器使用的控制标识位(低8位)
     * 调度器会擦除掉用户的低8位，存储调度器的信息
     */
    public static final int MASK_CTL_RESERVED = 0xFF;

    /** 优先级的存储偏移量 */
    public static final int OFFSET_PRIORITY = 8;
    /** 优先级的最大值 */
    public static final int MAX_PRIORITY = 31;

    /**
     * 延时任务的优先级，取值[0, 31]
     * 1. 当任务的触发时间相同时，按照优先级排序，值越低优先级越高。
     * 2. 由于0需要表示未设置优先级，因此Executor会对值进行偏移，通常而言是减1。
     * 3. 优先级值的约定取决于各自的实现。
     */
    @Beta
    public static final int MASK_PRIORITY = MAX_PRIORITY << OFFSET_PRIORITY;

    /** 延时任务：包含优先级 */
    public static final int HAS_PRIORITY = 14; // 预留1位给优先级
    /** 延时任务：包含调度阶段 */
    public static final int HAS_SCHEDULE_PHASE = 15;
    /** 延时任务：包含次数限制 */
    public static final int HAS_COUNTDOWN = 1 << 16;
    /** 延时任务：包含超时时间（执行时间限制） */
    public static final int HAS_TIMEOUT = 1 << 17;
    /**
     * 延时任务：在出现异常后继续执行。
     * 1.只适用无需结果的周期性任务 -- 分时任务会失败。
     * 2.如果需要取消任务，需通过取消令牌实现。
     */
    public static final int CAUGHT_EXCEPTION = 1 << 18;

    /**
     * 事件循环在执行该任务前必须先处理一次定时任务队列。
     * 1. EventLoop收到具有该特征的任务时，需要更新时间戳，尝试执行该任务之前的所有定时任务。
     * 2. 该选项不一定能保证时序，因为存在时序依赖的任务可能同时提交成功。
     */
    public static final int SCHEDULE_BARRIER = 1 << 19;
    /**
     * 唤醒事件循环线程
     * 事件循环线程可能阻塞某些操作上，如果一个任务需要EventLoop及时处理，则可以启用该选项唤醒线程。
     */
    public static final int WAKEUP_THREAD = 1 << 20;

    /**
     * 如果一个异步任务当前已在目标{@link SingleThreadExecutor}线程，则立即执行，而不提交任务。
     */
    public static final int STAGE_TRY_INLINE = 1 << 21;
    /**
     * 默认情况下，Stage会在触发回调之前检测ctx否为{@link IContext}或{@link ICancelToken}类型，并检测取消信号。
     * 用户如果不期望Stage进行检查，可启用该选项关闭自动检测。
     */
    public static final int STAGE_UNCANCELLABLE_CTX = 1 << 22;
    /**
     * 监听用户上下文中包含的取消令牌
     * <p>
     * 1.该选项用于延时任务或监听器列表管理。
     * 2.如果调度器默认不会监听CTX中的取消令牌，那么应当响应用户的该选项。
     */
    public static final int STAGE_LISTEN_CANCEL_TOKEN = 1 << 23;

    /**
     * C#：抑制await抛出取消异常(性能因素)
     */
    private static final int SUPPRESS_CANCELLATION_THROW = 1 << 26;
    /**
     * C#：抑制await抛出失败异常(性能因素)
     */
    private static final int SUPPRESS_ERROR_THROW = 1 << 27;

    // region util

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
        return (options & MASK_PRIORITY) >> OFFSET_PRIORITY;
    }

    /** 设置优先级 */
    public static int setPriority(int options, int priority) {
        if (priority < 0 || priority > MAX_PRIORITY) {
            throw new IllegalArgumentException("priority: " + priority);
        }
        options &= ~MASK_PRIORITY;
        options |= (priority << OFFSET_PRIORITY);
        return options;
    }

    // endregion
}