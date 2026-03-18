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

using System;

namespace Wjybxx.BTree.FSM.Handler
{
/// <summary>
/// 默认不考虑委托的序列化，因此该Wrapper也不你序列化
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListenerWrapper<T> : IStateMachineHandler<T> where T : class
{
    private readonly StateMachineListener<T> _listener;

    public ListenerWrapper(StateMachineListener<T> listener) {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
    }

    public void Reset() {
    }

    public void BeforeEnter(StateMachineTask<T> stateMachineTask) {
    }

    public void BeforeChangeState(StateMachineTask<T> stateMachineTask, Task<T>? curState, Task<T>? nextState) {
        _listener.Invoke(stateMachineTask, curState, nextState);
    }
}
}