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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该接口表示异步状态机的驱动类。
/// <see cref="IStateMachineDriver{T}"/>和<see cref="ValueFutureTask{T}"/>/>
/// 区别在于：一个由状态机（外部）设置结果，一个由自身（内部）设置结果。
///
/// ps：在c#中，Awaiter和StateMachine其实仅仅用于封装回调；Driver是Future和回调之间的桥梁。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStateMachineDriver<T> : ITaskDriver<T>
{
    /// <summary>
    /// 返回用于驱动StateMachine的委托
    /// 
    /// ps：定义为属性以允许实现类进行一些优化，比如：缓存实例，代理。
    /// </summary>
    Action MoveToNext { get; }

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(int reentryId, T result);

    /// <summary>
    /// 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <returns></returns>
    bool TrySetException(int reentryId, Exception cause);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cancelCode">相关的取消码</param>
    /// <returns></returns>
    bool TrySetCancelled(int reentryId, int cancelCode);
}
}