#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Threading;

namespace Wjybxx.Disruptor
{
/// <summary>
/// 生产者序号屏障
/// 1. 生产者屏障负责的是生产者与生产者之间，生产者和消费者之间的协调。
/// 2. 生产者与消费最慢的消费者之间进行协调 -- {@link #dependentSequence()}即为最慢的消费者进度，如果有消费者的话。
/// 3. 生产者之间可能毫无关系，因此生产者之间的协调需要由屏障实现。
/// 4. 生产者由于明确知道自己要生产的数据数量，因此tryNext(n)的接口更易于使用。
///
/// Q: 生产者为什么没有等待策略？
/// A：一开始我确实尝试添加等待策略，后来发现没有意义。我在{@link SequenceBlocker}中提到，生产者不能使用{@link Condition}等待消费者，
/// 因此等待策略的扩展性很有限，除了短暂的挂起线程外，没有好的替代方法。
///
/// Q: 生产者为什么没有{@link ConsumerBarrier#alert()}信号？
/// A：我们将生产者归属于外部系统，而将消费者归属于内部系统。生产者可能仅有部分逻辑与{@link Sequencer}相关，我们不能使用alert信号来中断或终止生产者。
///
/// PS：C#由于支持nullable，因此申请失败时返回null，代码可读性更好。
/// 
/// </summary>
public interface ProducerBarrier : SequenceBarrier
{
    #region disruptor

    /// <summary>
    /// 是否有足够的空间
    /// </summary>
    /// <param name="requiredCapacity">请求的空间大小</param>
    /// <returns>空间足够则返回true，否则返回false</returns>
    bool HasAvailableCapacity(int requiredCapacity);

    /// <summary>
    /// 获取下一个事件的序号，空间不足时会阻塞(等待)。
    /// 申请完空间之后,必须使用<see cref="Publish(long)"/>发布，否则会导致整个数据结构不可用。
    /// </summary>
    /// <returns>申请到的序号</returns>
    long Next();

    /// <summary>
    /// 获取接下来的n个事件的序号，空间不足时会阻塞(等待)。
    /// 申请完空间之后，必须使用<see cref="Publish(long,long)"/>发布，否则会导致整个数据结构不可用
    /// <code>
    /// int n = 10;
    /// long hi = sequencer.next(n);
    /// long lo = hi - (n - 1);
    /// for (long sequence = lo; sequence &lt;= hi; sequence++) {
    ///     // Do work.
    /// }
    /// sequencer.publish(lo, hi);
    /// </code>
    /// </summary>
    /// <param name="n">需要申请的序号数量</param>
    /// <returns>申请到的最大序号</returns>
    long Next(int n);

    /// <summary>
    /// 尝试获取下一个事件的序列 -- 不会阻塞。
    /// 申请完空间之后,必须使用<see cref="Publish(long)"/>发布，否则会导致整个数据结构不可用。
    /// </summary>
    /// <returns>申请成功则返回对应的序号，否则返回null</returns>
    long? TryNext();

    /// <summary>
    /// 尝试获取接下来n个数据的最后一个数据索引位置。不会阻塞,空间不足时抛出异常。
    /// 申请完空间之后，必须使用<see cref="Publish(long,long)"/>发布，否则会导致整个数据结构不可用
    /// <code>
    /// int n = 10;
    /// long hi = sequencer.next(n);
    /// long lo = hi - (n - 1);
    /// for (long sequence = lo; sequence &lt;= hi; sequence++) {
    ///     // Do work.
    /// }
    /// sequencer.publish(lo, hi);
    /// </code>
    /// </summary>
    /// <param name="n">需要申请的序号数量</param>
    /// <returns>申请成功则返回对应的序号，否则返回null</returns>
    long? TryNext(int n);

    /// <summary>
    /// 发布指定序号的数据，表示sequence对应的数据可用
    /// </summary>
    /// <param name="sequence">要发布的序号</param>
    void Publish(long sequence);

    /// <summary>
    /// 批量发布数据，表示 [lowest,highest]区间段整段数据可用。
    /// 一般情况下，hi是<see cref="Next()"/>等方法申请到的最大序号，但也可能不是，生产者可能分段发布数据，以避免阻塞消费者。
    /// </summary>
    /// <param name="lo">要发布的第一个序号</param>
    /// <param name="hi">要发布的最后一个序号</param>
    void Publish(long lo, long hi);

    /// <summary>
    /// 指定序号的数据是否已发布。
    /// 注意：
    /// 1. 该测试只测试序号自身，不测试其前置序号。
    /// 2. 通常情况下你不应该使用它，唯一合理的情况是：清理RingBuffer的时候。
    /// </summary>
    /// <param name="sequence">要查询的序号</param>
    /// <returns></returns>
    bool IsPublished(long sequence);

    /// <summary>
    /// 查询 [nextSequence , availableSequence] 区间段之间连续发布的最大序号。
    /// 该接口用于消费者屏障查询真实可用的序号。
    /// 
    /// 由于多线程的生产者是先申请序号，再发布数据；因此<see cref="SequenceBarrier.Sequence"/>指向的是即将发布数据的槽，而不一定已经具备数据。
    /// 而消费者只能顺序消费，因此【只要消费者的依赖可能包含生产者】，在观察到依赖的最大可用序号后，都应该查询真实可用的序号。
    ///
    /// Get the highest sequence number that can be safely read from the ring buffer.  Depending
    /// on the implementation of the Sequencer this call may need to scan a number of values
    /// in the Sequencer.  The scan will range from nextSequence to availableSequence.  If
    /// there are no available values <code>&gt;= nextSequence</code> the return value will be
    /// <code>nextSequence - 1</code>.  To work correctly a consumer should pass a value that
    /// is 1 higher than the last sequence that was successfully processed.
    /// 
    /// </summary>
    /// <param name="nextSequence">期望消耗的下一个序号;The sequence to start scanning from</param>
    /// <param name="availableSequence">看见的发布的最大序号;The sequence to scan to</param>
    /// <returns></returns>
    long GetHighestPublishedSequence(long nextSequence, long availableSequence);

    #endregion

    #region wjybxx

    //
    /// <summary>
    /// 在<see cref="Next()"/>基础上会响应中断请求
    /// </summary>
    /// <exception cref="ThreadInterruptedException">如果申请期间线程被中断</exception>
    /// <returns></returns>
    long NextInterruptibly();

    //
    /// <summary>
    /// 在<see cref="Next(int)"/>基础上会响应中断请求
    /// </summary>
    /// <param name="n">需要申请的序号数量</param>
    /// <exception cref="ThreadInterruptedException">如果申请期间线程被中断</exception>
    /// <returns></returns>
    long NextInterruptibly(int n);

    /// <summary>
    /// 在给定时间内尝试申请序号
    /// 注意：受限于等待策略的扩展限制，该接口本质是<see cref="TryNext(int)"/>的循环快捷方法。
    /// </summary>
    /// <param name="n">需要申请的序号数量</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>申请成功则返回对应的序号，否则返回null</returns>
    long? TryNext(int n, TimeSpan timeout);

    /// <summary>
    /// 获取用于阻塞等待序号的阻塞器
    /// 注意：可能为null，如果整个系统禁用了基于锁的条件等待。
    /// </summary>
    /// <value></value>
    SequenceBlocker? Blocker { get; }

    /// <summary>
    /// 唤醒所有因条件等待阻塞的线程
    /// <see cref="Blocker"/>的快捷方法
    /// </summary>
    void SignalAllWhenBlocking();

    #endregion
}
}