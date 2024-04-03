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
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 
/// </summary>
public class ScheduleOrderTest
{
    private static Counter counter;
    private static IEventLoop consumer;

    [SetUp]
    public void SetUp() {
        counter = new Counter();
        consumer = EventLoopBuilder.NewBuilder(new DefaultThreadFactory("global")).Build();
    }

    /// <summary>
    /// 测试initDelay相同时，任务是否按照按提交顺序执行
    /// </summary>
    [Test]
    public void TestScheduleOrder() {
        consumer.Start().Join();
        Random random = new Random();
        TimeSpan initDelay = TimeSpan.FromMilliseconds(100);
        TimeSpan period = TimeSpan.FromMilliseconds(200);

        for (int i = 0; i < 100; i++) {
            Action newTask = counter.NewTask(1, i);
            switch (random.Next(3)) {
                case 1: {
                    consumer.ScheduleWithFixedDelay(newTask, initDelay, period);
                    break;
                }
                case 2: {
                    consumer.ScheduleAtFixedRate(newTask, initDelay, period);
                    break;
                }
                default: {
                    consumer.ScheduleAction(newTask, initDelay);
                    break;
                }
            }
        }
        Thread.Sleep(3000);
        consumer.Shutdown();
        consumer.TerminationFuture.Join();

        Assert.IsTrue(counter.sequenceMap.Count > 0, "counter.sequenceMap.Count > 0");
        Assert.IsTrue(counter.errorMsgList.Count == 0, "counter.errorMsgList.Count == 0");
    }

    /// <summary>
    /// 测试execute和schedule(0)的顺序
    /// </summary>
    [Test]
    public void TestExecuteScheduleOrder() {
        consumer.Start().Join();
        Random random = new Random();
        for (int i = 0; i < 100; i++) {
            Action newTask = counter.NewTask(1, i);
            switch (random.Next(3)) {
                case 1: {
                    consumer.Execute(newTask);
                    break;
                }
                case 2: {
                    consumer.SubmitAction(newTask);
                    break;
                }
                default: {
                    consumer.ScheduleAction(newTask, TimeSpan.Zero);
                    break;
                }
            }
        }
        Thread.Sleep(3000);
        consumer.Shutdown();
        consumer.TerminationFuture.Join();

        Assert.IsTrue(counter.sequenceMap.Count > 0, "counter.sequenceMap.Count > 0");
        Assert.IsTrue(counter.errorMsgList.Count == 0, "counter.errorMsgList.Count == 0");
    }
}