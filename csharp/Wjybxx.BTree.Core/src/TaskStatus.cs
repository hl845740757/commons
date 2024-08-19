#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BTree
{
/// <summary>
/// Task的基础状态码
/// </summary>
public class TaskStatus
{
    /** 初始状态 */
    public const int NEW = 0;
    /** 执行中 */
    public const int RUNNING = 1;
    /** 执行成功 -- 最小的完成状态 */
    public const int SUCCESS = 2;

    /** 被取消 -- 需要放在所有失败码的前面，用户可以可以通过比较大小判断；向上传播时要小心 */
    public const int CANCELLED = 3;
    /** 默认失败码 -- 是最小的失败码 */
    public const int ERROR = 4;
    /** 前置条件检查失败 -- 未运行的情况下直接失败；注意！该错误码不能向父节点传播 */
    public const int GUARD_FAILED = 5;
    /** 没有子节点 */
    public const int CHILDLESS = 6;
    /** 子节点不足 */
    public const int INSUFFICIENT_CHILD = 7;
    /** 执行超时 */
    public const int TIMEOUT = 8;
    /** 循环结束 */
    public const int MAX_LOOP_LIMIT = 9;

    /** 这是Task类能捕获的最大前一个状态的值，超过该值时将被修正该值 */
    public const int MAX_PREV_STATUS = 31;

    //
    /** 任务是否正在运行 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRunning(int status) {
        return status == TaskStatus.RUNNING;
    }

    /** 任务是否已完成(成功、失败、取消) */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(int status) {
        return status >= TaskStatus.SUCCESS;
    }

    /** 任务是否已成功 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSucceeded(int status) {
        return status == TaskStatus.SUCCESS;
    }

    /** 任务是否已被取消 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCancelled(int status) {
        return status == TaskStatus.CANCELLED;
    }

    /** 任务是否已失败 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFailed(int status) {
        return status > TaskStatus.CANCELLED;
    }

    /** 任务是否已失败或被取消 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFailedOrCancelled(int status) {
        return status >= TaskStatus.CANCELLED;
    }

    //

    /** 将给定状态码归一化，所有的失败码将被转为<code>ERROR</code>  */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Normalize(int status) {
        if (status < 0) return 0;
        return status > ERROR ? ERROR : status;
    }

    /** 如果给定状态是失败码，则返回参数，否则返回默认失败码 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToFailure(int status) {
        return status < ERROR ? ERROR : status;
    }

    /** 如果给定状态是失败码，则返回成功；如果是成功，则返回默认失败码；如果是取消则返回取消； */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Invert(int status) {
        if (status < SUCCESS) {
            throw new ArgumentException(nameof(status));
        }
        if (status == CANCELLED) return CANCELLED;
        return status == SUCCESS ? ERROR : SUCCESS;
    }
}
}