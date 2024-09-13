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
/// 重复运行子节点，直到该任务失败
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class UntilFail<T> : LoopDecorator<T> where T : class
{
    public UntilFail() {
    }

    public UntilFail(Task<T> child) : base(child) {
    }

    protected override void OnChildRunning(Task<T> child, bool starting) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        if (child.IsCancelled) {
            SetCancelled();
            return;
        }
        if (child.IsFailed) {
            SetSuccess();
        } else if (!HasNextLoop()) {
            SetFailed(TaskStatus.MAX_LOOP_LIMIT);
        } else {
            Template_Execute(false);
        }
    }
}
}