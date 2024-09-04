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
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class LoopDecorator<T> : Decorator<T> where T : class
{
    /** 最大循环次数，超过次数直接失败；大于0有效 */
    protected int maxLoop = -1;
    /** 执行前+1，因此从1开始 */
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
        Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
        if (inlinedRunningChild != null) {
            Template_RunInlinedChild(inlinedRunningChild, inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            curLoop++;
            Template_RunChild(child);
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