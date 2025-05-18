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

import cn.wjybxx.base.concurrent.BetterCancellationException;
import cn.wjybxx.base.concurrent.ICancelToken;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

/**
 * 测试能否通过{@link ICancelToken}取消任务
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleCancelTest {

    private static IEventLoop consumer;

    @BeforeAll
    static void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(AgentEvent::new)
                        .build())
                .build();
        consumer.start().join();
    }

    @AfterAll
    static void tearDown() {
        consumer.shutdown();
        consumer.terminationFuture().join();
    }

    @Test
    void testCancel() {
        CancelTokenSource cts = new CancelTokenSource();
        IScheduledFuture<?> future = consumer.scheduleAction(() -> {}, 1000, TimeUnit.MILLISECONDS, cts);

        cts.cancel(1);
        future.awaitUninterruptibly();
        Assertions.assertTrue(future.isCancelled(), () -> future.status().name());

        // 测试关闭Future的取消监听 -- 含特殊统计代码
        {
//            ScheduledTaskBuilder<?> builder = ScheduledTaskBuilder.newAction(() -> {})
//                    .enable(TaskOption.IGNORE_FUTURE_CANCEL)
//                    .setOnlyOnce(1000);
//            IScheduledFuture<?> future = consumer.schedule(builder);
//            long skipped = ScheduledPromiseTask.skippedRegister.get();
//            Assertions.assertTrue(skipped >0, "skipped: " + skipped);
//            future.cancel(false);
//            Assertions.assertTrue(!future.isCancelled(), () -> future.status().name());
        }
    }

    @Test
    void testTimeout() {
        IScheduledFuture<?> future = consumer.schedule(ScheduledTaskBuilder.newFunc(() -> "hello world")
                .setFixedDelay(0, 200)
                .setTimeoutByCount(1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow(false) instanceof BetterCancellationException);
    }

    @Test
    void testCountLimit() {
        IScheduledFuture<?> future = consumer.schedule(ScheduledTaskBuilder.newFunc(() -> "hello world")
                .setFixedDelay(0, 200)
                .setCountLimit(1));

        future.awaitUninterruptibly(300, TimeUnit.MILLISECONDS);
        Assertions.assertTrue(future.exceptionNow(false) instanceof BetterCancellationException);
    }
}