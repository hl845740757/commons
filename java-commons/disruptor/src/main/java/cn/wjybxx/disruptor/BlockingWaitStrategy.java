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

import java.util.Objects;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.LockSupport;

/**
 * 阻塞等待策略 - 可以达到较低的cpu开销。
 * 1. 通过lock等待【生产者】发布数据。
 * 2. 通过sleep等待前置消费者消费数据。
 * 3. 当吞吐量和低延迟不如CPU资源重要时，可以使用此策略。
 * <p>
 * 第二阶段未沿用Disruptor的的BusySpin模式，因为：
 * 如果前置消费者消费较慢，而后置消费者速度较快，自旋等待可能消耗较多的CPU，
 * 而Blocking策略的目的是为了降低CPU。
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class BlockingWaitStrategy implements WaitStrategy {

    @Override
    public long waitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier)
            throws AlertException, InterruptedException, TimeoutException {
        SequenceBlocker blocker = Objects.requireNonNull(producerBarrier.getBlocker(), "blocker is null");
        // 先通过条件锁等待生产者发布数据
        if (producerBarrier.sequence() < sequence) {
            blocker.lock();
            try {
                while (producerBarrier.sequence() < sequence) {
                    barrier.checkAlert();
                    blocker.await();
                }
            } finally {
                blocker.unlock();
            }
        }
        // sleep方式等待前置消费者消费数据
        long availableSequence;
        while ((availableSequence = barrier.dependentSequence()) < sequence) {
            barrier.checkAlert();
            LockSupport.parkNanos(10);
        }
        return availableSequence;
    }
}