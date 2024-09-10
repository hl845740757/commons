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
using System.Diagnostics;
using Wjybxx.BTree.FSM;
using Wjybxx.Commons;

namespace Wjybxx.BTree
{
/// <summary>
/// 任务入口（可联想程序的Main）
/// 
/// 1. 该实现并不是典型的行为树实现，而是更加通用的任务树，因此命名TaskEntry。
/// 2. 该类允许继承，以提供一些额外的方法，但核心方法是禁止重写的。
/// 3. Entry的数据尽量也保存在黑板中，尤其是绑定的实体（Entity），尽可能使业务逻辑仅依赖黑板即可完成。
/// 4. Entry默认不检查<see cref="Task{T}.Guard"/>，如果需要由用户（逻辑上的control）检查。
/// 5. 如果要复用行为树，应当以树为单位整体复用，万莫以Task为单位复用 -- 节点之间的引用千丝万缕，容易内存泄漏。
/// 6. 该行为树虽然是事件驱动的，但心跳不是事件，仍需要每一帧调用<see cref="Update"/>方法。
/// 7. 避免直接使用外部的<see cref="CancelToken"/>，可将Entry的Token注册为外部的Child。
/// 
/// </summary>
public class TaskEntry<T> : Task<T> where T : class
{
    /** 行为树的名字 */
    private string? name;
    /** 行为树的根节点 */
    private Task<T>? rootTask;
    /** 行为树的类型 -- 用于加载时筛选 */
    private byte type;

    /** 行为树绑定的实体 -- 最好也存储在黑板里；这里的字段本是为了提高性能 */
    [NonSerialized] protected object? entity;
    /** 行为树加载器 -- 用于加载Task或配置 */
    [NonSerialized] protected ITreeLoader treeLoader;
    /** 当前帧号 */
    [NonSerialized] private int curFrame;
    /** 用于Entry的事件驱动 */
    [NonSerialized] protected ITaskEntryHandler<T>? handler;
    /** 用于内联优化 */
    [NonSerialized] protected readonly TaskInlineHelper<T> inlineHelper = new TaskInlineHelper<T>();

    public TaskEntry()
        : this(null, null, default) {
    }

    public TaskEntry(string? name, Task<T>? rootTask, T? blackboard,
                     object? entity = null, ITreeLoader? treeLoader = null) {
        this.name = name;
        this.rootTask = rootTask;
        this.blackboard = blackboard;
        this.entity = entity;
        this.treeLoader = treeLoader ?? ITreeLoader.NullLoader();

        this.taskEntry = this;
        this.cancelToken = new CancelToken();
    }

    #region getter/setter

    public string? Name {
        get => name;
        set => name = value;
    }

    public Task<T>? RootTask {
        get => rootTask;
        set => rootTask = value;
    }

    public byte Type {
        get => type;
        set => type = value;
    }

    public ITreeLoader TreeLoader {
        get => treeLoader;
        set => treeLoader = value ?? ITreeLoader.NullLoader();
    }

    public ITaskEntryHandler<T>? Handler {
        get => handler;
        set => handler = value;
    }

    public new object? Entity {
        get => entity;
        set => entity = value;
    }

    public int CurFrame => curFrame;

    #endregion

    #region logic

    /// <summary>
    /// C# await语法支持
    /// </summary>
    /// <returns></returns>
    public TaskAwaiter<T> GetAwaiter() => new TaskAwaiter<T>(this);
    
    /// <summary>
    /// 获取根状态机
    /// 状态机太重要了，值得我们为其提供各种快捷方法
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IllegalStateException"></exception>
    public StateMachineTask<T> GetRootStateMachine() {
        if (rootTask is StateMachineTask<T> stateMachine) {
            return stateMachine;
        }
        throw new IllegalStateException("rootTask is not state machine task");
    }

    /// <summary>
    /// 普通Update
    /// </summary>
    /// <param name="curFrame">当前帧号</param>
    public void Update(int curFrame) {
        this.curFrame = curFrame;
        if (IsRunning) {
            Template_Execute(true); // 用户就是control
        } else {
            Debug.Assert(IsInited());
            Template_Start(null, 0);
        }
    }

    /// <summary>
    /// 以内联的方式Update。
    /// 一般情况下，TaskEntry除了驱动root节点运行外，便没有额外逻辑，因此以内联的方式运行可省一些不必要的调用栈。
    /// </summary>
    /// <param name="curFrame">当前帧号</param>
    public void UpdateInlined(int curFrame) {
        this.curFrame = curFrame;
        if (IsRunning) {
            Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
            if (inlinedChild != null) {
                inlinedChild.Template_ExecuteInlined(inlineHelper, rootTask!);
            } else if (rootTask!.IsRunning) {
                rootTask.Template_Execute(true);
            } else {
                Template_StartChild(rootTask, true);
            }
        } else {
            Debug.Assert(IsInited());
            Template_Start(null, 0);
        }
    }

    /** 如果行为树代表的是一个条件树，则可以调用该方法；失败的情况下可以通过Status获取错误码 */
    public bool Test() {
        Debug.Assert(IsInited());
        Template_Start(null, MASK_CHECKING_GUARD); // entry本身不是条件节点
        return IsSucceeded;
    }

    protected override void Execute() {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(inlineHelper, rootTask!);
        } else if (rootTask!.IsRunning) {
            rootTask.Template_Execute(true);
        } else {
            Template_StartChild(rootTask, true);
        }
    }

    protected override void Exit() {
        inlineHelper.StopInline();
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        inlineHelper.StopInline();
        cancelToken.Reset(); // 避免内存泄漏

        SetCompleted(child.Status, true);
        if (handler != null) {
            handler.OnCompleted(this);
        }
    }

    public override bool CanHandleEvent(object eventObj) {
        return blackboard != null && rootTask != null; // 只测isInited的关键属性即可
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else if (rootTask != null) {
            rootTask.OnEvent(eventObj);
        }
    }

    public override void ResetForRestart() {
        base.ResetForRestart();
        cancelToken.Reset();
        curFrame = 0;
    }

    internal bool IsInited() {
        return rootTask != null && blackboard != null && cancelToken != null;
    }

    #endregion

#nullable disable

    #region child

    public override void VisitChildren(TaskVisitor<T> visitor, object param) {
        if (rootTask != null) visitor.VisitChild(rootTask, 0, param);
    }

    public sealed override int IndexChild(Task<T> task) {
        if (task != null && task == this.rootTask) {
            return 0;
        }
        return -1;
    }

    public override int ChildCount => rootTask == null ? 0 : 1;

    public override Task<T> GetChild(int index) {
        if (index == 0 && rootTask != null) {
            return rootTask;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    protected override int AddChildImpl(Task<T> task) {
        if (rootTask != null) {
            throw new IllegalStateException("Task entry cannot have more than one child");
        }
        rootTask = task;
        return 0;
    }

    protected override Task<T> SetChildImpl(int index, Task<T> task) {
        if (index == 0 && rootTask != null) {
            Task<T> r = this.rootTask;
            rootTask = task;
            return r;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    protected override Task<T> RemoveChildImpl(int index) {
        if (index == 0 && rootTask != null) {
            Task<T> r = this.rootTask;
            rootTask = null;
            return r;
        }
        throw new IndexOutOfRangeException(index.ToString());
    }

    #endregion
}
}