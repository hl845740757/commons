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
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

public class ValueFutureTaskTest
{
    private static Counter counter;
    private static IEventLoop consumer;

    [SetUp]
    public void SetUp() {
        counter = new Counter();
        consumer = new DisruptorEventLoopBuilder<MiniAgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("Scheduler", true),
            EventSequencer = new RingBufferEventSequencer<MiniAgentEvent>.Builder(() => new MiniAgentEvent())
                .Build()
        }.Build();
    }

    /// <summary>
    /// 测试execute和schedule(0)的顺序
    /// </summary>
    [Test]
    public void TestRun() {
        consumer.Start().Join();

        for (int idx = 0; idx < 100; idx++) {
            CountAsync(idx);
        }

        Thread.Sleep(3000);
        consumer.Shutdown();
        consumer.TerminationFuture.Join();

        Assert.IsTrue(counter.sequenceMap.Count > 0, "counter.sequenceMap.Count > 0");
        Assert.IsTrue(counter.errorMsgList.Count == 0, "counter.errorMsgList.Count == 0");
    }

    private static async void CountAsync(int idx) {
        Action newTask = counter.NewTask(1, idx);
        ValueFuture future = ValueFutureTask.Run(consumer, newTask);
        if (MathCommon.IsEven(idx)) {
            await future; // await会导致线程切换...单元测试下会切换到NUnit的线程
        } else {
            await future.GetAwaitable(consumer, TaskOptions.STAGE_TRY_INLINE);
            Assert.IsTrue(consumer.InEventLoop());
        }
        if (idx == 0) {
            // 重复await将抛出异常
            Assert.CatchAsync<IllegalStateException>(async () => await future);
        }
    }
}