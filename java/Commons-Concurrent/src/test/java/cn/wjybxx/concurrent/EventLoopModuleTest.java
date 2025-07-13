/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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
import cn.wjybxx.base.fx.ComponentDefine;
import cn.wjybxx.base.fx.ComponentId;
import cn.wjybxx.base.fx.ComponentKind;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import it.unimi.dsi.fastutil.ints.Int2ObjectArrayMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2025/3/27
 */
public class EventLoopModuleTest {

    private static IEventLoop eventLoop;
    private static final ComponentId<DataModule> dataCid = IEventLoopModule.GLOBAL.valueOf(DataModule.class);
    private static final ComponentId<BehaviorModule> behaviorCid = IEventLoopModule.GLOBAL.valueOf(BehaviorModule.class);

    @BeforeEach
    void setUp() {
        eventLoop = EventLoopBuilder.<AgentEvent>newDisruptBuilder()
                .setParent(null)
                .setThreadFactory(new DefaultThreadFactory("consumer"))
                .setAgent(new Agent())
                .setEventSequencer(RingBufferEventSequencer
                        .newMultiProducer(AgentEvent::new)
                        .build())
                // 添加模块
                .addModule(new DataModule())
                .addModule(new BehaviorModule())
                .build();
        eventLoop.start().join();
    }

    @Test
    void testUpdate() {
        ThreadUtils.sleepQuietly(1000);
        try {
            // 数据组件为0
            DataModule dataModule = eventLoop.getComponent(dataCid);
            Assertions.assertTrue(dataModule.awakeInvoked);
            Assertions.assertEquals(dataModule.updateCount, 0);
            Assertions.assertEquals(dataModule.lastUpdateCount, 0);
            // 行为组件非0
            BehaviorModule behaviorModule = eventLoop.getComponent(behaviorCid);
            Assertions.assertTrue(behaviorModule.awakeInvoked);
            Assertions.assertTrue(behaviorModule.onReadyInvoked);
            Assertions.assertNotEquals(behaviorModule.earlyUpdate, 0);
            Assertions.assertNotEquals(behaviorModule.updateCount, 0);
            Assertions.assertNotEquals(behaviorModule.lastUpdateCount, 0);
            Assertions.assertNotEquals(behaviorModule.eventCount, 0);
        } finally {
            eventLoop.shutdownNow();
        }
    }

    private static class Agent implements IEventLoopAgent<AgentEvent> {

        private long lastUpdateTime = System.currentTimeMillis();
        private final Int2ObjectMap<IAgentEventHandler<? super AgentEvent>> handlerMap = new Int2ObjectArrayMap<>(4);

        @Override
        public boolean checkMainLoop(long threadTime) {
            return System.currentTimeMillis() - lastUpdateTime >= 10;
        }

        @Override
        public void beforeMainLoop(long threadTime) {
            lastUpdateTime = System.currentTimeMillis();
        }

        @Override
        public void subscribe(int type, IAgentEventHandler<? super AgentEvent> handler) {
            handlerMap.put(type, handler);
        }

        @Override
        public void onEvent(long sequence, AgentEvent event) throws Exception {
            var handler = handlerMap.get(event.getType());
            if (handler != null) {
                handler.onEvent(sequence, event);
            }
        }
    }

    @ComponentDefine(kind = ComponentKind.DATA)
    private static class DataModule extends EventLoopModule {

        boolean awakeInvoked;
        long updateCount;
        long lastUpdateCount;

        @Override
        public void onAwake() {
            awakeInvoked = true;
        }

        @Override
        public void update() throws Exception {
            updateCount++;
        }

        @Override
        public void lateUpdate() throws Exception {
            lastUpdateCount++;
        }
    }

    @ComponentDefine(kind = ComponentKind.SCRIPT)
    private static class BehaviorModule extends EventLoopModule implements IAgentEventHandler<AgentEvent> {

        boolean awakeInvoked;
        boolean onReadyInvoked;
        long earlyUpdate;
        long updateCount;
        long lastUpdateCount;
        int eventCount;
        DisruptorEventLoop<AgentEvent> eventLoop;

        @Override
        public void onAwake() {
            @SuppressWarnings("unchecked") var eventLoop = (DisruptorEventLoop<AgentEvent>) getEntity();
            this.awakeInvoked = true;
            this.eventLoop = eventLoop;
        }

        @Override
        public void resolveDependence() {
            onReadyInvoked = true;
        }

        @Override
        public void start() {
            eventLoop.subscribe(1, this);
        }

        @Override
        public void earlyUpdate() throws Exception {
            earlyUpdate++;
        }

        @Override
        public void update() throws Exception {
            updateCount++;
            if (((updateCount) & 7) == 0) {
                generateEvent();
            }
        }

        @Override
        public void lateUpdate() throws Exception {
            lastUpdateCount++;
        }

        private void generateEvent() {
            long sequence = eventLoop.nextSequence(1);
            if (sequence < 0) return;
            AgentEvent event = eventLoop.getEvent(sequence);
            event.setType(1);
            eventLoop.publish(sequence);
        }

        @Override
        public void onEvent(long sequence, AgentEvent event) throws Exception {
            assert event.getType() == 1;
            eventCount++;
        }

    }

}