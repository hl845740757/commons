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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 测试EventLoop下await系统库的Task的问题
/// </summary>
public class TaskAwaitTest
{
    [Test]
    public void AwaitInEventLoop() {
        AwaitInEventLoopImpl().Join();
    }

    private async IFuture AwaitInEventLoopImpl() {
        await GlobalEventLoop.Inst;
        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public void AwaitInOtherEventLoop() {
        DisruptorEventLoop<MiniAgentEvent> netEventLoop = new DisruptorEventLoopBuilder<MiniAgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("GlobalEventLoop", true),
            EventSequencer = new MpUnboundedEventSequencer<MiniAgentEvent>.Builder(() => new MiniAgentEvent())
                {
                    WaitStrategy = new TimeoutSleepingWaitStrategy(10, 1, 10),
                    ChunkLength = 1024,
                    MaxPooledChunks = 1
                }
                .Build()
        }.Build();

        AwaitInOtherEventLoopImpl(netEventLoop).Join();
        netEventLoop.Shutdown();
    }

    private async IFuture AwaitInOtherEventLoopImpl(DisruptorEventLoop<MiniAgentEvent> netEventLoop) {
        await GlobalEventLoop.Inst;

        TaskCompletionSource cts = new TaskCompletionSource();
        netEventLoop.Execute(() => {
            cts.TrySetResult();
        });

        await cts.Task;
    }
}