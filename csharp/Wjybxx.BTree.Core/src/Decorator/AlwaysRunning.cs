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
/// 在子节点完成之后仍返回运行。
/// 注意：在运行期间只运行一次子节点
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class AlwaysRunning<T> : Decorator<T> where T : class
{
    [NonSerialized] private bool started;

    public AlwaysRunning() {
    }

    public AlwaysRunning(Task<T> child) : base(child) {
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        started = false;
    }

    protected override void Execute() {
        Task<T> child = this.child;
        if (child == null) {
            return;
        }
        if (started && child.IsCompleted) { // 勿轻易调整
            return;
        }
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            started = true;
            Template_StartChild(child, true);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        // 不响应其它状态，但还是需要响应取消...
        if (child.IsCancelled) {
            SetCancelled();
        }
    }
}
}