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

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 状态机扩展处理器
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStateMachineHandler<T> where T : class
{
    /// <summary>
    ///  handler可能也有需要重置的数据
    /// </summary>
    /// <param name="stateMachineTask"></param>
    void ResetForRestart(StateMachineTask<T> stateMachineTask) {
    }

    /// <summary>
    /// handler可能也有需要初始化的数据
    /// </summary>
    /// <param name="stateMachineTask"></param>
    void BeforeEnter(StateMachineTask<T> stateMachineTask) {
    }

    /// <summary>
    /// 是否可以切换到下一个状态
    /// </summary>
    /// <param name="stateMachineTask">状态机</param>
    /// <param name="curState">当前状态</param>
    /// <param name="nextState">要切换的状态</param>
    /// <returns></returns>
    bool IsReady(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T> nextState) {
        if (curState == null || curState.IsCompleted) {
            return true;
        }
        ChangeStateArgs changeStateArgs = (ChangeStateArgs)nextState.ControlData;
        return changeStateArgs.delayMode == ChangeStateArgs.DELAY_NONE;
    }

    /// <summary>
    /// 该方法在进入新状态前调用
    /// 
    /// 1.两个参数最多一个为null
    /// 2.可以设置新状态的黑板和其它数据
    /// 3.用户此时可为新状态分配上下文(黑板、取消令牌、共享属性)；同时清理前一个状态的上下文
    /// 4.用户此时可拿到新状态的状态切换参数<see cref="Task{T}.ControlData"/>，后续则不可
    /// 5.如果task需要感知redo和undo，则由用户将信息写入黑板
    /// </summary>
    /// <param name="stateMachineTask">状态机</param>
    /// <param name="curState">当前状态</param>
    /// <param name="nextState">下一个状态</param>
    void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
    }

    /// <summary>
    /// 当状态机没有下一个状态时调用该方法，以避免无可用状态
    ///
    /// 注意：
    /// 1.状态机启动时不会调用该方法
    /// 2.如果该方法返回后仍无可用状态，将触发无状态逻辑
    /// </summary>
    /// <param name="stateMachineTask">状态机</param>
    /// <param name="preState">前一个状态，用于计算下一个状态</param>
    /// <returns>用户是否执行了【状态切换】或【停止状态机】</returns>
    bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
        stateMachineTask.SetCompleted(preState.Status, true);
        return true;
    }
}
}