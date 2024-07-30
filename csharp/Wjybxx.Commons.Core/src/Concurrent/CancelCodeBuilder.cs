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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于构建取消码
/// </summary>
public struct CancelCodeBuilder
{
    private int code;

    public CancelCodeBuilder(int reason) {
        this.code = reason;
    }

    /// <summary>
    /// 启用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Enable(int optionMask) {
        code |= optionMask;
    }

    /// <summary>
    /// 禁用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Disable(int optionMask) {
        code &= ~optionMask;
    }

    /// <summary>
    /// 取消的原因
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Reason {
        get => CancelCodes.GetReason(code);
        set => code = CancelCodes.SetReason(code, value);
    }

    /// <summary>
    /// 紧急程度
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Degree {
        get => CancelCodes.GetDegree(code);
        set => code = CancelCodes.SetDegree(code, value);
    }

    /// <summary>
    /// 是否中断线程
    /// </summary>
    public bool IsInterruptible {
        get => CancelCodes.IsInterruptible(code);
        set => code = CancelCodes.SetInterruptible(code, value);
    }

    /// <summary>
    /// 是否无需立即从任务队列中删除
    /// </summary>
    public bool IsWithoutRemove {
        get => CancelCodes.IsWithoutRemove(code);
        set => code = CancelCodes.SetWithoutRemove(code, value);
    }

    /// <summary>
    /// 构建最终的取消码
    /// </summary>
    /// <returns></returns>
    public int Build() => code;
}
}