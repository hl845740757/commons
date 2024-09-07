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
/// 反转装饰器，它用于反转子节点的执行结果。
/// 如果被装饰的任务失败，它将返回成功；
/// 如果被装饰的任务成功，它将返回失败；
/// 如果被装饰的任务取消，它将返回取消。
///
/// 对于普通的条件节点，可以通过控制流标记直接取反<see cref="Task{T}.IsInvertedGuard"/>，避免增加封装。
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class Inverter<T> : Decorator<T> where T : class
{
    public Inverter() {
    }

    public Inverter(Task<T> child) : base(child) {
    }

    protected override void Enter(int reentryId) {
        if (IsCheckingGuard()) {
            Template_CheckGuard(child);
            SetCompleted(TaskStatus.Invert(child.Status), true);
        }
    }

    protected override void Execute() {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            Template_StartChild(child, true);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        SetCompleted(TaskStatus.Invert(child.Status), true);
    }
}
}