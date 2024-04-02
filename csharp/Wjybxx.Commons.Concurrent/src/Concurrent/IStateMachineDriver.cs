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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 该接口表示异步状态机的驱动类。
///
/// ps：在c#中，Awaiter和StateMachine其实仅仅用于封装回调；Driver是Future和回调之间的桥梁。
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IStateMachineDriver<T>
{
    /// <summary>
    /// 异步任务关联的Promise
    ///
    /// ps:暂时先不考虑复用对象问题，未来再考虑优化。
    /// </summary>
    IPromise<T> Promise { get; }

    /// <summary>
    /// 用于驱动StateMachine
    /// 
    /// ps：
    /// 1. 定义为属性以允许实现类进行一些优化，比如：缓存实例，代理。
    /// 2. 通常应该是Run方法的委托。
    /// </summary>
    Action MoveToNext { get; }
}