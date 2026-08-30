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

using Wjybxx.Commons;

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
    private ObjectPath path;
#nullable restore

    public SubtreeRef() {
    }

    public SubtreeRef(in ObjectPath path) {
        this.path = path;
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        if (child == null) {
            Task<T> rootTask = TaskEntry.TreeLoader.LoadRootTask<T>(path);
            AddChild(rootTask);
        }
    }

    protected override void Execute() {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            Template_StartChild(child, true);
        }
    }

    protected override void OnChildRunning(Task<T> child, bool starting) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        SetCompleted(child.Status, true);
    }

    /// <summary>
    /// 子树的名字
    /// </summary>
    public ObjectPath Path {
        get => path;
        set => path = value;
    }
}
}