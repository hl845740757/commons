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
using System.Collections.Generic;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Commons;

namespace Wjybxx.BTree.Branch
{
public class Join<T> : Parallel<T> where T : class
{
#nullable disable
    protected JoinPolicy<T> policy;
    /** 已进入完成状态的子节点 */
    [NonSerialized] protected int completedCount;
    /** 成功完成的子节点 */
    [NonSerialized] protected int succeededCount;
#nullable enable

    public Join() {
    }

    public Join(List<Task<T>>? children) : base(children) {
    }

    public override void ResetForRestart() {
        base.ResetForRestart();
        completedCount = 0;
        succeededCount = 0;
        policy.ResetForRestart();
    }

    protected override void BeforeEnter() {
        if (policy == null) {
            policy = JoinSequence<T>.GetInstance();
        }
        completedCount = 0;
        succeededCount = 0;
        // policy的数据重置
        policy.BeforeEnter(this);
    }

    protected override void Enter(int reentryId) {
        // 记录子类上下文 -- 由于beforeEnter可能改变子节点信息，因此在enter时处理
        InitChildHelpers(IsCancelTokenPerChild);
        policy.Enter(this);
    }

    protected override void Execute() {
        List<Task<T>> children = this.children;
        if (children.Count == 0) {
            return;
        }
        List<ParallelChildHelper<T>> childHelpers = this.childHelpers;
        int reentryId = ReentryId;
        for (int i = 0; i < children.Count; i++) {
            Task<T> child = children[i];
            ParallelChildHelper<T> childHelper = childHelpers[i];
            bool started = child.IsExited(childHelper.reentryId);
            if (started) {
                if (child.IsCompleted) {
                    continue; // 勿轻易调整--未重置的情况下可能是上一次的完成状态
                }
            } else {
                if (childHelper.cancelToken != null) {
                    cancelToken.AddListener(childHelper.cancelToken);
                    child.CancelToken = childHelper.cancelToken; // 运行前赋值
                }
            }
            Task<T>? inlinedRunningChild = childHelper.GetInlinedRunningChild();
            if (inlinedRunningChild != null) {
                Template_RunInlinedChild(inlinedRunningChild, childHelper, child);
            } else if (child.IsRunning) {
                child.Template_Execute();
            } else {
                Template_RunChild(child);
            }
            if (CheckCancel(reentryId)) {
                return;
            }
        }
        if (completedCount >= children.Count) { // child全部执行，但没得出结果
            throw new IllegalStateException();
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        ParallelChildHelper<T> childHelper = (ParallelChildHelper<T>)child.ControlData;
        childHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        ParallelChildHelper<T> childHelper = (ParallelChildHelper<T>)child.ControlData;
        childHelper.StopInline();
        // 删除分配的token
        if (childHelper.cancelToken != null) {
            cancelToken.RemListener(child.CancelToken);
            child.CancelToken.Reset();
            child.CancelToken = null;
        }
        completedCount++;
        if (child.IsSucceeded) {
            succeededCount++;
        }
        policy.OnChildCompleted(this, child);
    }

    protected override void OnEventImpl(object eventObj) {
        policy.OnEvent(this, eventObj);
    }

    // region

    public override bool IsAllChildCompleted => completedCount >= children.Count;

    public bool IsAllChildSucceeded => succeededCount >= children.Count;

    public int CompletedCount => completedCount;

    public int SucceededCount => succeededCount;
    // endregion

    public JoinPolicy<T> Policy {
        get => policy;
        set => policy = value;
    }
}
}