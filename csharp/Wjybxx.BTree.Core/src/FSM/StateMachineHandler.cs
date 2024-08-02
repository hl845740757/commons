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
/// 状态机扩展理解处理器
/// </summary>
/// <typeparam name="T"></typeparam>
public interface StateMachineHandler<T> where T : class
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
    /// 下个状态的前置条件检查失败
    /// </summary>
    /// <param name="stateMachineTask">状态机</param>
    /// <param name="nextState">下一个状态</param>
    void OnNextStateGuardFailed(StateMachineTask<T> stateMachineTask, Task<T> nextState) {
    }

    /// <summary>
    /// 当状态机没有下一个状态时调用该方法，以避免无可用状态
    ///
    /// 注意：
    /// 1.状态机启动时不会调用该方法
    /// 2.如果该方法返回后仍无可用状态，将触发无状态逻辑
    /// 3.【不可延迟新状态】，否则将导致错误；框架难以安全检测，由用户自身保证
    /// </summary>
    /// <param name="stateMachineTask">v</param>
    /// <param name="preState">前一个状态，用于计算下一个状态</param>
    /// <returns>用户是否执行了状态切换操作</returns>
    bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState);
}
}