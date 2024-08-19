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

import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;
import java.util.StringJoiner;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 * 测试{@link ScheduledTaskBuilder}
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleTest2 {

    private final List<String> stringList = List.of("hello", "world", "a", "b", "c");

    private EventLoop consumer;
    private StringJoiner joiner;
    private int index = 0;

    private String expectedString;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();
        consumer.start().join();

        joiner = new StringJoiner(",");

        StringJoiner tempJoiner = new StringJoiner(",");
        stringList.forEach(tempJoiner::add);
        expectedString = tempJoiner.toString();
    }

    @AfterEach
    void tearDown() {
        consumer.shutdown();
        consumer.terminationFuture().join();
    }

    ResultHolder<String> timeSharingJoinString() {
        joiner.add(stringList.get(index++));
        if (index >= stringList.size()) {
            return ResultHolder.success(joiner.toString());
        }
        return null;
    }

    ResultHolder<String> untilJoinStringSuccess() {
        ResultHolder<String> r;
        while ((r = timeSharingJoinString()) == null) {
        }
        return r;
    }

    @Test
    void testOnlyOnceFail() {
        IScheduledFuture<String> future = consumer.schedule(ScheduledTaskBuilder.newTimeSharing((ctx, firstStep) -> timeSharingJoinString())
                .setOnlyOnce(0));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow() instanceof TimeoutException);
    }

    @Test
    void testOnlyOnceSuccess() {
        String result = consumer.schedule(ScheduledTaskBuilder.newTimeSharing((ctx, firstStep) -> untilJoinStringSuccess())
                        .setOnlyOnce(0))
                .join();

        Assertions.assertEquals(expectedString, result);
    }

    @Test
    void testTimeSharingComplete() {
        String result = consumer.schedule(ScheduledTaskBuilder.newTimeSharing((ctx, firstStep) -> timeSharingJoinString())
                        .setFixedDelay(0, 200))
                .join();

        Assertions.assertEquals(expectedString, result);
    }

    // region timeout

    @Test
    void testRunnableTimeout() {
        IScheduledFuture<?> future = consumer.schedule(ScheduledTaskBuilder.newAction(() -> {})
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow() instanceof StacklessTimeoutException);

        consumer.submit(() -> {
            System.out.println();
        });
    }

    @Test
    void testCallableTimeout() {
        IScheduledFuture<?> future = consumer.schedule(ScheduledTaskBuilder.newFunc(() -> "hello world")
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow() instanceof StacklessTimeoutException);
    }

    @Test
    void testTimeSharingTimeout() {
        IScheduledFuture<String> future = consumer.schedule(ScheduledTaskBuilder.newTimeSharing((ctx, firstStep) -> timeSharingJoinString())
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow() instanceof StacklessTimeoutException);
    }

    // endregion

    // region count-limit

    @Test
    void testTimeSharingCountLimitSuccess() {
        long millis = System.currentTimeMillis();
        IScheduledFuture<String> future = consumer.schedule(ScheduledTaskBuilder.newTimeSharing((ctx, firstStep) -> timeSharingJoinString())
                .setFixedDelay(10, 10)
                .setCountLimit(stringList.size()));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        System.out.println(System.currentTimeMillis() - millis);
        Assertions.assertEquals(expectedString, future.resultNow());
    }

    @Test
    void testTimeSharingCountLimitFail() {
        IScheduledFuture<String> future = consumer.schedule(ScheduledTaskBuilder.newTimeSharing(
                (ctx, firstStep) -> timeSharingJoinString())
                .setFixedDelay(0, 10)
                .setCountLimit(stringList.size() - 1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow() == StacklessTimeoutException.INST_COUNT_LIMIT);
    }

    // endregion
}