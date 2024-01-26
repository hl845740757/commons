﻿#region LICENSE

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

namespace Wjybxx.Commons;

/// <summary>
/// 
/// </summary>
public interface ICancelToken
{
    /**
     * 返回一个只读的{@link ICancelToken}试图，返回的实例会在当前Token被取消时取消。
     * 其作用类似{@link IFuture#asReadonly()}
     */
    ICancelToken asReadonly();

    // region code

    //
    /// <summary>
    /// 取消码
    /// 1. 按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
    /// 2. 低20位为取消原因；高12位为特殊信息 <see cref="ICancelToken.MASK_REASON"/>
    /// 3. 不为0表示已发起取消请求
    /// 4. 取消时至少赋值一个信息，reason通常应该赋值
    /// </summary>
    /// <returns></returns>
    int cancelCode();

    /// <summary>
    /// 是否已收到取消信号
    /// 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
    /// </summary>
    /// <returns></returns>
    bool isCancelling() {
        return cancelCode() != 0;
    }

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)T
     */
    int reason() {
        return reason(cancelCode());
    }

    /** 取消的紧急程度 */
    int degree() {
        return degree(cancelCode());
    }

    /** 取消指令中是否要求了中断线程 */
    bool isInterruptible() {
        return isInterruptible(cancelCode());
    }

    /** 取消指令中是否要求了无需删除 */
    bool isWithoutRemove() {
        return isWithoutRemove(cancelCode());
    }

    /**
     * 检测取消信号
     * 如果收到取消信号，则抛出{@link CancellationException}
     */
    void checkCancel() {
        int code = cancelCode();
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }
    // endregion

    // region 监听器

    // region accept

    /**
     * 添加的action将在Context收到取消信号时执行
     * 1.如果已收到取消请求，则给定的action会立即执行。
     * 2.如果尚未收到取消请求，则给定action会在收到请求时执行。
     */
    IRegistration thenAccept(Action<ICancelToken> action, int options = 0);

    IRegistration thenAcceptAsync(IExecutor executor,
                                  Action<ICancelToken> action, int options = 0);

    // endregion

    // region accept-ctx

    IRegistration thenAccept(Action<IContext, ICancelToken> action, IContext ctx, int options = 0);

    IRegistration thenAcceptAsync(IExecutor executor,
                                  Action<IContext, ICancelToken> action, IContext ctx, int options = 0);

    // endregion

    // region run

    IRegistration thenRun(Action action, int options = 0);

    IRegistration thenRunAsync(IExecutor executor, Action action, int options = 0);

    // endregion

    // region run-ctx

    IRegistration thenRun(Action<IContext> action, IContext ctx, int options = 0);

    IRegistration thenRunAsync(IExecutor executor,
                               Action<IContext> action, IContext ctx, int options = 0);

    // endregion

    // region notify

    /**
     * 添加一个特定类型的监听器
     * (用于特殊需求时避免额外的闭包 - task经常需要监听取消令牌)
     */
    IRegistration thenNotify(CancelTokenListener action, int options = 0);

    IRegistration thenNotifyAsync(IExecutor executor, CancelTokenListener action, int options = 0);

    // endregion

    // region transferTo

    /**
     * 该接口用于方便构建子上下文
     * 1.子token会在当前token进入取消状态时被取消
     * 2.该接口本质是一个快捷方法，但允许子类优化
     *
     * @param child   接收结果的子token
     * @param options 调度选项
     */
    IRegistration thenTransferTo(ICancelTokenSource child, int options = 0);

    IRegistration thenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0);

    // endregion

    // endregion

    // region static

    /**
     * 原因的掩码
     * 1.如果cancelCode不包含其它信息，就等于reason
     * 2.设定为20位，可达到100W
     */
    const int MASK_REASON = 0xFFFFF;
    /** 紧迫程度的掩码（4it）-- 0表示未指定 */
    const int MASK_DEGREE = 0x00F0_0000;
    /** 预留4bit */
    const int MASK_REVERSED = 0x0F00_0000;
    /** 中断的掩码 （1bit） */
    const int MASK_INTERRUPT = 1 << 28;
    /** 告知任务无需执行删除逻辑 -- 慎用 */
    const int MASK_WITHOUT_REMOVE = 1 << 29;

    /** 最大取消原因 */
    const int MAX_REASON = MASK_REASON;
    /** 最大紧急程度 */
    const int MAX_DEGREE = 15;

    /** 取消原因的偏移量 */
    const int OFFSET_REASON = 0;
    /** 紧急度的偏移量 */
    const int OFFSET_DEGREE = 20;

    /** 默认原因 */
    const int REASON_DEFAULT = 1;
    /** 执行超时 -- {@link ICancelTokenSource#cancelAfter(int, long, TimeUnit)}就可使用 */
    const int REASON_TIMEOUT = 2;
    /** IExecutor关闭 -- IExecutor关闭不一定会取消任务 */
    const int REASON_SHUTDOWN = 3;

    /** 计算取消码中的原因 */
    static int reason(int code) {
        return code & MASK_REASON;
    }

    /** 计算取消码终归的紧急程度 */
    static int degree(int code) {
        return (code & MASK_DEGREE) >>> OFFSET_DEGREE;
    }

    /** 取消指令中是否要求了中断线程 */
    static bool isInterruptible(int code) {
        return (code & MASK_INTERRUPT) != 0;
    }

    /** 取消指令中是否要求了无需删除 */
    static bool isWithoutRemove(int code) {
        return (code & MASK_WITHOUT_REMOVE) != 0;
    }

    /**
     * 检查取消码的合法性
     *
     * @return argument
     */
    static int checkCode(int code) {
        if (reason(code) == 0) {
            throw new ArgumentException("reason is absent");
        }
        return code;
    }

    // endregion
}