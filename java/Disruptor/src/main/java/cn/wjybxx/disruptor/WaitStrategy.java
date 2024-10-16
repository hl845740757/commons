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
 * 消费者等待策略
 * PS：由于C#只要抛出异常就会导致性能下降（即使我们的异常实现为不捕获堆栈的），为保持代码一致，java端也使用{@code sequence-1}来表示超时。
 *
 * @author wjybxx
 * date - 2024/1/15
 */
public interface WaitStrategy {

    /**
     * 等待给定的序号可用
     * 实现类通过{@link ProducerBarrier#sequence()}}和{@link ConsumerBarrier#dependentSequence()}进行等待。
     *
     * @param sequence        期望消费的序号
     * @param producerBarrier 用于条件等待策略依赖策略感知生产者进度
     * @param barrier         消费者屏障 - 用于检测终止信号和查询依赖等。
     * @return 当前可用的序号，{@code sequence-1} 表示等待超时，返回值不可以比{@code sequence -1} 更小。
     * @throws AlertException       if a status change has occurred for the Disruptor
     * @throws InterruptedException if the thread needs awaking on a condition variable.
     */
    long waitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier)
            throws AlertException, InterruptedException;

}