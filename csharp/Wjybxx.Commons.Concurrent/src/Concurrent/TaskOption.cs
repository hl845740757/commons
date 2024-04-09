#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Diagnostics;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 任务的调度选项
/// </summary>
public static class TaskOption
{
    /// <summary>
    /// 低6位用于存储任务的调度阶段，取值[0, 31]，使用低位可以避免位移。
    /// 1.用于指定异步任务的调度时机。
    /// 2.主要用于{@link EventLoop}这类单线程的Executor -- 尤其是游戏这类分阶段的事件循环。
    ///</summary>
    public const int MASK_SCHEDULE_PHASE = 31;

    /// <summary>
    /// 延时任务的优先级，取值[0, 31]。
    /// 1.当任务的触发时间相同时，按照优先级排序，值越低优先级越高。
    /// </summary>
    [Beta]
    public const int MASK_PRIORITY = 31 << 5;

    /// <summary>
    /// 是否是【低优先级】的【延时任务】
    /// 1. 定时任务默认是高优先级，普通提交的任务一样；
    /// 2. 低优先级任务是指：无需保证和非延时任务之间的执行时序，且在退出前可以不执行的任务；
    /// 3. 也就是说：让EventLoop优先级处理一般业务，空闲时再处理这些定时任务。
    /// 以代码说明：
    /// <code>
    ///      // 该任务可能在后续任务之后执行
    ///      executor.schedule(task, 0, timeunit, LOW_PRIORITY);
    ///      executor.submit(task);
    /// </code>
    /// 注意：EventLoop可以不支持该特性，低优先任务延迟是可选优化项
    ///</summary>
    public const int LOW_PRIORITY = 1 << 10;
    /// <summary>
    /// 是否是【中优先级】的【延时任务】
    /// 中优先级任务是指：需要保证【首次执行】和非延时任务之间的时序，进入循环阶段后可变为低优先级的任务。
    /// 以代码说明：
    /// <code>
    ///      // 该任务将在下一个submit之前执行，但进入循环阶段后，优先级低于非延时任务
    ///      executor.scheduleWithFixedDelay(task, 0, 1000, timeunit, MIDDLE_PRIORITY);
    ///      executor.submit(task);
    /// </code>
    /// 注意：EventLoop可以不支持该特性，低优先任务延迟是可选优化项
    ///</summary>
    public const int MIDDLE_PRIORITY = 1 << 11;

    /// <summary>
    /// 在执行该任务前必须先处理一次定时任务队列
    /// EventLoop收到具有该特征的任务时，需要更新时间戳，尝试执行该任务之前的所有定时任务。
    ///</summary>
    public const int SCHEDULE_BARRIER = 1 << 12;

    /// <summary>
    /// 本地序（可以与其它线程无序）
    /// 对于EventLoop内部的任务，启用该特征值可跳过全局队列，这在EventLoop是有界的情况下可以避免死锁或阻塞。
    ///</summary>
    public const int LOCAL_ORDER = 1 << 13;

    /// <summary>
    /// 唤醒事件循环线程
    /// 事件循环线程可能阻塞某些操作上，如果一个任务需要EventLoop及时处理，则可以启用该选项唤醒线程。
    ///</summary>
    public const int WAKEUP_THREAD = 1 << 14;

    /// <summary>
    /// 延时任务：在出现异常后继续执行。
    /// 注意：只适用无需结果的周期性任务 -- 分时任务会失败。
    ///</summary>
    public const int CAUGHT_EXCEPTION = 1 << 15;
    /// <summary>
    /// 延时任务：在执行任务前检测超时
    /// 1. 也就是说在已经超时的情况下不执行任务。
    /// 2. 在执行后一定会检测一次超时。
    ///</summary>
    public const int TIMEOUT_BEFORE_RUN = 1 << 16;
    /// <summary>
    /// 延时任务：忽略来自future的取消
    /// 1. 由于要和jdk保持兼容，默认是需要监听来自Future的取消信号的。
    /// 2. 同时我们需要监听来自{@link ICancelToken}的取消，两端都监听有冗余开销。
    /// 3. 用户可以启用该选项以避免监听来自Future的取消。
    /// 
    /// ps:监听取消信号的目的在于及时从队列中删除任务。
    ///</summary>
    public const int IGNORE_FUTURE_CANCEL = 1 << 17;

    /**
     * 该选项表示异步任务需要继承上游任务的取消令牌。
     * 注意： 在显式指定了上下文的情况下无效。
     */
    public const int STAGE_INHERIT_CANCEL_TOKEN = 18;
    /// <summary>
    /// 如果一个异步任务当前已在目标{@link SingleThreadExecutor}线程，则立即执行，而不提交任务。
    /// 仅用于{@link ICompletionStage}
    ///</summary>
    public const int STAGE_TRY_INLINE = 1 << 19;
    /// <summary>
    /// 默认情况下，如果一个异步任务的Executor是{@link IExecutor}类型，options将传递给Executor。
    /// 如果期望禁用传递，可设置改选项。
    /// 仅用于{@link ICompletionStage}
    ///</summary>
    public const int STAGE_NON_TRANSITIVE = 1 << 20;
    /// <summary>
    /// 当回调接收的是Object类型的ctx，而不是{@link IContext}类型的ctx时，
    /// 也尝试检测obj实例是否为{@link IContext}类型，并检测取消信号。
    /// </summary>
    public const int STAGE_CHECK_OBJECT_CTX = 1 << 21;

    // region util

    /// <summary> 用户可用的选项的掩码 ///</summary>
    public const int MASK_USER_OPTIONS = 0x00FF_FFFF;
    /// <summary> 保留选项的掩码 ///</summary>
    public const int MASK_RESERVED_OPTIONS = unchecked((int)0xFF00_0000);

    /// <summary> 是否启用了所有选项 ///</summary>
    public static bool IsEnabled(int flags, int option) {
        return (flags & option) == option;
    }

    /// <summary>
    /// 是否未启用选项。
    /// 1.禁用任意bit即为未启用；
    /// 2.和{@link #isEnabled(int, int)}相反关系
    ///</summary>
    public static bool IsDisabled(int flags, int option) {
        return (flags & option) != option;
    }

    /// <summary>
    /// 启用特定调度选项
    /// </summary>
    /// <param name="flags">当前flags</param>
    /// <param name="option">要启用的选项</param>
    /// <returns>新的flags</returns>
    public static int Enable(int flags, int option) {
        return flags | option;
    }

    /// <summary>
    /// 禁用特定调度选项
    /// </summary>
    /// <param name="flags">当前flags</param>
    /// <param name="option">要禁用的选项</param>
    /// <returns>新的flags</returns>
    public static int Disable(int flags, int option) {
        return (flags & ~option);
    }

    /// <summary>
    /// 启用或关闭特定选项
    /// </summary>
    /// <param name="flags">当前flags</param>
    /// <param name="option">要启用或禁用的选项</param>
    /// <param name="enable">开启或禁用</param>
    /// <returns></returns>
    public static int SetEnable(int flags, int option, bool enable) {
        if (enable) {
            return (flags | option);
        } else {
            return (flags & ~option);
        }
    }

    /// <summary>
    /// 获取任务的调度阶段
    /// </summary>
    public static int GetSchedulePhase(int options) {
        return options & MASK_SCHEDULE_PHASE;
    }

    /// <summary>
    /// 设置调度阶段
    /// </summary>
    /// <param name="options">当前options</param>
    /// <param name="phase">调度阶段</param>
    /// <returns>新的options</returns>
    public static int SetSchedulePhase(int options, int phase) {
        if (phase < 0 || phase > MASK_SCHEDULE_PHASE) {
            throw new ArgumentException("phase: " + phase);
        }
        options &= ~MASK_SCHEDULE_PHASE;
        options |= phase;
        return options;
    }

    // endregion
}