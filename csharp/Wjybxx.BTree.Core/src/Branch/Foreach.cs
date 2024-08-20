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
/// 迭代所有的子节点最后返回成功
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class Foreach<T> : SingleRunningChildBranch<T> where T : class
{
    public Foreach() {
    }

    public Foreach(List<Task<T>>? children) : base(children) {
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
        if (IsAllChildCompleted) {
            SetSuccess();
        } else if (!IsExecuting() || !IsTailRecursion) {
            Template_Execute(false);
        }
    }
}
}