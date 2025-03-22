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

using System.Collections.Generic;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 事件循环线程组，它管理着一组<see cref="IEventLoop"/>。
/// 它的本质是容器，它主要负责管理持有的EventLoop的生命周期。
///
/// <h1>时序约定</h1>
/// 1.{@link EventLoopGroup}代表着一组线程，不对任务的执行时序提供任何保证，用户只能通过工具自行协调。
/// 2.{@link #execute(Runnable)}{@link #submit(Callable)}系列方法的时序等同于{@code schedule(task, 0, TimeUnit.SECONDS)}
/// 
/// Q: 为什么在接口层不提供严格的时序约定？
/// A: 如果在接口层定义了严格的时序约定，实现类就会受到限制。
/// 
/// 1.时序很重要，在提供并发组件时应该详细的说明时序约定，否则用户将无所措手足。
/// 2.EventLoopGroup也可以有自己的线程 - 一种常见的情况是Group是一个监控线程。
/// </summary>
public interface IEventLoopGroup : IScheduledExecutorService, IEnumerable<IEventLoop>
{
    /// <summary>
    /// 选择一个<see cref="IEventLoop"/>用于接下来的任务调度
    /// </summary>
    /// <returns></returns>
    IEventLoop Select();
}
}