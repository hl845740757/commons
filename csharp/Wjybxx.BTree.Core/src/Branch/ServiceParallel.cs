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
/// 服务并发节点
/// 1.其中第一个任务为主要任务，其余任务为后台服务。
/// 2.每次所有任务都会执行一次，但总是根据第一个任务执行结果返回结果。
/// 3.外部事件将派发给主要任务。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ServiceParallel<T> : Parallel<T> where T : class
{
    public ServiceParallel() {
    }

    public ServiceParallel(List<Task<T>>? children) : base(children) {
    }

    protected override void Enter(int reentryId) {
        InitChildHelpers(false);
    }

    protected override void Execute() {
        List<Task<T>> children = this.children;
        for (int idx = 0; idx < children.Count; idx++) {
            Task<T> child = children[idx];
            ParallelChildHelper<T> helper = GetChildHelper(child);
            Task<T> inlinedRunningChild = helper.GetInlinedRunningChild();
            if (inlinedRunningChild != null) {
                Template_RunInlinedChild(inlinedRunningChild, helper, child);
            } else if (child.IsRunning) {
                child.Template_Execute();
            } else {
                Template_RunChild(child);
            }
        }

        Task<T> mainTask = children[0];
        if (mainTask.IsCompleted) {
            SetCompleted(mainTask.Status, true);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        ParallelChildHelper<T> childHelper = GetChildHelper(child);
        childHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        ParallelChildHelper<T> childHelper = GetChildHelper(child);
        childHelper.StopInline();

        if (child == children[0] && !IsExecuting()) { // 测试是否正在心跳很重要
            SetSuccess();
        }
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T> mainTask = children[0];
        ParallelChildHelper<T> childHelper = GetChildHelper(mainTask);

        Task<T> inlinedRunningChild = childHelper.GetInlinedRunningChild();
        if (inlinedRunningChild != null) {
            inlinedRunningChild.OnEvent(eventObj);
        } else {
            mainTask.OnEvent(eventObj);
        }
    }
}
}