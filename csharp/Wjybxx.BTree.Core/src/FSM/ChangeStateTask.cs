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

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// 切换状态任务
/// </summary>
/// <typeparam name="T"></typeparam>
public class ChangeStateTask<T> : LeafTask<T> where T : class
{
    /** 下一个状态的guid -- 延迟加载 */
    private string? nextStateGuid;
    /** 下一个状态的对象缓存，通常延迟加载以避免循环引用 */
    [NonSerialized] private Task<T>? nextState;
    /** 目标状态的属性 */
    private object? stateProps;

    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private string? machineName;
    /** 延迟模式 */
    private byte delayMode;
    /** 延迟参数 */
    private int delayArg;

    public ChangeStateTask() {
    }

    public ChangeStateTask(Task<T> nextState) {
        this.nextState = nextState;
    }

    public override void ResetForRestart() {
        base.ResetForRestart();
        if (nextState != null && nextState.Control == null) {
            nextState.ResetForRestart();
        }
    }

    protected override void Execute() {
        if (nextState == null) {
            if (string.IsNullOrEmpty(nextStateGuid)) {
                throw new IllegalStateException("guid is empty");
            }
            nextState = TaskEntry.TreeLoader.LoadRootTask<T>(nextStateGuid);
        }
        if (stateProps != null) {
            nextState.SharedProps = stateProps;
        }

        int reentryId = ReentryId;
        StateMachineTask<T> stateMachine = StateMachineTask<T>.FindStateMachine(this, machineName);
        stateMachine.ChangeState(nextState, ChangeStateArgs.PLAIN.With(delayMode, delayArg));
        if (!IsExited(reentryId)) {
            SetSuccess();
        }
    }

    protected override void OnEventImpl(object eventObj) {
    }

    public string? NextStateGuid {
        get => nextStateGuid;
        set => nextStateGuid = value;
    }

    public Task<T>? NextState {
        get => nextState;
        set => nextState = value;
    }

    public object? StateProps {
        get => stateProps;
        set => stateProps = value;
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