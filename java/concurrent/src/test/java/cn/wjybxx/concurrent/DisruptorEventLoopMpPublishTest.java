/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.concurrent;

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.disruptor.MpUnboundedEventSequencer;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;

/**
 * 测试多生产者使用{@link DisruptorEventLoop#publish(long)}发布任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopMpPublishTest {

    private static final int PRODUCER_COUNT = 4;

    private Counter counter;
    private DisruptorEventLoop<RingBufferEvent> consumer;
    private List<Producer> producerList;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        counter = null;
        consumer = null;
        producerList = null;
        alert = false;
    }

    @Test
    void testRingBuffer() throws InterruptedException {
        CounterAgent agent = new CounterAgent();
        counter = agent.getCounter();

        consumer = EventLoopBuilder.<RingBufferEvent>newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();

        producerList = new ArrayList<>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            producerList.add(new Producer(i));
        }
        producerList.forEach(Thread::start);

        ThreadUtils.sleepQuietly(5000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        alert = true;
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    @Test
    void testUnboundedBuffer() throws InterruptedException {
        CounterAgent agent = new CounterAgent();
        counter = agent.getCounter();

        consumer = EventLoopBuilder.<RingBufferEvent>newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(MpUnboundedEventSequencer
                        .newBuilder(RingBufferEvent::new)
                        .build())
                .build();

        producerList = new ArrayList<>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            producerList.add(new Producer(i));
        }
        producerList.forEach(Thread::start);

        ThreadUtils.sleepQuietly(5000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        alert = true;
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    private class Producer extends Thread {

        private final int type;

        public Producer(int type) {
            super("Producer-" + type);
            this.type = type;
            if (type <= 0) { // 0是系统任务
                throw new IllegalArgumentException();
            }
        }

        @Override
        public void run() {
            DisruptorEventLoop<RingBufferEvent> consumer = DisruptorEventLoopMpPublishTest.this.consumer;
            long localSequence = 0;
            while (!alert && localSequence < 1000000) {
                long sequence = consumer.nextSequence();
                if (sequence < 0) {
                    break;
                }
                try {
                    RingBufferEvent event = consumer.getEvent(sequence);
                    event.setType(type);
                    event.longVal1 = localSequence++;
                } finally {
                    consumer.publish(sequence);
                }
            }
        }
    }

}