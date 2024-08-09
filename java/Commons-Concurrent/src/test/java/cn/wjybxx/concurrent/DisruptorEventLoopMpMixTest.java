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
import java.util.concurrent.RejectedExecutionException;

/**
 * 测试多生产者使用{@link DisruptorEventLoop#publish(long)}和{@link DisruptorEventLoop#execute(Runnable)}混合发布任务的时序
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class DisruptorEventLoopMpMixTest {

    private static final int PRODUCER_COUNT = 4;

    private CounterAgent agent;
    private Counter counter;
    private DisruptorEventLoop<RingBufferEvent> consumer;
    private List<Thread> producerList;
    private volatile boolean alert;

    @BeforeEach
    void setUp() {
        agent = new CounterAgent();
        counter = agent.getCounter();
        consumer = null;
        producerList = null;
        alert = false;
    }

    @Test
    void testRingBuffer() throws InterruptedException {
        consumer = EventLoopBuilder.<RingBufferEvent>newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();

        // 注意：用户事件从1开始
        producerList = new ArrayList<>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            if (i > PRODUCER_COUNT / 2) {
                producerList.add(new Producer2(i));
            } else {
                if (i == 1) {
                    producerList.add(new Producer3(i));
                } else {
                    producerList.add(new Producer(i));
                }
            }
        }
        producerList.forEach(Thread::start);

        ThreadUtils.sleepQuietly(5000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        alert = true;
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Assertions.assertEquals(PRODUCER_COUNT, counter.getSequenceMap().size(), "Counter.sequenceMap.size != PRODUCER_COUNT");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    @Test
    void testUnboundedBuffer() throws InterruptedException {
        consumer = EventLoopBuilder.<RingBufferEvent>newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(agent)
                .setEventSequencer(MpUnboundedEventSequencer
                        .newBuilder(RingBufferEvent::new)
                        .build())
                .build();

        producerList = new ArrayList<>(PRODUCER_COUNT);
        for (int i = 1; i <= PRODUCER_COUNT; i++) {
            if (i > PRODUCER_COUNT / 2) {
                producerList.add(new Producer2(i));
            } else {
                if (i == 1) {
                    producerList.add(new Producer3(i));
                } else {
                    producerList.add(new Producer(i));
                }
            }
        }
        producerList.forEach(Thread::start);

        ThreadUtils.sleepQuietly(5000);
        consumer.shutdown();
        consumer.terminationFuture().join();

        alert = true;
        producerList.forEach(ThreadUtils::joinUninterruptedly);

        Assertions.assertEquals(PRODUCER_COUNT, counter.getSequenceMap().size(), "Counter.sequenceMap.size != PRODUCER_COUNT");
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
            DisruptorEventLoop<RingBufferEvent> consumer = DisruptorEventLoopMpMixTest.this.consumer;
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

    private class Producer2 extends Thread {

        private final int type;

        public Producer2(int type) {
            super("Producer-" + type);
            this.type = type;
            if (type <= 0) { // 0是系统任务
                throw new IllegalArgumentException();
            }
        }

        @Override
        public void run() {
            DisruptorEventLoop<RingBufferEvent> consumer = DisruptorEventLoopMpMixTest.this.consumer;
            long localSequence = 0;
            while (!alert && localSequence < 1000000) {
                try {
                    consumer.execute(counter.newTask(type, localSequence++));
                } catch (RejectedExecutionException ignore) {
                    break;
                }
            }
        }
    }

    private class Producer3 extends Thread {

        private final int type;

        public Producer3(int type) {
            super("Producer-" + type);
            this.type = type;
            if (type <= 0) { // 0是系统任务
                throw new IllegalArgumentException();
            }
        }

        @Override
        public void run() {
            DisruptorEventLoop<RingBufferEvent> consumer = DisruptorEventLoopMpMixTest.this.consumer;
            long localSequence = 0;
            while (!alert && localSequence < 1000000) {
                int batchSize = 10;
                long hi = consumer.nextSequence(batchSize);
                if (hi < 0) {
                    break;
                }
                long low = hi - batchSize + 1;
                for (long sequence = low; sequence <= hi; sequence++) {
                    RingBufferEvent event = consumer.getEvent(sequence);
                    event.setType(type);
                    event.longVal1 = localSequence++;
                }
                consumer.publish(low, hi);
            }
        }
    }
}