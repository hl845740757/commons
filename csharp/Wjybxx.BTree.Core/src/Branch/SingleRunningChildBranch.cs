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
using Wjybxx.Commons;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 非并行分支节点抽象（最多只有一个运行中的子节点）
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SingleRunningChildBranch<T> : BranchTask<T> where T : class
{
#nullable disable
    /** 运行中的子节点索引*/
    [NonSerialized] protected int runningIndex = -1;
    /** 运行中的子节点*/
    [NonSerialized] protected Task<T> runningChild;

    /// <summary>
    /// 被内联运行的子节点
    /// 1.该字段定义在这里是为了减少抽象层次，该类并不提供功能。
    /// 2.子类要支持实现内联优化时，应当在<see cref="OnChildRunning"/>和<see cref="Task{T}.OnChildCompleted"/>维护字段引用。
    /// </summary>
    [NonSerialized]
    protected readonly TaskInlineHelper<T> inlineHelper = new TaskInlineHelper<T>();
#nullable enable

    protected SingleRunningChildBranch() {
    }

    protected SingleRunningChildBranch(List<Task<T>>? children) : base(children) {
    }

    protected SingleRunningChildBranch(Task<T> first, Task<T>? second) : base(first, second) {
    }

    #region open

    /** 允许外部在结束后查询 */
    public int GetRunningIndex() {
        return runningIndex;
    }

    /** 获取运行中的子节点 */
    public Task<T>? GetRunningChild() {
        return runningChild;
    }

    public TaskInlineHelper<T> GetInlineHelper() {
        return inlineHelper;
    }

    public int GetCompletedCount() {
        return runningIndex + 1;
    }

    /** 是否所有子节点已进入完成状态 */
    public bool IsAllChildCompleted => runningIndex + 1 >= children.Count;

    /** 进入完成状态的子节点数量 */
    public int CompletedCount => runningIndex + 1;

    /** 成功的子节点数量 */
    public int SucceededCount {
        get {
            int r = 0;
            for (int i = 0; i <= runningIndex; i++) {
                if (children[r].IsSucceeded) {
                    r++;
                }
            }
            return r;
        }
    }

    #endregion

    #region logic

    public override void ResetForRestart() {
        base.ResetForRestart();
        runningIndex = -1;
        runningChild = null;
        inlineHelper.StopInline();
    }

    /** 模板类不重写enter方法，只有数据初始化逻辑 */
    protected override void BeforeEnter() {
        // 这里不调用super是安全的
        runningIndex = -1;
        runningChild = null;
        // inlineHelper.StopInline();
    }

    protected override void Exit() {
        // index不立即重置，允许返回后查询
        runningChild = null;
        inlineHelper.StopInline();
    }

    protected override void StopRunningChildren() {
        // 停止需要从上层开始
        Stop(runningChild);
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T>? inlinedChild = inlineHelper.GetInlinedRunningChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else if (runningChild != null) {
            runningChild.OnEvent(eventObj);
        }
    }

    protected override void Execute() {
        Task<T>? runningChild = this.runningChild;
        if (runningChild == null) {
            this.runningChild = runningChild = NextChild();
            Template_RunChild(runningChild);
        } else {
            Task<T>? inlinedChild = inlineHelper.GetInlinedRunningChild();
            if (inlinedChild != null) {
                Template_RunInlinedChild(inlinedChild, inlineHelper, runningChild);
            } else if (runningChild.IsRunning) {
                runningChild.Template_Execute(true);
            } else {
                Template_RunChild(runningChild);
            }
        }
    }

    protected virtual Task<T> NextChild() {
        // 避免状态错误的情况下修改了index
        int nextIndex = runningIndex + 1;
        if (nextIndex < children.Count) {
            runningIndex = nextIndex;
            return children[nextIndex];
        }
        throw new IllegalStateException(IllegalStateMsg());
    }

    /** 没有可继续运行的子节点 */
    protected string IllegalStateMsg() {
        return $"numChildren: {children.Count}, currentIndex: {runningIndex}";
    }

    /** 子类如果支持内联，则重写该方法 */
    protected override void OnChildRunning(Task<T> child) {
        runningChild = child; // 子类可能未赋值
    }

    /// <summary>
    ///  子类的实现模板：
    /// <code>
    ///  protected void OnChildCompleted(Task child) {
    ///     runningChild = null;
    ///     inlinedHolder.Reset();
    ///     // 尝试计算结果（记得处理取消）
    ///      ...
    ///      // 如果未得出结果
    ///      if (!IsExecuting()) {
    ///         Template_Execute();
    ///     }
    ///  }
    /// </code>
    /// ps: 推荐子类重复编码避免调用base
    /// </summary>
    /// <param name="child"></param>
    protected override void OnChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.StopInline();
    }

    #endregion
}
}