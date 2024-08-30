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
using System.Collections.Generic;
using Wjybxx.Disruptor;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 全局事件循环，用于执行一些简单的任务
/// </summary>
public sealed class GlobalEventLoop : DisruptorEventLoop<MiniAgentEvent>
{
    public static GlobalEventLoop Inst { get; } = new GlobalEventLoop(new DisruptorEventLoopBuilder<MiniAgentEvent>()
    {
        Agent = EmptyAgent<MiniAgentEvent>.Inst,
        ThreadFactory = new DefaultThreadFactory("GlobalEventLoop", true),
        EventSequencer = new MpUnboundedEventSequencer<MiniAgentEvent>.Builder(MiniAgentEvent.FACTORY) // 需要使用无界队列
            {
                WaitStrategy = new TimeoutSleepingWaitStrategy(10, 1, 10), // 等待策略需要支持超时，否则无法调度定时任务
                ChunkLength = 1024,
                MaxPooledChunks = 1
            }
            .Build()
    });

    private GlobalEventLoop(DisruptorEventLoopBuilder<MiniAgentEvent> builder)
        : base(builder) {
    }

    // TODO 其实最好返回的Future不能支持等待
    public override bool AwaitTermination(TimeSpan timeout) {
        return false;
    }

    public override void Shutdown() {
    }

    public override List<ITask> ShutdownNow() {
        return new List<ITask>();
    }
}
}