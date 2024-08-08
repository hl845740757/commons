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

namespace Wjybxx.Disruptor
{
/// <summary>
/// 事件生成器
/// 事件生成器是<see cref="Disruptor.Sequencer"/>和<see cref="DataProvider{T}"/>的集成，每一种实现通常都和数据结构绑定。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface EventSequencer<T> : DataProvider<T>
{
    /** 无界空间对应的常量 */
    public const int UNBOUNDED_CAPACITY = -1;

    #region disruptor

    /// <summary>
    /// 数据结构大小
    /// 1.如果为【无界】数据结构，则返回-1；
    /// 2.如果为【有界】数据结构，则返回真实值。
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// 当前剩余容量
    /// 1.并不一定具有价值，因为多线程模型下查询容器的当前大小时，它反映的总是一个旧值。
    /// 2.如果为【无界】数据结构，可能返回任意值（大于0），但建议返回{@link Integer#MAX_VALUE}。
    /// 3.如果为【有界】数据结构，则返回真实的值。
    /// </summary>
    long RemainingCapacity { get; }

    #endregion

    #region wjybxx

    /** 获取序号生成器 -- 用于特殊需求 */
    Sequencer Sequencer { get; }

    /** 获取生产者屏障 -- 生产者发布数据 */
    ProducerBarrier ProducerBarrier { get; }

    #endregion

    #region 转发-提高易用性

    #region sequencer

    /** 添加一个网关屏障 -- 消费链最末端的消费者屏障 */
    void AddGatingBarriers(params SequenceBarrier[] gatingBarriers) {
        Sequencer.AddGatingBarriers(gatingBarriers);
    }

    /** 移除一个网关屏障 -- 消费链最末端的消费者屏障 */
    bool RemoveGatingBarrier(SequenceBarrier gatingBarrier) {
        return Sequencer.RemoveGatingBarrier(gatingBarrier);
    }

    /** 创建一个【单消费者】的屏障 -- 使用默认的等待策略 */
    ConsumerBarrier NewSingleConsumerBarrier(params SequenceBarrier[] barriersToTrack) {
        return Sequencer.NewSingleConsumerBarrier(barriersToTrack);
    }

    /** 创建一个【单消费者】的屏障 -- 使用自定义等待策略 */
    ConsumerBarrier NewSingleConsumerBarrier(WaitStrategy waitStrategy, params SequenceBarrier[] barriersToTrack) {
        return Sequencer.NewSingleConsumerBarrier(waitStrategy, barriersToTrack);
    }

    /** 创建一个【多消费者】的屏障 -- 使用自定义等待策略 */
    ConsumerBarrier NewMultiConsumerBarrier(int workerCount, params SequenceBarrier[] barriersToTrack) {
        return Sequencer.NewMultiConsumerBarrier(workerCount, barriersToTrack);
    }

    /** 创建一个【多消费者】的屏障 -- 使用自定义等待策略 */
    ConsumerBarrier NewMultiConsumerBarrier(int workerCount, WaitStrategy waitStrategy, params SequenceBarrier[] barriersToTrack) {
        return Sequencer.NewMultiConsumerBarrier(workerCount, waitStrategy, barriersToTrack);
    }

    #endregion

    #region producer

    bool HasAvailableCapacity(int requiredCapacity) {
        return ProducerBarrier.HasAvailableCapacity(requiredCapacity);
    }

    long Next() {
        return ProducerBarrier.Next();
    }

    long Next(int n) {
        return ProducerBarrier.Next(n);
    }

    long? TryNext() {
        return ProducerBarrier.TryNext();
    }

    long? TryNext(int n) {
        return ProducerBarrier.TryNext(n);
    }

    long NextInterruptibly() {
        return ProducerBarrier.NextInterruptibly();
    }

    long NextInterruptibly(int n) {
        return ProducerBarrier.NextInterruptibly(n);
    }

    long? TryNext(int n, TimeSpan timeout) {
        return ProducerBarrier.TryNext(n, timeout);
    }

    void Publish(long sequence) {
        ProducerBarrier.Publish(sequence);
    }

    void Publish(long lo, long hi) {
        ProducerBarrier.Publish(lo, hi);
    }

    #endregion

    # endregion
}
}