/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.disruptor;

/**
 * 消费者组
 * <p>
 * 1. 组可以单线程的或多线程的，外部不关注；外部只关注其关联的屏障{@link ConsumerBarrier}。
 * 2. 屏障负责管理进度信息，消费者负责真正的消费。
 * 3. 该接口只是一个示例实现，Barrier并没有依赖该接口 -- 依赖是反转的。
 *
 * @author wjybxx
 * date - 2024/1/18
 */
public interface ConsumerGroup {

    /**
     * 消费组关联的屏障
     * <p>
     * 1. 消费者需要更新{@link ConsumerBarrier#groupSequence()}以更新屏障的进度。
     * 2. 消费者需要响应{@link ConsumerBarrier#alert()}信号，以及时响应停止。
     * 3. 多线程消费者需要通过{@link ConsumerBarrier#memberSequence(int)}获取自身的sequence。
     */
    ConsumerBarrier getBarrier();

}