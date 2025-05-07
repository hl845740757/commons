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
using Wjybxx.Commons.Ex;
using Wjybxx.Commons.Mutable;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 测试能否通过<see cref="ICancelToken"/>取消任务
/// </summary>
public class ScheduleCancelTest
{
    private static IEventLoop consumer;

    [OneTimeSetUp]
    public void SetUp() {
        consumer = new DisruptorEventLoopBuilder<AgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("Scheduler", true),
            EventSequencer = new RingBufferEventSequencer<AgentEvent>.Builder(AgentEvent.FACTORY)
                .Build()
        }.Build();
        consumer.Start().Join();
    }

    [OneTimeTearDown]
    public void TearDown() {
        consumer.ShutdownNow();
        consumer.TerminationFuture.Join();
    }
    
    [Test]
    public void testCancel() {
        CancelTokenSource cts = new CancelTokenSource();
        IFuture future = consumer.ScheduleAction(() => { }, TimeSpan.FromMilliseconds(1000), cts).AsFuture();

        cts.Cancel(1);
        future.AwaitUninterruptibly();
        Assert.IsTrue(future.IsCancelled);
    }
    
    [Test]
    public void testTimeout() {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(() => { });
        builder.SetFixedDelay(0, 200);
        builder.SetTimeoutByCount(1);

        IFuture<int> future = consumer.Schedule(in builder).AsFuture();
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.ExceptionNow(false) is BetterCancellationException);
    }
    
    [Test]
    public void testCountLimit() {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(() => { });
        builder.SetFixedDelay(0, 200);
        builder.SetTimeoutByCount(1);

        IFuture<int> future = consumer.Schedule(in builder).AsFuture();
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.ExceptionNow(false) is BetterCancellationException);
    }
    
    
    [Test]
    public void testErrorCode() {
        MutableInt counter = new MutableInt(0);
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(() => {
            if (counter.IncrementAndGet() > 5) {
                throw new TaskResultException(null);
            }
        });
        builder.SetFixedDelay(0, 10);

        IFuture<int> future = consumer.Schedule(in builder).AsFuture();
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.IsSucceeded);
    }
}