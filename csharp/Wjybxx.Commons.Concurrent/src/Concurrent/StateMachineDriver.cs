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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// C#的开发者总喜欢优化GC问题...
/// 为了减少GC管理的对象，我们继承<see cref="Promise{T}"/>；继承Promise可能带来一些扩展性问题，但StateMachineTask通常是无需扩展的，因此影响较小。
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
/// <typeparam name="S">状态机类型</typeparam>
internal sealed class StateMachineDriver<T, S> : Promise<T>, IStateMachineDriver<T> where S : IAsyncStateMachine
{
    /// <summary>
    /// 任务状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 驱动状态机的委托（延迟分配）
    /// </summary>
    private Action _moveToNext;

    public StateMachineDriver(IExecutor? executor) : base(executor) {
        _moveToNext = Run;
    }
    
    public static void SetStateMachine(IExecutor? executor, ref S stateMachine, ref IStateMachineDriver<T> driver) {
        StateMachineDriver<T,S> result = new StateMachineDriver<T, S>(executor);
        driver = result; // set driver before copy
        result._stateMachine = stateMachine; // copy struct... 从栈拷贝到堆，ref也没用，不错一次不知道...
    }

    public void Run() {
        _stateMachine.MoveNext();
    }

    /// <summary>
    /// 任务关联的Future
    /// </summary>
    public IPromise<T> Promise => this;

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext;
}