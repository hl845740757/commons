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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// C#的开发者总喜欢优化GC问题...
/// 为了减少GC管理的对象，我们继承<see cref="Promise{T}"/>，但在接口层面仍然是<see cref="IFutureTask{T,S}"/>，
/// 即接口层面我是更推荐组合的。
///
/// ps：继承Promise可能带来一些扩展性问题，复用其它库的代码会较难，但在一个特定的库中通常影响较小。
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
/// <typeparam name="S">状态机类型</typeparam>
public class PromiseTask<T, S> : Promise<T>, IFutureTask<T> where S : IAsyncStateMachine
{
    /// <summary>
    /// 关联的异步状态机
    /// </summary>
    private S _stateMachine;
    /// <summary>
    /// 驱动状态机的委托（延迟分配）
    /// </summary>
    private Action? _moveToNext;

    public PromiseTask() {
    }

    public void Run() {
        _stateMachine?.MoveNext();
    }

    /// <summary>
    /// 任务关联的调度选项
    /// </summary>
    public int Options { get; set; }

    /// <summary>
    /// 任务关联的Future
    /// </summary>
    public IPromise<T> Future => this;

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// </summary>
    public Action MoveToNext => _moveToNext ??= Run;

    /// <summary>
    /// 绑定异步状态机
    /// </summary>
    /// <param name="stateMachine"></param>
    public void SetStateMachine(ref S stateMachine) {
        this._stateMachine = stateMachine;
    }
}