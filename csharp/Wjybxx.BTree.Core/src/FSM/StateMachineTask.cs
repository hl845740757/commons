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
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 状态机节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class StateMachineTask<T> : Decorator<T> where T : class
{
    private static readonly IDeque<Task<T>> EMPTY_QUEUE = EmptyDequeue<Task<T>>.Instance;

    /** 状态机名字 */
    private string? name;
    /** 无可用状态时状态码 -- 默认成功退出更安全 */
    private int noneChildStatus = TaskStatus.SUCCESS;
    /** 初始状态 */
    private Task<T>? initState;
    /** 初始状态的属性 */
    private object? initStateProps;

    [NonSerialized] private Task<T>? tempNextState;
    [NonSerialized] private IDeque<Task<T>> undoQueue = EMPTY_QUEUE;
    [NonSerialized] private IDeque<Task<T>> redoQueue = EMPTY_QUEUE;
#nullable disable
    [NonSerialized] private ICancelToken childCancelToken;
#nullable enable

    [NonSerialized] private StateMachineListener<T>? listener;
    [NonSerialized] private StateMachineHandler<T>? stateMachineHandler;

    #region MyRegion

    /** 获取当前状态 */
    public Task<T>? GetCurState() {
        return child;
    }

    /** 获取临时的下一个状态 */
    public Task<T>? GetTempNextState() {
        return tempNextState;
    }

    /** 丢弃未切换的临时状态 */
    public Task<T>? DiscardTempNextState() {
        Task<T>? r = tempNextState;
        if (r != null) tempNextState = null;
        return r;
    }

    /** 对当前当前状态发出取消命令 */
    public void CancelCurState(int cancelCode) {
        if (child != null && child.IsRunning) {
            child.CancelToken.Cancel(cancelCode);
        }
    }

    /** 查看undo对应的state */
    public Task<T>? PeekUndoState() {
        return undoQueue.TryPeekLast(out Task<T> r) ? r : null;
    }

    /** 查看redo对应的state */
    public Task<T>? PeekRedoState() {
        return redoQueue.TryPeekFirst(out Task<T> r) ? r : null;
    }

    /** 开放以允许填充 */
    public IDeque<Task<T>> GetUndoQueue() {
        return undoQueue;
    }

    /** 开放以允许填充 */
    public IDeque<Task<T>> GetRedoQueue() {
        return redoQueue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxSize">最大大小；0表示禁用；大于0启用</param>
    /// <returns>最新的queue</returns>
    public IDeque<Task<T>> SetUndoQueueSize(int maxSize) {
        if (maxSize < 0) throw new ArgumentException("maxSize: " + maxSize);
        return undoQueue = SetQueueMaxSize(undoQueue, maxSize, DequeOverflowBehavior.DiscardHead);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxSize">最大大小；0表示禁用；大于0启用</param>
    /// <returns>最新的queue</returns>
    public IDeque<Task<T>> SetRedoQueueSize(int maxSize) {
        if (maxSize < 0) throw new ArgumentException("maxSize: " + maxSize);
        return redoQueue = SetQueueMaxSize(redoQueue, maxSize, DequeOverflowBehavior.DiscardTail);
    }

    private static IDeque<Task<T>> SetQueueMaxSize(IDeque<Task<T>> queue, int maxSize, DequeOverflowBehavior overflowBehavior) {
        if (maxSize == 0) {
            queue.Clear();
            return EMPTY_QUEUE;
        }
        if (queue == EMPTY_QUEUE) {
            return new BoundedArrayDeque<Task<T>>(maxSize, overflowBehavior);
        } else {
            BoundedArrayDeque<T> boundedArrayDeque = (BoundedArrayDeque<T>)queue;
            boundedArrayDeque.SetCapacity(maxSize, overflowBehavior);
            return queue;
        }
    }

    /// <summary>
    /// 撤销到前一个状态
    /// </summary>
    /// <returns>如果有前一个状态则返回true</returns>
    public bool UndoChangeState() {
        return UndoChangeState(ChangeStateArgs.UNDO);
    }

    /// <summary>
    /// 撤销到前一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有前一个状态则返回true</returns>
    public virtual bool UndoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.IsUndo()) {
            throw new ArgumentException();
        }
        // 真正切换以后再删除
        if (!undoQueue.TryPeekLast(out Task<T> prevState)) {
            return false;
        }
        ChangeState(prevState, changeStateArgs);
        return true;
    }

    /// <summary>
    /// 重新进入到下一个状态
    /// </summary>
    /// <returns>如果有下一个状态则返回true</returns>
    public bool RedoChangeState() {
        return RedoChangeState(ChangeStateArgs.REDO);
    }

    /// <summary>
    /// 重新进入到下一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有下一个状态则返回true</returns>
    public virtual bool RedoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.IsRedo()) {
            throw new ArgumentException();
        }
        // 真正切换以后再删除
        if (!redoQueue.TryPeekFirst(out Task<T> nextState)) {
            return false;
        }
        ChangeState(nextState, changeStateArgs);
        return true;
    }

    /** 切换状态 -- 如果状态机处于运行中，则立即切换 */
    public void ChangeState(Task<T> nextState) {
        ChangeState(nextState, ChangeStateArgs.PLAIN);
    }

    /// <summary>
    /// 切换状态
    /// 1.如果当前有一个待切换的状态，则会被悄悄丢弃(todo 可以增加一个通知)
    /// 2.无论何种模式，在当前状态进入完成状态时一定会触发
    /// 3.如果状态机未运行，则仅仅保存在那里，等待下次运行的时候执行
    /// 4.当前状态可先正常完成，然后再切换状态，就可以避免进入被取消状态；可参考<see cref="ChangeStateTask{T}"/>
    /// <code>
    ///      Task nextState = nextState();
    ///      setSuccess();
    ///      stateMachine.changeState(nextState)
    /// </code>
    /// 
    /// </summary>
    /// <param name="nextState">要进入的下一个状态</param>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ChangeState(Task<T> nextState, ChangeStateArgs changeStateArgs) {
        if (nextState == null) throw new ArgumentNullException(nameof(nextState));
        if (changeStateArgs == null) throw new ArgumentNullException(nameof(changeStateArgs));

        changeStateArgs = CheckArgs(changeStateArgs);
        nextState.ControlData = changeStateArgs;
        tempNextState = nextState;

        if (IsRunning && IsReady(child, nextState)) {
            Template_Execute();
        }
    }

    /** 检测正确性和自动初始化；不可修改掉cmd */
    protected virtual ChangeStateArgs CheckArgs(ChangeStateArgs changeStateArgs) {
        // 当前未运行，不能指定延迟帧号
        if (!IsRunning) {
            if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
                throw new ArgumentException("invalid args");
            }
            return changeStateArgs.WithDelayMode(ChangeStateArgs.DELAY_NONE);
        }
        // 运行中一定可以拿到帧号
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
            if (changeStateArgs.frame < 0) {
                return changeStateArgs.WithFrame(taskEntry.CurFrame + 1);
            }
        }
        return changeStateArgs;
    }

    #endregion

    #region logic

    public override void ResetForRestart() {
        base.ResetForRestart();
        if (stateMachineHandler != null) {
            stateMachineHandler.ResetForRestart(this);
        }
        if (initState != null) {
            initState.ResetForRestart();
        }
        if (child != null) {
            RemoveChild(0);
        }
        tempNextState = null;
        undoQueue.Clear(); // 保留用户的设置
        redoQueue.Clear();
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        if (stateMachineHandler != null) {
            stateMachineHandler.BeforeEnter(this);
        }
        if (childCancelToken == null) {
            childCancelToken = cancelToken.NewInstance();
        }
        if (noneChildStatus == 0) { // 兼容编辑器忘记赋值，默认成功退出更安全
            noneChildStatus = TaskStatus.SUCCESS;
        }
        if (initState != null && initStateProps != null) {
            initState.SharedProps = initStateProps;
        }
        if (tempNextState == null && initState != null) { // 允许运行前调用changeState
            tempNextState = initState;
        }
        if (tempNextState != null && tempNextState.ControlData == null) {
            tempNextState.ControlData = ChangeStateArgs.PLAIN;
        }
        if (child != null) {
            child.CancelToken = childCancelToken;
        }
    }

    protected override void Exit() {
        if (child != null) {
            RemoveChild(0);
        }
        tempNextState = null;
        undoQueue.Clear();
        redoQueue.Clear();
        base.Exit();
    }

    protected override void Execute() {
        Task<T> curState = this.child;
        Task<T>? nextState = this.tempNextState;
        if (nextState != null && IsReady(curState, nextState)) {
            this.tempNextState = null;
            if (!Template_CheckGuard(nextState.Guard)) { // 下个状态无效
                nextState.SetGuardFailed(null);
                if (stateMachineHandler != null) { // 通知特殊情况
                    stateMachineHandler.OnNextStateGuardFailed(this, nextState);
                }
            } else {
                if (curState != null) {
                    curState.Stop();
                    inlineHelper.StopInline(); // help gc
                }
                ChangeStateArgs changeStateArgs = (ChangeStateArgs)nextState.ControlData;
                switch (changeStateArgs.cmd) {
                    case ChangeStateArgs.CMD_UNDO: {
                        undoQueue.TryRemoveLast(out _);
                        if (curState != null) {
                            redoQueue.TryAddFirst(curState);
                        }
                        break;
                    }
                    case ChangeStateArgs.CMD_REDO: {
                        redoQueue.TryRemoveFirst(out _);
                        if (curState != null) {
                            undoQueue.TryAddLast(curState);
                        }
                        break;
                    }
                    default: {
                        // 进入新状态，需要清理redo队列
                        redoQueue.Clear();
                        if (curState != null) {
                            undoQueue.AddLast(curState);
                        }
                        break;
                    }
                }
                NotifyChangeState(curState, nextState);

                curState = nextState;
                cancelToken.AddListener(childCancelToken); // 需要每次添加
                curState.CancelToken = childCancelToken; // state可独立取消 -- 可复用cancelToken
                curState.ControlData = null;
                if (child != null) {
                    SetChild(0, curState);
                } else {
                    AddChild(curState);
                }
            }
        }
        if (curState == null) { // 当前无可用状态
            OnNoChildRunning();
            return;
        }

        // 继续运行或新状态enter；在尾部才能保证安全
        Task<T>? inlinedRunningChild = inlineHelper.GetInlinedRunningChild();
        if (inlinedRunningChild != null) {
            Template_RunInlinedChild(inlinedRunningChild, inlineHelper, curState);
        } else if (curState.IsRunning) {
            curState.Template_Execute();
        } else {
            Template_RunChildDirectly(curState);
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        Debug.Assert(this.child == child);
        inlineHelper.StopInline();
        cancelToken.RemListener(childCancelToken);
        childCancelToken.Reset();
        child.CancelToken = null;

        if (tempNextState == null) {
            if (stateMachineHandler != null && stateMachineHandler.OnNextStateAbsent(this, child)) {
                return;
            }
            undoQueue.AddLast(child);
            RemoveChild(0);
            NotifyChangeState(child, null);
            OnNoChildRunning();
        } else {
            ChangeStateArgs? changeStateArgs = (ChangeStateArgs?)tempNextState.ControlData;
            if (changeStateArgs == null) {
                tempNextState.ControlData = ChangeStateArgs.PLAIN;
            } else {
                // 必须立即切换，保留其它参数(extraInfo - 用户需要)
                tempNextState.ControlData = changeStateArgs.WithDelayMode(ChangeStateArgs.DELAY_NONE);
            }
            Template_Execute();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnNoChildRunning() {
        if (noneChildStatus != TaskStatus.RUNNING) {
            SetCompleted(noneChildStatus, false);
        }
    }

    protected virtual bool IsReady(Task<T>? curState, Task<T> nextState) {
        if (curState == null) {
            return true;
        }
        ChangeStateArgs changeStateArgs = (ChangeStateArgs)nextState.ControlData;
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_CURRENT_COMPLETED) {
            return curState.IsCompleted;
        }
        if (changeStateArgs.delayMode == ChangeStateArgs.DELAY_NEXT_FRAME) {
            return taskEntry.CurFrame >= changeStateArgs.frame;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void NotifyChangeState(Task<T>? curState, Task<T>? nextState) {
        Debug.Assert(curState != null || nextState != null);
        if (listener != null) listener(this, curState, nextState);
    }

    #endregion

    #region find

    /**
     * 查找task最近的状态机节点
     * 1.仅递归查询父节点和长兄节点
     * 2.优先查找附近的，然后测试长兄节点 - 状态机作为第一个节点的情况比较常见
     */
    public static StateMachineTask<T> FindStateMachine(Task<T> task) {
        Task<T> control;
        while ((control = task.Control) != null) {
            // 父节点
            if (control is StateMachineTask<T> stateMachineTask1) {
                return stateMachineTask1;
            }
            // 长兄节点
            Task<T> eldestBrother = control.GetChild(0);
            if (eldestBrother is StateMachineTask<T> stateMachineTask2) {
                return stateMachineTask2;
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls");
    }

    /**
     * 查找task最近的状态机节点
     * 1.名字不为空的情况下，支持从兄弟节点中查询
     * 2.优先测试父节点，然后测试兄弟节点
     */
    public static StateMachineTask<T> FindStateMachine(Task<T> task, string? name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return FindStateMachine(task);
        }
        Task<T>? control;
        StateMachineTask<T>? stateMachine;
        while ((control = task.Control) != null) {
            // 父节点
            if ((stateMachine = CastAsStateMachine(control, name)) != null) {
                return stateMachine;
            }
            // 兄弟节点
            for (int i = 0, n = control.GetChildCount(); i < n; i++) {
                Task<T> brother = control.GetChild(i);
                if ((stateMachine = CastAsStateMachine(brother, name)) != null) {
                    return stateMachine;
                }
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls and brothers");
    }

    private static StateMachineTask<T>? CastAsStateMachine(Task<T> task, string name) {
        if (task is StateMachineTask<T> stateMachineTask
            && (name == stateMachineTask.name)) {
            return stateMachineTask;
        }
        return null;
    }

    #endregion

    public string? Name {
        get => name;
        set => name = value;
    }

    public int NoneChildStatus {
        get => noneChildStatus;
        set => noneChildStatus = value;
    }

    public Task<T>? InitState {
        get => initState;
        set => initState = value;
    }

    public object? InitStateProps {
        get => initStateProps;
        set => initStateProps = value;
    }

    public Task<T>? TempNextState {
        get => tempNextState;
        set => tempNextState = value;
    }

    public StateMachineListener<T>? Listener {
        get => listener;
        set => listener = value;
    }

    public StateMachineHandler<T>? StateMachineHandler {
        get => stateMachineHandler;
        set => stateMachineHandler = value;
    }
}
}