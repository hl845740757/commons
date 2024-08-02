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
using Wjybxx.Commons;

namespace Wjybxx.BTree.Decorator
{
/// <summary>
/// 常量工具类
/// </summary>
public static class RepeatMode
{
    /** 总是计数 */
    public const int MODE_ALWAYS = 0;
    /** 成功时计数 */
    public const int MODE_ONLY_SUCCESS = 1;
    /** 失败时计数 */
    public const int MODE_ONLY_FAILED = 2;
    /** 不计数 - 可能无限执行 */
    public const int MODE_NEVER = 3;
}

/// <summary>
/// 重复N次
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class Repeat<T> : LoopDecorator<T> where T : class
{
    /** 考虑到Java枚举与其它语言的兼容性问题，我们在编辑器中使用数字 */
    private int countMode = RepeatMode.MODE_ALWAYS;
    /** 需要重复的次数，-1表示无限重复 */
    private int required = 1;
    /** 当前计数 */
    [NonSerialized] private int count;

    public override void ResetForRestart() {
        base.ResetForRestart();
        count = 0;
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        if (required < -1) {
            throw new IllegalStateException("required < -1");
        }
        count = 0;
    }

    protected override void Enter(int reentryId) {
        base.Enter(reentryId);
        if (required == 0) {
            SetSuccess();
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        if (child.IsCancelled) {
            SetCancelled();
            return;
        }
        bool match = countMode switch
        {
            RepeatMode.MODE_ALWAYS => true,
            RepeatMode.MODE_ONLY_SUCCESS => child.IsSucceeded,
            RepeatMode.MODE_ONLY_FAILED => child.IsFailed,
            _ => false
        };
        if (match) {
            count++;
            if (required >= 0 && count >= required) {
                SetSuccess();
                return;
            }
        }

        if (!IsExecuting() || !IsTailRecursion) {
            Template_Execute();
        }
    }

    #region 序列化

    /** 计数模式 */
    public int CountMode {
        get => countMode;
        set => countMode = value;
    }

    /** 期望的次数 */
    public int Required {
        get => required;
        set => required = value;
    }

    #endregion
}
}