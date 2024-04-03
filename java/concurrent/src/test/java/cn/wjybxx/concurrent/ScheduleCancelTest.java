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
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

/**
 * 测试能否通过{@link ICancelToken}取消任务
 *
 * @author wjybxx
 * date 2023/4/11
 */
public class ScheduleCancelTest {

    private EventLoop consumer;

    @BeforeEach
    void setUp() {
        consumer = EventLoopBuilder.newDisruptBuilder()
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();
    }

    @SuppressWarnings("deprecation")
    @Test
    void testCancel() {
        consumer.start().join();
        {
            IScheduledFuture<String> future = consumer.schedule(() -> "", 1000, TimeUnit.MILLISECONDS);
            future.cancel(false);
            Assertions.assertTrue(future.isCancelled());
        }
        {
            CancelTokenSource cts = new CancelTokenSource();
            Context<Object> context = Context.ofCancelToken(cts);
            IScheduledFuture<?> future = consumer.scheduleAction(ctx -> {
                System.out.println();
            }, context, 1000, TimeUnit.MILLISECONDS);

            cts.cancel(1);
            Assertions.assertTrue(future.isCancelled(), () -> future.status().name());
        }
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
        consumer.shutdown();
        consumer.terminationFuture().join();
    }

}