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

namespace Wjybxx.BTree.Decorator
{
/// <summary>
/// 循环节点抽象
/// 注意：该模板类默认支持了尾递归优化，如果子类没有重写<see cref="Execute"/>方法，
/// 那么在<see cref="Task{T}.OnChildCompleted"/>方法中还需要判断是否启用了尾递归优化，如果启用了尾递归优化，
/// 也需要调用<see cref="Task{T}.Template_Execute"/>方法。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class LoopDecorator<T> : Decorator<T> where T : class
{
    /** 最大循环次数，超过次数直接失败；大于0有效 */
    protected int maxLoop = -1;
    [NonSerialized]
    protected int curLoop;

    protected LoopDecorator() {
    }

    protected LoopDecorator(Task<T> child) : base(child) {
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        curLoop = 0;
    }

    protected override void Execute() {
        if (IsTailRecursion) {
            // 尾递归优化--普通循环代替递归
            int reentryId = ReentryId;
            while (true) {
                Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
                if (inlinedRunningChild != null) {
                    Template_RunInlinedChild(inlinedRunningChild, inlineHelper, child);
                } else if (child.IsRunning) {
                    if (child.IsActiveInHierarchy) {
                        child.Template_Execute();
                    }
                } else {
                    curLoop++;
                    Template_RunChild(child);
                }
                if (CheckCancel(reentryId)) { // 得出结果或被取消
                    return;
                }
                if (child.IsRunning) { // 子节点未结束
                    return;
                }
            }
        } else {
            Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
            if (inlinedRunningChild != null) {
                Template_RunInlinedChild(inlinedRunningChild, inlineHelper, child);
            } else if (child.IsRunning) {
                if (child.IsActiveInHierarchy) {
                    child.Template_Execute();
                }
            } else {
                curLoop++;
                Template_RunChild(child);
            }
        }
    }

    /** 是否还有下一次循环 */
    public bool HasNextLoop() {
        return maxLoop <= 0 || curLoop < maxLoop;
    }

    /** 最大循环次数，用于序列化 */
    public int MaxLoop {
        get => maxLoop;
        set => maxLoop = value;
    }
}
}