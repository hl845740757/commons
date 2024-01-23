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

import java.util.concurrent.TimeoutException;

/**
 * 消费者等待策略
 *
 * @author wjybxx
 * date - 2024/1/15
 */
public interface WaitStrategy {

    /**
     * 等待给定的序号可用
     * 实现类通过{@link ProducerBarrier#sequence()}}和{@link ConsumerBarrier#dependentSequence()}进行等待。
     *
     * @param sequence        期望生产或消费的序号
     * @param producerBarrier 用于条件等待策略依赖策略感知生产者进度
     * @param blocker         用于条件等待策略阻塞等待生产者
     * @param barrier         序号屏障 - 用于检测终止信号和查询依赖等。
     * @return 当前可用的序号
     * @throws AlertException       if a status change has occurred for the Disruptor
     * @throws InterruptedException if the thread needs awaking on a condition variable.
     * @throws TimeoutException     if a timeout occurs while waiting for the supplied sequence.
     */
    long waitFor(long sequence,
                 ProducerBarrier producerBarrier,
                 SequenceBlocker blocker,
                 ConsumerBarrier barrier) throws AlertException, InterruptedException, TimeoutException;

}