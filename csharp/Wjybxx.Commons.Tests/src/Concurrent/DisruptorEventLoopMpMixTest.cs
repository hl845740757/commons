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
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// Disruptor事件循环，混合模式测试
/// <see cref="DisruptorEventLoop{T}.Execute(System.Action,int)"/>
/// <see cref="DisruptorEventLoop{T}.Publish(long, long)"/>
/// </summary>
public class DisruptorEventLoopMpMixTest
{
    private const int PRODUCER_COUNT = 8;

    private static CounterAgent agent;
    private static Counter counter;
    private static DisruptorEventLoop<CounterEvent> consumer;
    private static IList<Thread> producerList;
    private static volatile bool alert;

    [SetUp]
    public void SetUp() {
        agent = new CounterAgent();
        counter = agent.Counter;
        consumer = null!;
        producerList = null!;
        alert = false;
    }

    [Test]
    public void TestRingBuffer() {
        consumer = new DisruptorEventLoopBuilder<CounterEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("consumer"),
            EventSequencer = new RingBufferEventSequencer<CounterEvent>.Builder(() => new CounterEvent()).Build(),
            Agent = agent
        }.Build();

        // 注意：用户事件从1开始
        producerList = new List<Thread>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            int type = i;
            if (i > PRODUCER_COUNT / 2) {
                producerList.Add(new Thread(() => ProducerLoop2(type)));
            } else {
                if (i == 1) {
                    producerList.Add(new Thread(() => ProducerLoop3(type)));
                } else {
                    producerList.Add(new Thread(() => ProducerLoop1(type)));
                }
            }
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

        Assert.That(counter.sequenceMap.Count, Is.EqualTo(PRODUCER_COUNT), "counter.sequenceMap.Count != PRODUCER_COUNT");
        Assert.IsTrue(counter.errorMsgList.Count == 0, CollectionUtil.ToString(counter.errorMsgList));
    }

    [Test]
    public void TestUnboundedBuffer() {
        consumer = new DisruptorEventLoopBuilder<CounterEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("consumer"),
            EventSequencer = new MpUnboundedEventSequencer<CounterEvent>.Builder(() => new CounterEvent()).Build(),
            Agent = agent
        }.Build();

        // 注意：用户事件从1开始
        producerList = new List<Thread>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            int type = i;
            if (i > PRODUCER_COUNT / 2) {
                producerList.Add(new Thread(() => ProducerLoop2(type)));
            } else {
                if (i == 1) {
                    producerList.Add(new Thread(() => ProducerLoop3(type)));
                } else {
                    producerList.Add(new Thread(() => ProducerLoop1(type)));
                }
            }
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

        Assert.That(counter.sequenceMap.Count, Is.EqualTo(PRODUCER_COUNT), "counter.sequenceMap.Count != PRODUCER_COUNT");
        Assert.IsTrue(counter.errorMsgList.Count == 0, CollectionUtil.ToString(counter.errorMsgList));
    }

    /** 单个申请和发布 */
    private static void ProducerLoop1(int type) {
        DisruptorEventLoop<CounterEvent> consumer = DisruptorEventLoopMpMixTest.consumer;
        long localSequence = 0;
        while (!alert && localSequence < 1000000) {
            long? sequence = consumer.NextSequence();
            if (sequence == null) {
                break;
            }
            try {
                CounterEvent agentEvent = new CounterEvent(type);
                agentEvent.longVal1 = localSequence++;
                consumer.SetEvent(sequence.Value, agentEvent);
            }
            finally {
                consumer.Publish(sequence.Value);
            }
        }
    }

    private static void ProducerLoop2(int type) {
        DisruptorEventLoop<CounterEvent> consumer = DisruptorEventLoopMpMixTest.consumer;
        long localSequence = 0;
        while (!alert && localSequence < 1000000) {
            try {
                consumer.Execute(counter.NewTask(type, localSequence++));
            }
            catch (RejectedExecutionException) {
                break;
            }
        }
    }

    /** 批量申请和发布 */
    private static void ProducerLoop3(int type) {
        DisruptorEventLoop<CounterEvent> consumer = DisruptorEventLoopMpMixTest.consumer;
        long localSequence = 0;
        while (!alert && localSequence < 1000000) {
            int batchSize = 10;
            long? hi = consumer.NextSequence(batchSize);
            if (hi == null) {
                break;
            }
            long low = hi.Value - batchSize + 1;
            try {
                for (long sequence = low; sequence <= hi.Value; sequence++) {
                    CounterEvent agentEvent = new CounterEvent(type);
                    agentEvent.longVal1 = localSequence++;
                    consumer.SetEvent(sequence, agentEvent);
                }
            }
            finally {
                consumer.Publish(low, hi.Value);
            }
        }
    }
}