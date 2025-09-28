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

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 通过新状态的name发起状态切换，目标状态存在于配置中
/// </summary>
/// <typeparam name="T"></typeparam>
public class ChangeStateTask2<T> : LeafTask<T> where T : class
{
#nullable disable
    /** 下一个状态的name */
    private string stateName;
    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private string? machineName;
    /** 延迟模式 */
    private byte delayMode;
    /** 延迟参数 */
    private int delayArg;

    protected override void Execute() {
        int reentryId = ReentryId;
        StateMachineTask<T> stateMachine = StateMachineTask<T>.FindStateMachine(this, machineName);
        if (delayMode == 0) {
            stateMachine.ChangeState(stateName, delayArg);
        } else {
            stateMachine.ChangeState(stateName, ChangeStateArgs.PLAIN.With(delayMode, delayArg));
        }
        if (!IsExited(reentryId)) {
            SetSuccess();
        }
    }

    protected override void OnEventImpl(object eventObj) {
    }

    public string StateName {
        get => stateName;
        set => stateName = value;
    }

    public string? MachineName {
        get => machineName;
        set => machineName = value;
    }

    public byte DelayMode {
        get => delayMode;
        set => delayMode = value;
    }

    public int DelayArg {
        get => delayArg;
        set => delayArg = value;
    }
}
}