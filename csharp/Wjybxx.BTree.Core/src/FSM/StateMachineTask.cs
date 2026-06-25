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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.BTree.Branch;
using Wjybxx.BTree.FSM.Handler;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 状态机节点
/// ps:以我的经验来看，状态机是最重要的节点，<see cref="Join{T}"/>则是仅次于状态机的节点 -- 不能以使用数量而定。
/// </summary>
/// <typeparam name="T"></typeparam>
public class StateMachineTask<T> : Decorator<T> where T : class
{
    /// <summary>
    /// 该FSM关联的状态，第一个状态为初始状态；
    ///
    /// 注：序列化库需要支持对象图，可以通过子树引用为对象附加属性和共享状态。
    /// </summary>
    [SerializeReference] private List<Task<T>> stateList = new();

    /** 待切换的状态，主要用于支持当前状态退出后再切换 */
    [NonSerialized] private Task<T>? tempNextState;
    /** handler也加入序列化，用于在编辑器中配置 */
    private IStateMachineHandler<T> handler = DefaultStateMachineHandler<T>.Inst;

    #region fsm基础api

    /** 获取当前状态 */
    public Task<T>? CurState => child;

    /// <summary>
    /// 撤销到前一个状态
    /// </summary>
    /// <returns>如果有前一个状态则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool UndoChangeState() {
        return UndoChangeState(ChangeStateArgs.UNDO);
    }

    /// <summary>
    /// 撤销到前一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有前一个状态则返回true</returns>
    public virtual bool UndoChangeState(ChangeStateArgs changeStateArgs) {
        return false;
    }

    /// <summary>
    /// 重新进入到下一个状态
    /// </summary>
    /// <returns>如果有下一个状态则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RedoChangeState() {
        return RedoChangeState(ChangeStateArgs.REDO);
    }

    /// <summary>
    /// 重新进入到下一个状态
    /// </summary>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <returns>如果有下一个状态则返回true</returns>
    public virtual bool RedoChangeState(ChangeStateArgs changeStateArgs) {
        return false;
    }

    /** 切换状态 -- 如果状态机处于运行中，则立即切换；当前状态会进去被取消状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ChangeState(Task<T> nextState) {
        ChangeState(nextState, ChangeStateArgs.PLAIN);
    }

    /// <summary>
    /// 切换状态 -- 如果状态机处于运行中，则立即切换
    /// </summary>
    /// <param name="nextState">要进入的下一个状态</param>
    /// <param name="curStateResult">当前状态的结果</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ChangeState(Task<T> nextState, int curStateResult) {
        ChangeState(nextState, ChangeStateArgs.PlainWithArg(curStateResult));
    }

    /// <summary>
    /// 切换状态
    /// 1.如果当前有一个待切换的状态，则会被悄悄丢弃(todo 可以增加一个通知)
    /// 2.无论何种模式，在当前状态进入完成状态时一定会触发
    /// 3.如果状态机未运行，则仅仅保存在那里，等待下次运行的时候执行。
    /// 4.关于如何避免当前状态被取消，可参考<see cref="ChangeStateTask{T}"/>
    /// </summary>
    /// <param name="nextState">要进入的下一个状态</param>
    /// <param name="changeStateArgs">状态切换参数</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ChangeState(Task<T> nextState, ChangeStateArgs changeStateArgs) {
        if (nextState == null) throw new ArgumentNullException(nameof(nextState));
        if (changeStateArgs == null) throw new ArgumentNullException(nameof(changeStateArgs));

        nextState.ControlData = changeStateArgs;
        tempNextState = nextState;

        if (IsRunning && handler.IsReady(this, child, nextState)) {
            Template_Execute(false);
        }
    }

    /** 查找状态 */
    public Task<T>? GetState(string stateName) {
        foreach (var state in stateList) {
            if (state.Name == stateName) return state;
        }
        return null;
    }

    /** 通过状态的名字发起状态切换 */
    public void ChangeState(string stateName, int curStateResult = 0) {
        Task<T> state = GetState(stateName);
        if (state == null) {
            throw new InvalidOperationException("state is absent, stateName: " + stateName);
        }
        ChangeState(state, ChangeStateArgs.PlainWithArg(curStateResult));
    }

    /** 通过状态的名字发起状态切换 */
    public void ChangeState(string stateName, ChangeStateArgs stateArgs) {
        Task<T> state = GetState(stateName);
        if (state == null) {
            throw new InvalidOperationException("state is absent, stateName: " + stateName);
        }
        ChangeState(state, stateArgs);
    }

    #endregion

    #region logic

    public override void Reset() {
        base.Reset();
        handler.Reset();
        // 所有关联状态都重置
        foreach (Task<T> state in stateList) {
            state.Reset();
        }
        if (child != null) {
            RemoveChild(0);
        }
        tempNextState = null;
    }

    protected override void BeforeEnter() {
        // base.BeforeEnter();
        handler.BeforeEnter(this);
        // 初始化为初始化状态
        if (tempNextState == null && stateList.Count > 0) {
            tempNextState = stateList[0];
        }
        if (tempNextState != null && tempNextState.ControlData == null) {
            tempNextState.ControlData = ChangeStateArgs.PLAIN;
        }
    }

    protected override void Exit() {
        tempNextState = null;
        if (child != null) {
            RemoveChild(0);
        }
        base.Exit();
    }

    protected override void Execute() {
        Task<T>? curState = this.child;
        Task<T>? nextState = this.tempNextState;
        if (nextState != null && handler.IsReady(this, curState, nextState)) {
            StopCurState(curState, (ChangeStateArgs)nextState.ControlData);

            this.tempNextState = null;
            if (curState != null) {
                SetChild(0, nextState);
            } else {
                AddChild(nextState);
            }

            BeforeChangeState(curState, nextState);
            nextState.ControlData = null; // 用户需要提前将数据填充到黑板
            Template_StartChild(nextState, true); // 启动新状态
            return;
        }
        if (curState == null) {
            return;
        }
        // 继续运行或新状态enter；在尾部才能保证安全
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, curState);
        } else if (curState.IsRunning) {
            curState.Template_Execute(true);
        } else {
            Template_StartChild(curState, true);
        }
    }

    private void StopCurState(Task<T>? curState, ChangeStateArgs changeStateArgs) {
        if (curState == null) return;
        if (changeStateArgs.delayMode == 0 && changeStateArgs.delayArg > 0) {
            curState.Stop(changeStateArgs.delayArg);
        } else {
            curState.Stop();
        }
        inlineHelper.StopInline(); // help gc
    }

    protected virtual void BeforeChangeState(Task<T>? curState, Task<T>? nextState) {
        Debug.Assert(curState != null || nextState != null);
        handler.BeforeChangeState(this, curState, nextState);
    }

    protected override void OnChildRunning(Task<T> child, bool starting) {
        inlineHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        Debug.Assert(this.child == child);
        inlineHelper.StopInline();

        // 先判断是否有下一个状态，保持和changeState调用相同的逻辑
        if (tempNextState != null) {
            Template_Execute(false);
            return;
        }
        if (handler.OnNextStateAbsent(this, child)) {
            return;
        }
        RemoveChild(0);
        BeforeChangeState(child, null);
    }

    #endregion

    #region find

    /**
     * 查找task最近的状态机节点
     * 1.仅递归查询父节点和长兄节点
     * 2.优先测试父节点，然后测试长兄节点 - 状态机作为第一个节点的情况比较常见
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
            for (int i = 0, n = control.ChildCount; i < n; i++) {
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
        if (task is StateMachineTask<T> stateMachineTask && (name == stateMachineTask.name)) {
            return stateMachineTask;
        }
        return null;
    }

    #endregion

    #region 序列化

    public List<Task<T>> StateList {
        get => stateList;
        set => stateList = value ?? new List<Task<T>>(); // null处理
    }

    public IStateMachineHandler<T> Handler {
        get => handler;
        set => handler = value ?? DefaultStateMachineHandler<T>.Inst; // null处理
    }

    #endregion
}
}