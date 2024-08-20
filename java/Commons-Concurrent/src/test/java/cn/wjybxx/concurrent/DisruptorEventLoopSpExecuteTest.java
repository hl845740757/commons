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

import java.util.concurrent.Executor;

/**
 * 测试单生产者使用{@link Executor#execute(Runnable)}提交任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopSpExecuteTest {

    private static CounterAgent agent;
    private static Counter counter;
    private static EventLoop consumer;
    private static Producer producer;
    private static volatile boolean alert;

    @BeforeEach
    void setUp() {
        agent = new CounterAgent();
        counter = agent.getCounter();
        consumer = null;
        producer = null;
        alert = false;
    }

    @Test
    void testRingBuffer() throws InterruptedException {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();
        producer = new Producer(1);
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
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setEventSequencer(MpUnboundedEventSequencer
                        .newBuilder(RingBufferEvent::new)
                        .build())
                .build();
        producer = new Producer(1);
        producer.start();

        ThreadUtils.sleepQuietly(5000);
        alert = true;
        producer.join();

        consumer.shutdown();
        consumer.terminationFuture().join();

        Assertions.assertTrue(counter.getSequenceMap().size() > 0, "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    private static class Producer extends Thread {

        final int type;

        public Producer(int type) {
            super("Producer-" + type);
            this.type = type;
        }

        @Override
        public void run() {
            EventLoop consumer = DisruptorEventLoopSpExecuteTest.consumer;
            long sequencer = 0;
            while (!alert && sequencer < 1000000) {
                consumer.execute(counter.newTask(type, sequencer++));
            }
        }
    }

}