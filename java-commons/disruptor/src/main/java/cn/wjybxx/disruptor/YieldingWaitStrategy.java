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
 * 该策略在尝试一定次数的自旋等待(空循环)之后使用尝试让出cpu。
 * 该策略将会占用大量的CPU资源(100%)，但是比{@link BusySpinWaitStrategy}策略更容易在其他线程需要CPU时让出CPU。
 * <p>
 * 它有着较低的延迟、较高的吞吐量，以及较高CPU占用率。当CPU数量足够时，可以使用该策略。
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class YieldingWaitStrategy implements WaitStrategy {

    private final int spinTries;

    public YieldingWaitStrategy() {
        spinTries = 100;
    }

    /** @param spinTries 自旋等待尝试次数 */
    public YieldingWaitStrategy(int spinTries) {
        this.spinTries = spinTries;
    }

    @Override
    public long waitFor(long sequence, ProducerBarrier producerBarrier, SequenceBlocker blocker, ConsumerBarrier barrier)
            throws AlertException, InterruptedException, TimeoutException {

        int counter = spinTries;
        long availableSequence;
        while ((availableSequence = barrier.dependentSequence()) < sequence) {
            barrier.checkAlert();

            if (counter > 0) {
                --counter;
            } else {
                Thread.yield();
            }
        }
        return availableSequence;
    }

}