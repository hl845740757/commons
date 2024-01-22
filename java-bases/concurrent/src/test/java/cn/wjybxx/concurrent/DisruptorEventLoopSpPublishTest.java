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

/**
 * 测试单生产者使用{@link DisruptorEventLoop#publish(long)}的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopSpPublishTest {

    private Counter counter;
    private DisruptorEventLoop consumer;
    private Producer producer;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        counter = null;
        consumer = null;
        producer = null;
        alert = false;
    }

    @Test
    void testRingBuffer() throws InterruptedException {
        CounterAgent agent = new CounterAgent();
        counter = agent.getCounter();

        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(RingBufferEventSequencer.<RingBufferEvent>newMultiProducer()
                        .setFactory(RingBufferEvent::new)
                        .build())
                .build();
        producer = new Producer();
        producer.start();

        ThreadUtils.sleepQuietly(5000);
        alert = true;
        producer.join();

        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    @Test
    void testUnboundedBuffer() throws InterruptedException {
        CounterAgent agent = new CounterAgent();
        counter = agent.getCounter();

        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(MpUnboundedEventSequencer.<RingBufferEvent>newBuilder()
                        .setFactory(RingBufferEvent::new)
                        .build())
                .build();

        producer = new Producer();
        producer.start();

        ThreadUtils.sleepQuietly(5000);
        alert = true;
        producer.join();

        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    private class Producer extends Thread {

        public Producer() {
            super("Producer");
        }

        @Override
        public void run() {
            DisruptorEventLoop consumer = DisruptorEventLoopSpPublishTest.this.consumer;
            long sequence = -1;
            while (!alert && sequence < 1000000) {
                sequence = consumer.nextSequence();
                if (sequence < 0) {
                    break;
                }
                try {
                    RingBufferEvent event = consumer.getEvent(sequence);
                    event.setType(1);
                    event.longVal1 = sequence;
                } finally {
                    consumer.publish(sequence);
                }
            }
        }
    }

}