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
using Wjybxx.Disruptor;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 事件循环工厂
/// </summary>
public interface IEventLoopFactory
{
    /// <summary>
    /// 创建一个子事件循环
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="index">child索引</param>
    /// <param name="extraInfo">额外数据</param>
    /// <returns></returns>
    IEventLoop NewChild(IEventLoopGroup? parent, int index, object? extraInfo = null);
}

/// <summary>
/// 默认事件循环工厂实现
/// </summary>
public class EventLoopFactory : IEventLoopFactory
{
    private readonly ThreadFactory threadFactory;

    public EventLoopFactory(ThreadFactory threadFactory) {
        this.threadFactory = threadFactory ?? throw new ArgumentNullException(nameof(threadFactory));
    }

    public IEventLoop NewChild(IEventLoopGroup parent, int index, object? extraInfo = null) {
        return new DisruptorEventLoopBuilder<MiniAgentEvent>()
        {
            Parent = parent,
            Index = index,
            ThreadFactory = threadFactory,
            EventSequencer = new MpUnboundedEventSequencer<MiniAgentEvent>.Builder(() => new MiniAgentEvent())
            {
                ChunkLength = 1024,
                MaxPooledChunks = 4,
            }.Build()
        }.Build();
    }
}
}