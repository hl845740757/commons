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

package cn.wjybxx.sequential;

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.time.TimeProviders;
import cn.wjybxx.concurrent.Counter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/4/3
 */
public class UniScheduleOrderTest {

    private Counter counter;
    private UniScheduledExecutor consumer;

    @BeforeEach
    void setUp() {
        counter = new Counter();
        consumer = new DefaultUniScheduledExecutor(TimeProviders.systemMillisProvider());
    }

    /** 测试initDelay相同时，任务是否按照按提交顺序执行 */
    @Test
    void testScheduleOrder() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;
        for (int i = 0; i < 100; i++) {
            Runnable newTask = counter.newTask(1, i);
            switch (random.nextInt(3)) {
                case 1 -> consumer.scheduleWithFixedDelay(newTask, 100, 200, milliseconds);
                case 2 -> consumer.scheduleAtFixedRate(newTask, 100, 200, milliseconds);
                default -> consumer.schedule(newTask, 100, milliseconds);
            }
        }

        for (int i = 0; i < 100; i++) {
            consumer.update();
            ThreadUtils.sleepQuietly(1);
        }
        consumer.shutdownNow();

        Assertions.assertTrue(!counter.getSequenceMap().isEmpty(), "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }

    /** 测试execute和schedule(0)的顺序 */
    @Test
    void testExecuteScheduleOrder() {
        final ThreadLocalRandom random = ThreadLocalRandom.current();
        final TimeUnit milliseconds = TimeUnit.MILLISECONDS;
        for (int i = 0; i < 100; i++) {
            Runnable newTask = counter.newTask(1, i);
            switch (random.nextInt(3)) {
                case 1 -> consumer.execute(newTask);
                case 2 -> consumer.submitAction(newTask);
                default -> consumer.schedule(newTask, 0, milliseconds);
            }
        }

        for (int i = 0; i < 100; i++) {
            consumer.update();
            ThreadUtils.sleepQuietly(1);
        }
        consumer.shutdownNow();

        Assertions.assertTrue(!counter.getSequenceMap().isEmpty(), "Counter.sequenceMap.size == 0");
        Assertions.assertTrue(counter.getErrorMsgList().isEmpty(), counter.getErrorMsgList()::toString);
    }
}
