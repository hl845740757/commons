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

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 并行节点基类
/// 定义该类主要说明一些注意事项，包括：
/// 1.不建议在子节点完成事件中再次驱动子节点，避免运行<see cref="Task{T}.Execute"/>方法，否则可能导致其它task单帧内运行多次。
/// 2.如果有缓存数据，务必小心维护。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Parallel<T> : BranchTask<T> where T : class
{
    [NonSerialized]
    protected readonly List<ParallelChildHelper<T>> childHelpers = new List<ParallelChildHelper<T>>();

    protected Parallel() {
    }

    protected Parallel(List<Task<T>>? children) : base(children) {
    }

    public override void ResetForRestart() {
        base.ResetForRestart();
        ResetHelpers();
    }

    /** 模板类不重写enter方法，只有数据初始化逻辑 */
    protected override void BeforeEnter() {
        // ResetHelpers();
    }

    protected override void Exit() {
        ResetHelpers();
    }

    public ParallelChildHelper<T> GetChildHelper(int index) {
        return childHelpers[index];
    }

    /// <summary>
    /// 初始化child关联的helper
    /// 1.默认会设置为child的controlData，以避免反向查找开销。
    /// 2.建议在enter方法中调用。
    /// </summary>
    /// <param name="allocCancelToken">是否分配取消令牌</param>
    protected void InitChildHelpers(bool allocCancelToken) {
        List<ParallelChildHelper<T>> childHelpers = this.childHelpers;
        List<Task<T>> children = this.children;
        while (childHelpers.Count < children.Count) {
            childHelpers.Add(new ParallelChildHelper<T>());
        }
        for (int i = 0; i < children.Count; i++) {
            Task<T> child = children[i];
            ParallelChildHelper<T> childHelper = childHelpers[i];
            child.ControlData = childHelper;
            childHelper.reentryId = child.ReentryId;
            if (allocCancelToken && childHelper.cancelToken == null) {
                childHelper.cancelToken = cancelToken.NewInstance();
            } else {
                childHelper.cancelToken = null;
            }
        }
    }

    protected void ResetHelpers() {
        // 两者长度可能不一致
        foreach (Task<T> child in children) {
            child.ControlData = null;
        }
        foreach (ParallelChildHelper<T> helper in childHelpers) {
            helper.Reset();
        }
    }

    /// <summary>
    /// 1.并发节点通常不需要在该事件中将自己更新为运行状态，而是应该在<see cref="Task{T}.Execute"/>方法的末尾更新
    /// 2.实现类可以在该方法中内联子节点
    /// </summary>
    /// <param name="child"></param>
    protected override void OnChildRunning(Task<T> child) {
    }
}
}