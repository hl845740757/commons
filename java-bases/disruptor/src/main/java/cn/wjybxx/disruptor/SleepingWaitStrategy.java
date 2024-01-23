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

import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.LockSupport;

/**
 * 睡眠等待策略。
 * 表现：延迟不均匀，吞吐量较低，但是cpu占有率也较低。
 * 算是CPU与性能之间的一个折中，当CPU资源紧张时可以考虑使用该策略。
 * <p>
 * 1. 先尝试自旋等待一定次数。
 * 2. 然后尝试yield方式自旋一定次数。
 * 3. 然后sleep等待。
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class SleepingWaitStrategy implements WaitStrategy {

    private static final int SPIN_TRIES = 100;
    private static final int YIELD_TRIES = 100;
    private static final int SLEEP_NANOS = 1000;

    private final int spinTries;
    private final int yieldTries;
    private final long sleepTimeNs;

    public SleepingWaitStrategy() {
        this(SPIN_TRIES, YIELD_TRIES, SLEEP_NANOS, TimeUnit.NANOSECONDS);
    }

    public SleepingWaitStrategy(int spinTries, int yieldTries, long sleepTime, TimeUnit unit) {
        this.spinTries = spinTries;
        this.yieldTries = yieldTries;
        this.sleepTimeNs = unit.toNanos(sleepTime);
    }

    @Override
    public long waitFor(long sequence, ProducerBarrier producerBarrier, SequenceBlocker blocker, ConsumerBarrier barrier)
            throws AlertException, InterruptedException, TimeoutException {

        int counter = spinTries + yieldTries;
        long availableSequence;
        while ((availableSequence = barrier.dependentSequence()) < sequence) {
            barrier.checkAlert();

            if (counter > yieldTries) {
                --counter;
            } else if (counter > 0) {
                --counter;
                Thread.yield();
            } else {
                LockSupport.parkNanos(sleepTimeNs);
            }
        }
        return availableSequence;
    }
}