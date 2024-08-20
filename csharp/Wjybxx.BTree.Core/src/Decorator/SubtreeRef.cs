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
/// 子树引用
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class SubtreeRef<T> : Decorator<T> where T : class
{
#nullable disable
    private string subtreeName;
#nullable enable

    public SubtreeRef() {
    }

    public SubtreeRef(string subtreeName) {
        this.subtreeName = subtreeName;
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        if (child == null) {
            Task<T> rootTask = TaskEntry.TreeLoader.LoadRootTask<T>(subtreeName);
            AddChild(rootTask);
        }
    }

    protected override void Execute() {
        Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
        if (inlinedRunningChild != null) {
            Template_RunInlinedChild(inlinedRunningChild, inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            Template_RunChild(child);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        SetCompleted(child.Status, true);
    }

    /// <summary>
    /// 子树的名字
    /// </summary>
    public string SubtreeName {
        get => subtreeName;
        set => subtreeName = value;
    }
}
}