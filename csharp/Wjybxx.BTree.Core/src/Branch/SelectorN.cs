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
using Wjybxx.BTree.Leaf;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 多选Selector。
/// 如果{required}小于等于0，则等同于<see cref="Success{T}"/>
/// 如果{required}等于1，则等同于<see cref="Selector{T}"/>
/// 如果{required}等于<code>children.code</code>，则在所有child成功之后成功 -- 默认不会提前失败。
/// 如果{required}大于<code>children.size</code>，则在所有child运行完成之后失败 -- 默认不会提前失败。
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class SelectorN<T> : SingleRunningChildBranch<T> where T : class
{
    /** 需要达成的次数 */
    private int required = 1;
    /** 是否快速失败 */
    private bool failFast;
    /** 当前计数 */
    [NonSerialized] private int count;

    public override void ResetForRestart() {
        base.ResetForRestart();
        count = 0;
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        count = 0;
    }

    protected override void Enter(int reentryId) {
        if (required < 1) {
            SetSuccess();
        } else if (ChildCount == 0) {
            SetFailed(TaskStatus.CHILDLESS);
        } else if (CheckFailFast()) {
            SetFailed(TaskStatus.INSUFFICIENT_CHILD);
        } else if (IsCheckingGuard()) {
            // 条件检测性能优化
            for (int i = 0; i < children.Count; i++) {
                Task<T> child = children[i];
                if (Template_CheckGuard(child) && ++count >= required) {
                    SetSuccess();
                    return;
                }
            }
            SetFailed(TaskStatus.ERROR);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.StopInline();
        if (child.IsCancelled) {
            SetCancelled();
            return;
        }
        if (child.IsSucceeded && ++count >= required) {
            SetSuccess();
        } else if (IsAllChildCompleted || CheckFailFast()) {
            SetFailed(TaskStatus.ERROR);
        } else {
            Template_Execute(false);
        }
    }

    private bool CheckFailFast() {
        return failFast && (children.Count - CompletedCount < required - count);
    }

    /** 需要达成的次数 */
    public int Required {
        get => required;
        set => required = value;
    }

    /** 是否快速失败 */
    public bool FailFast {
        get => failFast;
        set => failFast = value;
    }
}
}