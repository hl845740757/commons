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
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 测试能否通过<see cref="ICancelToken"/>取消任务
/// </summary>
public class ScheduleCancelTest
{
    private static IEventLoop consumer;

    [SetUp]
    public void SetUp() {
        consumer = new DisruptorEventLoopBuilder<MiniAgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("Scheduler", true),
            EventSequencer = new RingBufferEventSequencer<MiniAgentEvent>.Builder(MiniAgentEvent.FACTORY)
                .Build()
        }.Build();
        consumer.Start().Join();
    }

    [Test]
    public void testCancel() {
        CancelTokenSource cts = new CancelTokenSource();
        IScheduledFuture future = consumer.ScheduleAction(() =>{}, TimeSpan.FromMilliseconds(1000), cts);

        cts.Cancel(1);
        future.AwaitUninterruptibly();
        Assert.IsTrue(future.IsCancelled);
    }
}