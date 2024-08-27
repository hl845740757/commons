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
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// 
/// PS：
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口。
/// 2.用户在获取结果时触发回收。
/// 3.该实现并不是严格线程安全的，用在非StateMachine场景可能导致以错误。
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
/// <typeparam name="S">状态机类型</typeparam>
internal sealed class ValueFutureStateMachineTask<T, S> : ValuePromise<T>, IValueFutureStateMachineTask<T> where S : IAsyncStateMachine
{
    private static readonly ConcurrentObjectPool<ValueFutureStateMachineTask<T, S>> POOL =
        new(() => new ValueFutureStateMachineTask<T, S>(), driver => driver.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolConfig.TaskType.ValueFutureStateMachineDriver));

    /// <summary>
    /// 任务状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 驱动状态机的委托
    /// </summary>
    private readonly Action _moveToNext;

    private ValueFutureStateMachineTask() {
        _moveToNext = Run;
    }

    public static void SetStateMachine(ref S stateMachine, out IValueFutureStateMachineTask<T> task, out int reentryId) {
        ValueFutureStateMachineTask<T, S> result = POOL.Acquire();

        // driver和reentryId是builder的属性，而builder是状态机的属性，需要在拷贝状态机之前完成初始化
        // init builder before copy state machine
        task = result;
        reentryId = result.IncReentryId(); // 重用时也+1

        // copy struct... 从栈拷贝到堆，此后栈上的状态机将被丢弃
        result._stateMachine = stateMachine;
    }

    private void Run() {
        _stateMachine.MoveNext();
    }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext;

    public override void Reset() {
        base.Reset();
        _stateMachine = default;
    }

    protected override void PrepareToRecycle() {
        POOL.Release(this);
    }
}
}