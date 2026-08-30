#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BTree.FSM.Handler
{
public class RedoStateMachineHandler<T> : IStateMachineHandler<T> where T : class
{
    public static readonly RedoStateMachineHandler<T> Inst = new RedoStateMachineHandler<T>();

    public void Reset() {
    }

    public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
    }

    public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
    }

    //-----------------------

    public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
        if (stateMachineTask.RedoChangeState()) {
            return true;
        }
        stateMachineTask.SetCompleted(preState.Status, true);
        return true;
    }
}
}