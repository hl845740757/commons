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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 
/// </summary>
public interface ICancelToken
{
    /// <summary>
    /// 表示不可取消的令牌
    /// </summary>
    static readonly ICancelToken NONE = UncancellableToken.INST;

    /**
     * 返回一个只读的<see cref="ICancelToken"/>试图，返回的实例会在当前Token被取消时取消。
     * 其作用类似<see cref="IFuture.AsReadonly"/>。
     */
    ICancelToken AsReadonly();

    /// <summary>
    /// Token是否可以进入取消状态
    /// </summary>
    /// <returns></returns>
    bool CanBeCancelled { get; }

    #region code

    /// <summary>
    /// 取消码
    /// 1. 按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
    /// 2. 低20位为取消原因；高12位为特殊信息 <see cref="CancelCodes.MASK_REASON"/>
    /// 3. 不为0表示已发起取消请求
    /// 4. 取消时至少赋值一个信息，reason通常应该赋值
    /// </summary>
    /// <value></value>
    int CancelCode { get; }

    /// <summary>
    /// 是否已收到取消信号
    /// 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
    /// </summary>
    /// <value></value>
    bool IsCancelling => CancelCode != 0;

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)
     */
    int Reason => CancelCodes.GetReason(CancelCode);

    /** 取消的紧急程度 */
    int Degree => CancelCodes.GetDegree(CancelCode);

    /** 取消指令中是否要求了中断线程 */
    bool IsInterruptible => CancelCodes.IsInterruptible(CancelCode);

    /** 取消指令中是否要求了无需删除 */
    bool IsWithoutRemove => CancelCodes.IsWithoutRemove(CancelCode);

    /**
     * 检测取消信号
     * 如果收到取消信号，则抛出{@link CancellationException}
     */
    void CheckCancel() {
        int code = CancelCode;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #endregion

    #region 监听器

    // region accept

    /**
     * 添加的action将在Context收到取消信号时执行
     * 1.如果已收到取消请求，则给定的action会立即执行。
     * 2.如果尚未收到取消请求，则给定action会在收到请求时执行。
     */
    IRegistration ThenAccept(Action<ICancelToken> action, int options = 0);

    IRegistration ThenAcceptAsync(IExecutor executor,
                                  Action<ICancelToken> action, int options = 0);

    // endregion

    // region accept-ctx

    IRegistration ThenAccept(Action<ICancelToken, object> action, object? state, int options = 0);

    IRegistration ThenAcceptAsync(IExecutor executor,
                                  Action<ICancelToken, object> action, object? state, int options = 0);

    // endregion

    // region run

    IRegistration ThenRun(Action action, int options = 0);

    IRegistration ThenRunAsync(IExecutor executor, Action action, int options = 0);

    // endregion

    // region run-ctx

    IRegistration ThenRun(Action<object> action, object? state, int options = 0);

    IRegistration ThenRunAsync(IExecutor executor,
                               Action<object> action, object? state, int options = 0);

    // endregion

    // region notify

    /**
     * 添加一个特定类型的监听器
     * (用于特殊需求时避免额外的闭包 - task经常需要监听取消令牌)
     */
    IRegistration ThenNotify(ICancelTokenListener action, int options = 0);

    IRegistration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, int options = 0);

    // endregion

    // region transferTo

    /**
     * 该接口用于方便构建子上下文
     * 1.子token会在当前token进入取消状态时被取消
     * 2.该接口本质是一个快捷方法，但允许子类优化
     */
    IRegistration ThenTransferTo(ICancelTokenSource child, int options = 0);

    IRegistration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0);

    // endregion

    #endregion
}