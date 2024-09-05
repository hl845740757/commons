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

using System.Collections.Generic;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 主动选择节点
/// 每次运行时都会重新测试节点的运行条件，选择一个新的可运行节点。
/// 如果新选择的运行节点与之前的运行节点不同，则取消之前的任务。
/// (ActiveSelector也是比较常用的节点，做内联支持是合适的)
/// </summary>
/// <typeparam name="T"></typeparam>
public class ActiveSelector<T> : SingleRunningChildBranch<T> where T : class
{
    public ActiveSelector() {
    }

    public ActiveSelector(List<Task<T>>? children) : base(children) {
    }

    protected override void Execute() {
        Task<T> childToRun = null;
        int childIndex = -1;
        for (int idx = 0; idx < children.Count; idx++) {
            Task<T> child = children[idx];
            if (!Template_CheckGuard(child.Guard)) {
                continue; // 不能调用SetGuardFailed，会中断当前运行中的child
            }
            childToRun = child;
            childIndex = idx;
            break;
        }

        Task<T>? runningChild = this.runningChild;
        if (runningChild != null && runningChild != childToRun) {
            runningChild.Stop();
            inlineHelper.StopInline();
            this.runningChild = null;
            this.runningIndex = -1;
        }

        if (childToRun == null) {
            SetFailed(TaskStatus.ERROR);
            return;
        }

        if (runningChild == childToRun) {
            Task<T> inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
            if (inlinedRunningChild != null) {
                Template_RunInlinedChild(inlinedRunningChild, inlineHelper, childToRun);
            } else if (childToRun.IsRunning) {
                childToRun.Template_Execute(true);
            } else {
                Template_StartChild(childToRun, false);
            }
        } else {
            this.runningChild = childToRun;
            this.runningIndex = childIndex;
            Template_StartChild(childToRun, false);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.StopInline();
        SetCompleted(child.Status, true);
    }
}
}