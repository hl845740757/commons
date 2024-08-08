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
 * 自旋等待策略
 * 特征：极低的延迟，极高的吞吐量，以及极高的CPU占用。
 * <p>
 * 1. 该策略通过占用CPU资源去比避免系统调用带来的延迟抖动。最好在线程能绑定到特定的CPU核心时使用。
 * 2. 会持续占用CPU资源，基本不会让出CPU资源。
 * 3. 如果你要使用该等待策略，确保有足够的CPU资源，且你能接受它带来的CPU使用率。
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class BusySpinWaitStrategy implements WaitStrategy {

    public static final BusySpinWaitStrategy INSTANCE = new BusySpinWaitStrategy();

    @Override
    public long waitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier)
            throws TimeoutException, AlertException, InterruptedException {

        long availableSequence;
        while ((availableSequence = barrier.dependentSequence()) < sequence) {
            barrier.checkAlert();
            Thread.onSpinWait();
        }
        return availableSequence;
    }

}