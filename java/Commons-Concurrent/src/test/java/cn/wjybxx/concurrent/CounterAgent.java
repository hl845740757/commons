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

import javax.annotation.concurrent.NotThreadSafe;

/**
 * 仅限于单消费者测试
 *
 * @author wjybxx
 * date 2023/4/13
 */
@NotThreadSafe
final class CounterAgent implements IEventLoopAgent<AgentEvent> {

    final Counter counter = new Counter();
    long lastTime = System.currentTimeMillis();

    public Counter getCounter() {
        return counter;
    }

    @Override
    public void onEvent(long sequence, AgentEvent event) throws Exception {
        counter.count(event.getType(), event.longVal1);
    }

    @Override
    public boolean checkMainLoop(long threadTime) {
        return System.currentTimeMillis() - lastTime >= 10;
    }

    @Override
    public void beforeMainLoop(long threadTime) {
        lastTime = System.currentTimeMillis();
    }
}