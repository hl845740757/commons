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

namespace Wjybxx.BTree.Decorator
{
/// <summary>
/// 每一帧都检查子节点的前置条件，如果前置条件失败，则取消child执行并返回失败。
/// 这是一个常用的节点类型，我们做内联优化，可以提高效率。
/// </summary>
/// <typeparam name="T"></typeparam>
public class AlwaysCheckGuard<T> : Decorator<T> where T : class
{
    public AlwaysCheckGuard() {
    }

    public AlwaysCheckGuard(Task<T> child) : base(child) {
    }

    protected override void Execute() {
        if (Template_CheckGuard(child.Guard)) {
            Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
            if (inlinedRunningChild != null) {
                Template_RunInlinedChild(inlinedRunningChild, inlineHelper, child);
            } else if (child.IsRunning) {
                child.Template_Execute(true);
            } else {
                Template_RunChildDirectly(child);
            }
        } else {
            child.Stop();
            inlineHelper.StopInline(); // help gc
            SetFailed(TaskStatus.ERROR);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        SetCompleted(child.Status, true);
    }
}
}