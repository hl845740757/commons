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
 * 在{@link SleepingWaitStrategy}的基础上增加了超时，让消费者可以从等待中醒来干其它的事情。
 * <p>
 * 1. 先尝试自旋等待一定次数。
 * 2. 然后尝试yield方式自旋一定次数。
 * 3. 然后sleep等待一定次数。
 * 4. 如果数据仍不可用，抛出{@link TimeoutException}
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class TimeoutSleepingWaitStrategy implements WaitStrategy {

    /** 默认实例 */
    public static final TimeoutSleepingWaitStrategy INSTANCE = new TimeoutSleepingWaitStrategy();

    private static final int SPIN_TRIES = 100;
    private static final int YIELD_TRIES = 100;
    private static final int SLEEP_TRIES = 1000;
    private static final int SLEEP_NANOS = 1000; // 共1毫秒

    private final int spinTries;
    private final int yieldTries;
    private final int sleepTries;
    private final long sleepTimeNs;

    public TimeoutSleepingWaitStrategy() {
        this(SPIN_TRIES, YIELD_TRIES, SLEEP_TRIES, SLEEP_NANOS, TimeUnit.NANOSECONDS);
    }

    public TimeoutSleepingWaitStrategy(int spinTries, int yieldTries,
                                       int sleepTries, long sleepTime, TimeUnit unit) {
        this.spinTries = spinTries;
        this.yieldTries = yieldTries;
        this.sleepTries = sleepTries;
        this.sleepTimeNs = unit.toNanos(sleepTime);
    }

    @Override
    public long waitFor(long sequence, ProducerBarrier producerBarrier, SequenceBlocker blocker, ConsumerBarrier barrier)
            throws AlertException, InterruptedException, TimeoutException {

        int counter = spinTries + yieldTries + sleepTries;
        long availableSequence;
        while ((availableSequence = barrier.dependentSequence()) < sequence) {
            barrier.checkAlert();

            if (counter > yieldTries) {
                --counter;
            } else if (counter > sleepTries) {
                --counter;
                Thread.yield();
            } else if (counter > 0) {
                --counter;
                LockSupport.parkNanos(sleepTimeNs);
            } else {
                throw StacklessTimeoutException.INSTANCE;
            }
        }
        return availableSequence;
    }
}