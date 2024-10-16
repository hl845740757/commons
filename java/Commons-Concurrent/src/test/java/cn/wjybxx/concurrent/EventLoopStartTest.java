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
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.concurrent.CompletionException;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public class EventLoopStartTest {

    private EventLoop newEventLoop(boolean thr, long delay) {
        return EventLoopBuilder.newDisruptBuilder()
                .setParent(null)
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(new Agent(thr, delay))
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(RingBufferEvent::new)
                        .build())
                .build();
    }

    @Test
    void testSucceeded() {
        EventLoop eventLoop = newEventLoop(false, 0);
        Assertions.assertDoesNotThrow(() -> {
            eventLoop.start().join();
        });
        ThreadUtils.sleepQuietly(10);
        eventLoop.shutdownNow();
    }

    @Test
    void testFailed() {
        EventLoop eventLoop = newEventLoop(true, 0);
        Assertions.assertThrowsExactly(CompletionException.class, () -> {
            eventLoop.start().join();
        });
        ThreadUtils.sleepQuietly(10);
        eventLoop.shutdownNow();
    }

    @Test
    void testDelayFailed() {
        EventLoop eventLoop = newEventLoop(true, 100);
        Assertions.assertThrowsExactly(CompletionException.class, () -> {
            eventLoop.start();
            eventLoop.shutdown();
            eventLoop.runningFuture().join();
        });
        ThreadUtils.sleepQuietly(10);
        eventLoop.shutdownNow();
    }

    private static class Agent implements EventLoopAgent<IAgentEvent> {

        final boolean thr;
        final long delay;

        /** @param thr 启动时是否抛出异常 */
        private Agent(boolean thr, long delay) {
            this.thr = thr;
            this.delay = delay;
        }

        @Override
        public void inject(EventLoop eventLoop) {

        }

        @Override
        public void onStart() throws Exception {
            if (delay > 0) ThreadUtils.sleepQuietly(delay);
            if (thr) throw new RuntimeException();
        }

        @Override
        public void onEvent(long sequence, IAgentEvent event) throws Exception {

        }

        @Override
        public void update() throws Exception {

        }

        @Override
        public void onShutdown() throws Exception {

        }
    }


}