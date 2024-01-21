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

import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;

/**
 * 生产者序号屏障
 * 1. 生产者屏障负责的是生产者与生产者之间，生产者和消费者之间的协调。<br>
 * 2. 生产者与消费最慢的消费者之间进行协调 -- {@link #dependentSequence()}即为最慢的消费者进度，如果有消费者的话。<br>
 * 3. 生产者之间可能毫无关系，因此生产者之间的协调需要由屏障实现。<br>
 * 4. 生产者由于明确知道自己要生产的数据数量，因此tryNext(n)的接口更易于使用。
 * <p>
 * Q: 生产者为什么没有等待策略？<br>
 * A：一开始我确实尝试添加等待策略，后来发现没有意义。我在{@link SequenceBlocker}中提到，生产者不能使用{@link Condition}等待消费者，
 * 因此等待策略的扩展性很有限，除了短暂的挂起线程外，没有好的替代方法。
 * <p>
 * Q: 生产者为什么没有{@link ConsumerBarrier#alert()}信号？
 * A：我们将生产者归属于外部系统，而将消费者归属于内部系统。生产者可能仅有部分逻辑与{@link Sequencer}相关，我们不能使用alert信号来中断或终止生产者。
 *
 * @author wjybxx
 * date - 2024/1/16
 */
public interface ProducerBarrier extends SequenceBarrier {

    // region disruptor

    /**
     * 是否有足够的空间
     * Has the buffer got capacity to allocate another sequence.  This is a concurrent
     * method so the response should only be taken as an indication of available capacity.
     *
     * @param requiredCapacity in the buffer
     * @return true if the buffer has the capacity to allocate the next sequence otherwise false.
     */
    boolean hasAvailableCapacity(int requiredCapacity);

    /**
     * 获取下一个事件的序号，空间不足时会阻塞(等待)。
     * 申请完空间之后,必须使用 {@link #publish(long)} 发布，否则会导致整个数据结构不可用。
     * <p>
     * Claim the next event in sequence for publishing.
     *
     * @return the claimed sequence value
     */
    long next();

    /**
     * 获取接下来的n个事件的序号，空间不足时会阻塞(等待)。
     * 申请完空间之后，必须使用 {@link #publish(long, long)} 发布，否则会导致整个数据结构不可用
     * <p>
     * Claim the next n events in sequence for publishing.  This is for batch event producing.  Using batch producing
     * requires a little care and some math.
     * <pre>
     * int n = 10;
     * long hi = sequencer.next(n);
     * long lo = hi - (n - 1);
     * for (long sequence = lo; sequence &lt;= hi; sequence++) {
     *     // Do work.
     * }
     * sequencer.publish(lo, hi);
     * </pre>
     *
     * @param n the number of sequences to claim
     * @return the highest claimed sequence value
     */
    long next(int n);

    /**
     * 尝试获取下一个事件的序列，空间不足时抛出异常。
     * 申请完空间之后,必须使用 {@link #publish(long)} 发布，否则会导致整个数据结构不可用。
     * <p>
     * Attempt to claim the next event in sequence for publishing.  Will return the
     * number of the slot if there is at least <code>requiredCapacity</code> slots
     * available.
     *
     * @return 申请成功则返回对应的序号，否则返回 -1
     */
    long tryNext();

    /**
     * 尝试获取接下来n个数据的最后一个数据索引位置。不会阻塞,空间不足时抛出异常。
     * 申请完空间之后，必须使用 {@link #publish(long, long)} 发布，否则会导致整个数据结构不可用
     * <b>使用该方法可以避免死锁</b>
     * <p>
     * Attempt to claim the next n events in sequence for publishing.  Will return the
     * highest numbered slot if there is at least <code>requiredCapacity</code> slots
     * available.  Have a look at {@link #next()} for a description on how to
     * use this method.
     *
     * @param n 需要申请的序号数量
     * @return 申请成功则返回对应的序号，否则返回 -1
     */
    long tryNext(int n);

    /**
     * 发布指定序号的数据，表示sequence对应的数据可用
     * Publishes a sequence. Call when the event has been filled.
     *
     * @param sequence the sequence to be published.
     */
    void publish(long sequence);

    /**
     * 批量发布数据，表示 [lowest,highest]区间段整段数据可用。
     * 一般情况下，{@code hi}是{@link #next()}等方法申请到的最大序号，但也可能不是，生产者可能分段发布数据，以避免阻塞消费者。
     * <p>
     * Batch publish sequences.  Called when all of the events have been filled.
     *
     * @param lo first sequence number to publish
     * @param hi last sequence number to publish
     */
    void publish(long lo, long hi);

    /**
     * 指定序号的数据是否已发布。
     * 注意：
     * 1. 该测试只测试序号自身，不测试其前置序号。
     * 2. 通常情况下你不应该使用它，唯一合理的情况是：清理RingBuffer的时候。
     * <p>
     * Confirms if a sequence is published and the event is available for use; non-blocking.
     *
     * @param sequence of the buffer to check
     * @return true if the sequence is available for use, false if not
     */
    boolean isPublished(long sequence);

    /**
     * 查询 [nextSequence , availableSequence] 区间段之间连续发布的最大序号。
     * 该接口用于消费者屏障查询真实可用的序号。
     * <p>
     * 由于多线程的生产者是先申请序号，再发布数据；因此{@link #sequence()}指向的是即将发布数据的槽，而不一定已经具备数据。
     * 而消费者只能顺序消费，因此【只要消费者的依赖可能包含生产者】，在观察到依赖的最大可用序号后，都应该查询真实可用的序号。
     * <p>
     * Get the highest sequence number that can be safely read from the ring buffer.  Depending
     * on the implementation of the Sequencer this call may need to scan a number of values
     * in the Sequencer.  The scan will range from nextSequence to availableSequence.  If
     * there are no available values <code>&gt;= nextSequence</code> the return value will be
     * <code>nextSequence - 1</code>.  To work correctly a consumer should pass a value that
     * is 1 higher than the last sequence that was successfully processed.
     *
     * @param nextSequence      The sequence to start scanning from.
     * @param availableSequence The sequence to scan to.
     * @return The highest value that can be safely read, will be at least <code>nextSequence - 1</code>.
     */
    long getHighestPublishedSequence(long nextSequence, long availableSequence);

    // endregion

    // region wjybxx

    /**
     * 在{@link #next()}基础上会响应中断请求
     *
     * @throws InterruptedException 如果申请期间线程被中断
     */
    long nextInterruptibly() throws InterruptedException;

    /**
     * 在{@link #next(int)}基础上会响应中断请求
     *
     * @throws InterruptedException 如果申请期间线程被中断
     */
    long nextInterruptibly(int n) throws InterruptedException;

    /**
     * 在给定时间内尝试申请序号
     * 注意：受限于等待策略的扩展限制，该接口本质是{@link #tryNext(int)}的循环快捷方法。
     *
     * @param n 需要申请的序号数量
     * @return 申请成功则返回对应的序号，否则返回 -1
     */
    long tryNext(int n, long timeout, TimeUnit unit);

    // endregion

}