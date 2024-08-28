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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 取消码辅助类
/// </summary>
public static class CancelCodes
{
    /**
     * 原因的掩码
     * 1.如果cancelCode不包含其它信息，就等于reason
     * 2.设定为20位，可达到100W
     */
    public const int MASK_REASON = 0xFFFFF;
    /** 紧迫程度的掩码（4it）-- 0表示未指定 */
    public const int MASK_DEGREE = 0x00F0_0000;
    /** 预留4bit */
    public const int MASK_REVERSED = 0x0F00_0000;
    /** 中断的掩码 （1bit） */
    public const int MASK_INTERRUPT = 1 << 28;
    /** 告知任务无需执行删除逻辑 -- 慎用 */
    public const int MASK_WITHOUT_REMOVE = 1 << 29;
    /** 表示取消信号来自Future的取消接口 -- c#端无用 */
    public const int MASK_FROM_FUTURE = 1 << 30;

    /** 最大取消原因 */
    public const int MAX_REASON = MASK_REASON;
    /** 最大紧急程度 */
    public const int MAX_DEGREE = 15;

    /** 取消原因的偏移量 */
    public const int OFFSET_REASON = 0;
    /** 紧急度的偏移量 */
    public const int OFFSET_DEGREE = 20;

    /** 默认原因 */
    public const int REASON_DEFAULT = 1;
    /** 执行超时 -- {@link ICancelTokenSource#cancelAfter(int, long, TimeUnit)}就可使用 */
    public const int REASON_TIMEOUT = 2;
    /** IExecutor关闭 -- IExecutor关闭不一定会取消任务 */
    public const int REASON_SHUTDOWN = 3;
    /** 执行超时，触发次数限制 */
    public const int REASON_TRIGGER_COUNT_LIMIT = 4;

    #region query

    /** 计算取消码中的原因 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetReason(int code) {
        return code & MASK_REASON;
    }

    /** 计算取消码终归的紧急程度 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDegree(int code) {
        return (code & MASK_DEGREE) >> OFFSET_DEGREE;
    }

    /** 取消指令中是否要求了中断线程 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInterruptible(int code) {
        return (code & MASK_INTERRUPT) != 0;
    }

    /** 取消指令中是否要求了无需删除 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWithoutRemove(int code) {
        return (code & MASK_WITHOUT_REMOVE) != 0;
    }

    /** 取消信号是否来自future接口 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFromFuture(int code) {
        return (code & MASK_FROM_FUTURE) != 0;
    }

    #endregion

    #region util

    /** 设置紧急程度 */
    public static int SetDegree(int code, int value) {
        if (value < 0 || value > MAX_DEGREE) {
            throw new ArgumentException("degree");
        }
        code &= (~MASK_DEGREE);
        code |= (value << OFFSET_DEGREE);
        return code;
    }

    /** 设置取消原因 */
    public static int SetReason(int code, int value) {
        if (value <= 0 || value > MAX_REASON) {
            throw new ArgumentException("reason");
        }
        code &= (~MASK_REASON);
        code |= value;
        return code;
    }

    /** 设置中断标记 */
    public static int SetInterruptible(int code, bool value) {
        return value
            ? code | MASK_INTERRUPT
            : code & (~MASK_INTERRUPT);
    }

    /** 设置是否不立即删除 */
    public static int SetWithoutRemove(int code, bool value) {
        return value
            ? code | MASK_WITHOUT_REMOVE
            : code & (~MASK_WITHOUT_REMOVE);
    }

    #endregion

    /**
     * 检查取消码的合法性
     *
     * @return argument
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CheckCode(int code) {
        if (GetReason(code) == 0) {
            throw new ArgumentException("reason is absent");
        }
        return code;
    }
}
}