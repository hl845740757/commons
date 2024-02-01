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
/// 
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
/// <typeparam name="S">状态机的类型</typeparam>
public interface IRunnableFutureTask<T, S> : IFutureTask<T> where S : IAsyncStateMachine
{
    
    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// 
    /// ps：定义为属性以允许实现类进行一些优化，比如：插入代理，缓存实例。
    /// </summary>
    Action MoveToNext { get; }

    /// <summary>
    /// 设置关联的异步状态机
    /// 
    /// PS：<see cref="ITask.Run"/>用于驱动状态机前进
    /// </summary>
    /// <param name="stateMachine"></param>
    void SetStateMachine(ref S stateMachine);
}