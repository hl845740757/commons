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
using Wjybxx.Commons;

namespace Wjybxx.BTree
{
/// <summary>
/// 装饰节点基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Decorator<T> : Task<T> where T : class
{
#nullable disable
    protected Task<T> child;

    /// <summary>
    /// 被内联运行的子节点
    /// 1.该字段定义在这里是为了减少抽象层次，该类并不提供功能。
    /// 2.子类要支持实现内联优化时，应当在<see cref="OnChildRunning"/>和<see cref="Task{T}.OnChildCompleted"/>维护字段引用。
    /// </summary>
    [NonSerialized]
    protected readonly TaskInlineHelper<T> inlineHelper = new TaskInlineHelper<T>();
#nullable enable

    public Decorator() {
    }

    public Decorator(Task<T> child) {
        this.child = child;
    }

    public TaskInlineHelper<T> GetInlineHelper() {
        return inlineHelper;
    }

    #region logic

    public override void ResetForRestart() {
        base.ResetForRestart();
        inlineHelper.StopInline();
    }

    protected override void Exit() {
        inlineHelper.StopInline();
    }

    protected override void StopRunningChildren() {
        Stop(child);
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else if (child != null) {
            child.OnEvent(eventObj);
        }
    }

    /** 子类如果支持内联，则重写该方法(超类不能安全内联) */
    protected override void OnChildRunning(Task<T> child) {
    }

    #endregion

#nullable disable

    #region child

    public override void VisitChildren(TaskVisitor<T> visitor, object param) {
        if (child != null) visitor.VisitChild(child, 0, param);
    }

    public sealed override int IndexChild(Task<T> task) {
        if (task != null && task == this.child) {
            return 0;
        }
        return -1;
    }

    public override int ChildCount => child == null ? 0 : 1;

    public sealed override Task<T> GetChild(int index) {
        if (index == 0 && child != null) {
            return child;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    protected sealed override int AddChildImpl(Task<T> task) {
        if (child != null) {
            throw new IllegalStateException("Decorator cannot have more than one child");
        }
        child = task;
        return 0;
    }

    protected sealed override Task<T> SetChildImpl(int index, Task<T> task) {
        if (index == 0 && child != null) {
            Task<T> r = this.child;
            child = task;
            return r;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    protected sealed override Task<T> RemoveChildImpl(int index) {
        if (index == 0 && child != null) {
            Task<T> r = this.child;
            child = null;
            return r;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    # endregion

    #region 序列化

    public Task<T> Child {
        get => child;
        set => child = value;
    }

    #endregion
}
}