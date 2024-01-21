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


import cn.wjybxx.disruptor.EventSequencer;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import cn.wjybxx.disruptor.WaitStrategy;

import java.util.concurrent.ThreadFactory;

/**
 * @author wjybxx
 * date 2023/4/11
 */
public abstract class EventLoopBuilder {

    private EventLoopGroup parent;
    private RejectedExecutionHandler rejectedExecutionHandler = RejectedExecutionHandlers.abort();
    private ThreadFactory threadFactory;

    private EventLoopAgent agent;
    private EventLoopModule mainModule;
    private int batchSize = 8192;

    public abstract EventLoop build();

    public EventLoopGroup getParent() {
        return parent;
    }

    public EventLoopBuilder setParent(EventLoopGroup parent) {
        this.parent = parent;
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return threadFactory;
    }

    public EventLoopBuilder setThreadFactory(ThreadFactory threadFactory) {
        this.threadFactory = threadFactory;
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return rejectedExecutionHandler;
    }

    public EventLoopBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        this.rejectedExecutionHandler = rejectedExecutionHandler;
        return this;
    }

    /** EventLoop的内部代理 */
    public EventLoopAgent getAgent() {
        return agent;
    }

    public EventLoopBuilder setAgent(EventLoopAgent agent) {
        this.agent = agent;
        return this;
    }

    /** EventLoop的主模块 */
    public EventLoopModule getMainModule() {
        return mainModule;
    }

    public EventLoopBuilder setMainModule(EventLoopModule mainModule) {
        this.mainModule = mainModule;
        return this;
    }

    /** 每次最多处理多少个事件就尝试执行一次{@link EventLoopAgent#update()}方法 */
    public int getBatchSize() {
        return batchSize;
    }

    public EventLoopBuilder setBatchSize(int batchSize) {
        this.batchSize = batchSize;
        return this;
    }
    //

    public static DisruptorBuilder newDisruptBuilder() {
        return new DisruptorBuilder();
    }

    //

    public static class DisruptorBuilder extends EventLoopBuilder {

        private EventSequencer<RingBufferEvent> eventSequencer;
        private WaitStrategy waitStrategy;
        private boolean cleanBufferOnExit = true;

        //

        @Override
        public DisruptorBuilder setParent(EventLoopGroup parent) {
            super.setParent(parent);
            return this;
        }

        @Override
        public DisruptorBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
            super.setRejectedExecutionHandler(rejectedExecutionHandler);
            return this;
        }

        @Override
        public DisruptorBuilder setThreadFactory(ThreadFactory threadFactory) {
            super.setThreadFactory(threadFactory);
            return this;
        }

        @Override
        public DisruptorBuilder setAgent(EventLoopAgent agent) {
            super.setAgent(agent);
            return this;
        }

        @Override
        public DisruptorBuilder setMainModule(EventLoopModule mainModule) {
            super.setMainModule(mainModule);
            return this;
        }

        public DisruptorBuilder setBatchSize(int batchSize) {
            super.setBatchSize(batchSize);
            return this;
        }

        @Override
        public DisruptorEventLoop build() {
            if (getThreadFactory() == null) {
                setThreadFactory(new DefaultThreadFactory("DisruptorEventLoop"));
            }
            if (eventSequencer == null) {
                eventSequencer = RingBufferEventSequencer.<RingBufferEvent>newMultiProducer()
                        .setFactory(RingBufferEvent::new)
                        .setBufferSize(8192)
                        .build();
            }
            return new DisruptorEventLoop(this);
        }

        //

        /**
         * 事件序列生成器
         * 注意：应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
         */
        public EventSequencer<RingBufferEvent> getEventSequencer() {
            return eventSequencer;
        }

        public DisruptorBuilder setEventSequencer(EventSequencer<RingBufferEvent> eventSequencer) {
            this.eventSequencer = eventSequencer;
            return this;
        }

        /** 等待策略 -- 如果未显式指定，则使用{@link #eventSequencer}中的默认等待策略 */
        public WaitStrategy getWaitStrategy() {
            return waitStrategy;
        }

        public DisruptorBuilder setWaitStrategy(WaitStrategy waitStrategy) {
            this.waitStrategy = waitStrategy;
            return this;
        }

        /**
         * EventLoop在退出的时候是否清理buffer
         * 1. 默认清理
         * 2. 如果该值为true，意味着当前消费者是消费者的末端，或仅有该EventLoop消费者。
         */
        public boolean isCleanBufferOnExit() {
            return cleanBufferOnExit;
        }

        public DisruptorBuilder setCleanBufferOnExit(boolean cleanBufferOnExit) {
            this.cleanBufferOnExit = cleanBufferOnExit;
            return this;
        }
    }

}