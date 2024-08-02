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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BTree
{
/// <summary>
/// 分支节点抽象
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BranchTask<T> : Task<T> where T : class
{
#nullable disable
    protected List<Task<T>> children;
#nullable enable

    protected BranchTask() {
        children = new List<Task<T>>();
    }

    protected BranchTask(List<Task<T>>? children) {
        this.children = children ?? new List<Task<T>>();
    }

    protected BranchTask(Task<T> first, Task<T>? second) {
        if (first == null) throw new ArgumentNullException(nameof(first));
        children = new List<Task<T>>(2);
        children.Add(first);
        if (second != null) {
            children.Add(second);
        }
    }

    #region

    /** 是否是第一个子节点 */
    public bool IsFirstChild(Task<T> child) {
        int count = this.children.Count;
        if (count == 0) {
            return false;
        }
        return this.children[0] == child;
    }

    /** 是否是第最后一个子节点 */
    public bool IsLastChild(Task<T> child) {
        int count = this.children.Count;
        if (count == 0) {
            return false;
        }
        return children[count - 1] == child;
    }

    /** 获取第一个子节点 -- 主要为MainPolicy提供帮助 */
    public Task<T>? GetFirstChild() {
        int size = children.Count;
        return size > 0 ? children[0] : null;
    }

    /** 获取最后一个子节点 */
    public Task<T>? GetLastChild() {
        int size = children.Count;
        return size > 0 ? children[size - 1] : null;
    }

    /** 是否所有的子节点已进入完成状态 */
    public virtual bool IsAllChildCompleted {
        get {
            // 在判断是否全部完成这件事上，逆序遍历有优势
            for (int idx = children.Count - 1; idx >= 0; idx--) {
                Task<T> child = children[idx];
                if (child.IsRunning) {
                    return false;
                }
            }
            return true;
        }
    }

    /** 用于避免测试的子节点过于规律 */
    internal void ShuffleChild() {
        CollectionUtil.Shuffle(children);
    }

    #endregion

#nullable disable

    #region child

    public sealed override void RemoveAllChild() {
        children.ForEach(e => e.UnsetControl());
        children.Clear();
    }

    public sealed override int IndexChild(Task<T> task) {
        return CollectionUtil.IndexOfRef(children, task);
    }

    public sealed override List<Task<T>> ListChildren() {
        return new List<Task<T>>(children);
    }

    public sealed override int GetChildCount() {
        return children.Count;
    }

    public sealed override Task<T> GetChild(int index) {
        return children[index];
    }

    protected sealed override int AddChildImpl(Task<T> task) {
        children.Add(task);
        return children.Count - 1;
    }

    protected sealed override Task<T> SetChildImpl(int index, Task<T> task) {
        return children[index] = task;
    }

    protected sealed override Task<T> RemoveChildImpl(int index) {
        Task<T> child = children[index];
        children.RemoveAt(index);
        return child;
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 该接口仅用于序列化
    /// </summary>
    public List<Task<T>> Children {
        get => children;
        set => children = value ?? new List<Task<T>>();
    }

    #endregion
}
}