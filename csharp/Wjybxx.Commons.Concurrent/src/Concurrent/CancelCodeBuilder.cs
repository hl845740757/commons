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
/// 用于构建取消码
/// </summary>
public struct CancelCodeBuilder
{
    private int code = ICancelToken.REASON_DEFAULT;

    public CancelCodeBuilder() {
    }

    /// <summary>
    /// 取消的原因
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Reason {
        get => ICancelToken.Reason(code);
        set {
            if (value <= 0 || value > ICancelToken.MAX_REASON) {
                throw new ArgumentException("reason");
            }
            code &= (~ICancelToken.MASK_REASON);
            code |= value;
        }
    }

    /// <summary>
    /// 紧急程度
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public int Degree {
        get => ICancelToken.Degree(code);
        set {
            if (value < 0 || value > ICancelToken.MAX_DEGREE) {
                throw new ArgumentException("degree");
            }
            code &= (~ICancelToken.MASK_DEGREE);
            code |= (value << ICancelToken.OFFSET_DEGREE);
        }
    }

    /// <summary>
    /// 是否中断线程
    /// </summary>
    public bool IsInterruptible {
        get => ICancelToken.IsInterruptible(code);
        set {
            if (value) {
                code |= ICancelToken.MASK_INTERRUPT;
            } else {
                code &= (~ICancelToken.MASK_INTERRUPT);
            }
        }
    }

    /// <summary>
    /// 是否无需立即从任务队列中删除
    /// </summary>
    public bool IsWithoutRemove {
        get => ICancelToken.IsWithoutRemove(code);
        set {
            if (value) {
                code |= ICancelToken.MASK_WITHOUT_REMOVE;
            } else {
                code &= (~ICancelToken.MASK_WITHOUT_REMOVE);
            }
        }
    }

    /// <summary>
    /// 构建最终的取消码
    /// </summary>
    /// <returns></returns>
    public int Build() => code;
}