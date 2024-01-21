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

package cn.wjybxx.disruptor;

import cn.wjybxx.base.Preconditions;

import java.util.Objects;
import java.util.concurrent.TimeoutException;

/**
 * 单线程消费者屏障
 *
 * @author wjybxx
 * date - 2024/1/18
 */
public class SingleConsumerBarrier implements ConsumerBarrier {

    private final Sequencer sequencer;
    private final WaitStrategy waitStrategy;

    private final Sequence groupSequence = new Sequence(INITIAL_SEQUENCE);
    private final SequenceBarrier[] dependentBarriers;
    private volatile boolean alerted = false;

    /**
     * @param sequencer         序号生成器
     * @param waitStrategy      该组消费者的等待策略
     * @param dependentBarriers 依赖的屏障
     */
    public SingleConsumerBarrier(Sequencer sequencer,
                                 WaitStrategy waitStrategy,
                                 SequenceBarrier... dependentBarriers) {
        Objects.requireNonNull(dependentBarriers, "dependentBarriers");
        Preconditions.checkNullElements(dependentBarriers, "dependentBarriers");
        // 如果未显式指定前置依赖，则添加生产者依赖
        if (dependentBarriers.length == 0) {
            dependentBarriers = new SequenceBarrier[1];
            dependentBarriers[0] = sequencer.getProducerBarrier();
        }
        this.sequencer = Objects.requireNonNull(sequencer);
        this.waitStrategy = Objects.requireNonNull(waitStrategy);
        this.dependentBarriers = dependentBarriers;
    }

    // region consumer

    @Override
    public long waitFor(long sequence) throws AlertException, InterruptedException, TimeoutException {
        checkAlert();

        // available是生产者或前置消费者的进度
        long availableSequence = waitStrategy.waitFor(sequence, sequencer, this);
        if (availableSequence < sequence) {
            return availableSequence;
        }
        // 只要依赖可能包含生产者，都需要检查数据的连续性
        return sequencer.getProducerBarrier().getHighestPublishedSequence(sequence, availableSequence);
    }

    @Override
    public boolean isAlerted() {
        return alerted;
    }

    @Override
    public void alert() {
        alerted = true;
        sequencer.signalAllWhenBlocking();
    }

    @Override
    public void clearAlert() {
        alerted = false;
    }

    @Override
    public void checkAlert() throws AlertException {
        if (alerted) {
            throw AlertException.INSTANCE;
        }
    }
    // endregion

    //region barrier

    @Deprecated
    @Override
    public void claim(long sequence) {
        groupSequence.setRelease(sequence);
    }

    @Override
    public Sequence memberSequence(int index) {
        if (index != 0) throw new IndexOutOfBoundsException(index);
        return groupSequence;
    }

    @Override
    public Sequence groupSequence() {
        return groupSequence;
    }

    @Override
    public long sequence() {
        return groupSequence.getVolatile();
    }

    @Override
    public long dependentSequence() {
        return Util.getMinimumSequence(dependentBarriers);
    }

    @Override
    public long minimumSequence() {
        return Util.getMinimumSequence(dependentBarriers, groupSequence.getVolatile());
    }

    @Override
    public void addDependentBarriers(SequenceBarrier... barriersToTrack) {
        throw new UnsupportedOperationException();
    }

    @Override
    public boolean removeDependentBarrier(SequenceBarrier barrier) {
        return false;
    }

    // endregion
}
