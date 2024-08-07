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

import javax.annotation.concurrent.ThreadSafe;

/**
 * 序号屏障
 * <p>
 * 1. 一个屏障关联一组生产者或一组消费者，生产者和消费者都以组为单元。<br>
 * 2. 屏障协调的是这些组单元，即：协调的基本单位是Barrier! 协调的基本单位是Barrier! 协调的基本单位是Barrier!<br>
 * 3. 生产者与生产者之间，生产者与消费者之间，消费者与消费者之间，都通过屏障进行协调。
 *
 * @author wjybxx
 * date - 2024/1/16
 */
@ThreadSafe
public interface SequenceBarrier {

    /**
     * 将-1作为默认序号
     * Set to -1 as sequence starting point
     */
    long INITIAL_SEQUENCE = -1L;

    // region wjybxx

    /**
     * 获取生产者组/消费者组用的{@link Sequence}。
     * 1. 生产者/消费者通过更新该Sequence更新屏障的进度。
     * 2. 如果当前屏障是一个视图对象，而不是真正用于工作的屏障，该方法应当抛出异常，而不是创建一个虚假的{@link Sequence}。
     *
     * @throws UnsupportedOperationException 如果当前屏障是一个视图对象
     */
    Sequence groupSequence();

    /**
     * 设置当前屏障的序号。
     * 1. 仅在初始化屏障时使用。
     * 2. 这是个很危险的方法，避免在运行中使用，否则可能导致错误。
     * 3. 屏障的默认初始值为{@link SequenceBarrier#INITIAL_SEQUENCE}。
     *
     * @param sequence 要初始化的值
     * @deprecated 使用该方法时必须显式禁用警告。
     */
    @Deprecated
    void claim(long sequence);

    /**
     * 当前Barrier关联的【生产或消费】进度。
     * 上游需要感知当前Barrier的消费进度 -- 计算它的{@link #dependentSequence()}
     *
     * @return 进度
     */
    long sequence();

    /**
     * 获取当前屏障【依赖的生产者或消费者】的序号。
     * <h3>生产者</h3>
     * 1.对生产者而言，这是下一个可写数据的序号。
     * 2.可能没有消费者，因此生产者可能没有依赖。
     *
     * <h3>消费者</h3>
     * 1.对消费者而言：这是下一个可消费数据的序号。
     * 2.消费者可依赖生产者和其它消费者，该序号为所有依赖的最小序号。
     * 3.依赖其它消费者时，依赖的是其已消费的序号 -- 确保可见性。
     *
     * @return 所有依赖的最小序号；没有依赖的情况下返回{@link Long#MAX_VALUE}
     */
    long dependentSequence();

    /**
     * 获取{@link #sequence()}和{@link #dependentSequence()}两者之间的最小序号。
     */
    long minimumSequence();

    /**
     * 添加当前屏障需要追踪的屏障。
     * 当前屏障关联的生产者或消费者会持续最终他们的进度，以保证数据生产或消费的正确性。
     * <p>
     * ps:生产者的要追踪屏障通常是动态的，消费者要追踪的屏障通常在构造时指定。
     *
     * @param barriersToTrack 要追踪的屏障
     * @throws UnsupportedOperationException 如果不支持动态添加依赖
     */
    void addDependentBarriers(SequenceBarrier... barriersToTrack);

    /**
     * 移除这些屏障，不再跟踪它们的进度信息；
     * 特殊用法：如果移除了所有的消费者，那么生产者便不会被阻塞，也就能{@link ProducerBarrier#next()} 死循环中醒来！
     *
     * @param barrier 要删除的屏障
     * @return 如果给定barrier存在且删除成功则返回true，否则返回false。
     */
    boolean removeDependentBarrier(SequenceBarrier barrier);

    // endregion
}