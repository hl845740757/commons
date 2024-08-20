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
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

public class ScheduleTest2
{
    private static readonly List<string> stringList = new List<string>() { "hello", "world", "a", "b", "c" };
    private static readonly string expectedString = string.Join(",", stringList);

    private static IEventLoop consumer;
    private static List<string> joiner;
    private static int index = 0;


    [SetUp]
    public void SetUp() {
        consumer = new DisruptorEventLoopBuilder<MiniAgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("Scheduler", true),
            EventSequencer = new RingBufferEventSequencer<MiniAgentEvent>.Builder(() => new MiniAgentEvent())
                .Build()
        }.Build();
        consumer.Start().Join();

        joiner = new List<string>(6);
        index = 0;
    }

    [TearDown]
    public void tearDown() {
        consumer.Shutdown();
        consumer.TerminationFuture.Join();
    }

    private bool timeSharingJoinString([NotNullWhen(true)] out string? result) {
        joiner.Add(stringList[index++]);
        if (index >= stringList.Count) {
            result = string.Join(",", joiner);
            return true;
        }
        result = null;
        return false;
    }

    public string untilJoinStringSuccess() {
        string result;
        while (!timeSharingJoinString(out result)) {
        }
        return result;
    }

    [Test]
    public void testOnlyOnceFail() {
        ScheduledTaskBuilder<string> builder =
            ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => timeSharingJoinString(out r));
        builder.SetOnlyOnce(0);

        IScheduledFuture<string> future = consumer.Schedule(in builder);
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.ExceptionNow() is TimeoutException);
    }

    [Test]
    public void testOnlyOnceSuccess() {
        ScheduledTaskBuilder<string> builder = ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => {
            r = untilJoinStringSuccess();
            return true;
        });
        builder.SetOnlyOnce(0);

        string result = consumer.Schedule(in builder).Join();
        Assert.AreEqual(expectedString, result);
    }

    [Test]
    public void testTimeSharingComplete() {
        ScheduledTaskBuilder<string> builder =
            ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => timeSharingJoinString(out r));
        builder.SetFixedDelay(0, 200);

        string result = consumer.Schedule(in builder).Join();
        Assert.AreEqual(expectedString, result);
    }

    #region timeout

    [Test]
    public void testRunnableTimeout() {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(() => { });
        builder.SetFixedDelay(0, 200);
        builder.SetTimeoutByCount(1);

        IScheduledFuture<int> future = consumer.Schedule(in builder);
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.ExceptionNow() is StacklessTimeoutException);
    }


    [Test]
    public void testTimeSharingTimeout() {
        ScheduledTaskBuilder<string> builder =
            ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => timeSharingJoinString(out r));
        builder.SetFixedDelay(0, 200);
        builder.SetTimeoutByCount(1);

        IScheduledFuture<string> future = consumer.Schedule(in builder);
        future.AwaitUninterruptibly(TimeSpan.FromMilliseconds(300));
        Assert.IsTrue(future.ExceptionNow() is StacklessTimeoutException);
    }

    #endregion

    #region count-limit

    [Test]
    public void testTimeSharingCountLimitSuccess() {
        long startTime = ObjectUtil.SystemTickMillis();
        ScheduledTaskBuilder<string> builder =
            ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => timeSharingJoinString(out r));
        builder.SetFixedDelay(10, 10);
        builder.CountLimit = (stringList.Count);

        IScheduledFuture<string> future = consumer.Schedule(in builder);
        future.AwaitUninterruptibly();
        Console.WriteLine("costTime: " + (ObjectUtil.SystemTickMillis() - startTime));

        Assert.AreEqual(expectedString, future.ResultNow());
    }

    [Test]
    public void testTimeSharingCountLimitFail() {
        long startTime = ObjectUtil.SystemTickMillis();
        ScheduledTaskBuilder<string> builder =
            ScheduledTaskBuilder.NewTimeSharing<string>((IContext ctx, bool firstStep, out string r) => timeSharingJoinString(out r));
        builder.SetFixedDelay(10, 10);
        builder.CountLimit = (stringList.Count - 1);

        IScheduledFuture<string> future = consumer.Schedule(in builder);
        future.AwaitUninterruptibly();
        Console.WriteLine("costTime: " + (ObjectUtil.SystemTickMillis() - startTime));

        Assert.IsTrue(future.ExceptionNow() == StacklessTimeoutException.INST_COUNT_LIMIT);
    }

    #endregion
}