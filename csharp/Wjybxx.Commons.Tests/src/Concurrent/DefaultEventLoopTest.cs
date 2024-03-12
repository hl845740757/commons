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

using System.Diagnostics;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 测试多线程提交任务，执行时序是否正确
/// </summary>
public class DefaultEventLoopTest
{
    private const int PRODUCER_COUNT = 4;

    private static Counter counter;
    private static IEventLoop consumer;
    private static IList<Thread> producerList;
    private static volatile bool alert;

    [SetUp]
    public void SetUp() {
        counter = null!;
        consumer = null!;
        producerList = null!;
        alert = false;
    }

    [Test]
    public void Test() {
        counter = new Counter();
        consumer = EventLoopBuilder.NewBuilder(new DefaultThreadFactory("consumer")).Build();

        producerList = new List<Thread>(PRODUCER_COUNT);
        for (int i = 0; i < PRODUCER_COUNT; i++) {
            int type = i + 1;
            producerList.Add(new Thread(() => ProducerLoop(type)));
        }
        foreach (Thread thread in producerList) {
            thread.Start();
        }

        Thread.Sleep(5000);
        consumer.Shutdown();
        alert = true;

        consumer.TerminationFuture.Join();
        foreach (Thread thread in producerList) {
            thread.Join();
        }
        
        Assert.IsTrue(counter.sequenceMap.Count > 0, "counter.sequenceMap.Count == 0");
        Assert.IsTrue(counter.errorMsgList.Count == 0, CollectionUtil.ToString(counter.errorMsgList));
    }

    private static void ProducerLoop(int type) {
        long localSequence = 0;
        while (!alert && localSequence < 100_0000) {
            try {
                consumer.Execute(counter.NewTask(type, localSequence++));
            }
            catch (RejectedExecutionException) {
                Debug.Assert(alert);
                break;
            }
        }
    }
}