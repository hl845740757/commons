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

import cn.wjybxx.disruptor.MpUnboundedEventSequencer;
import cn.wjybxx.disruptor.TimeoutSleepingWaitStrategy;

import javax.annotation.Nonnull;
import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * 全局事件循环，用于执行一些简单的任务。
 *
 * @author wjybxx
 * date - 2024/8/21
 */
public final class GlobalEventLoop extends DisruptorEventLoop<MiniAgentEvent> {

    public static final GlobalEventLoop INST = new GlobalEventLoop(EventLoopBuilder.<MiniAgentEvent>newDisruptBuilder()
            .setAgent(EmptyAgent.getInstance())
            .setThreadFactory(new DefaultThreadFactory("GlobalEventLoop", true))
            .setEventSequencer(MpUnboundedEventSequencer.newBuilder(MiniAgentEvent::new) // 需要使用无界队列
                    .setWaitStrategy(new TimeoutSleepingWaitStrategy()) // 等待策略需要支持超时，否则无法调度定时任务
                    .setChunkSize(1024)
                    .setMaxPooledChunks(1)
                    .build()));

    private GlobalEventLoop(EventLoopBuilder.DisruptorBuilder<MiniAgentEvent> builder) {
        super(builder);
    }

    // TODO 其实最好返回的Future不能支持等待
    @Override
    public boolean awaitTermination(long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        return false;
    }

    @Override
    public void shutdown() {

    }

    @Nonnull
    @Override
    public List<Runnable> shutdownNow() {
        return List.of();
    }

}
