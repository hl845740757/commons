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

namespace Wjybxx.Disruptor
{
/// <summary>
/// 序号生成器
/// 1. 序号生成器是生产者和消费者协调的集成。
/// 2. 不继承<see cref="ProducerBarrier"/>是为了避免暴露不必要的接口给等待策略等。
///
/// <h3>安全停止</h3>
/// 要安全停止整个系统，必须调用所有消费者的<see cref="ConsumerBarrier.Alert"/>方法停止消费者，
/// 然后调用<see cref="SequenceBarrier.RemoveDependentBarrier"/>从生产者要追踪的屏障中删除。
/// 否则会导致死锁！！！
///
/// ps: Sequencer默认不追踪所有的<see cref="SequenceBarrier"/>，因此依赖用户进行管理。
/// </summary>
public interface Sequencer
{
    #region disruptor

    /// <summary>
    /// 添加序号生成器需要追踪的网关屏障（新增的末端消费者消费序列/进度），
    /// Sequencer（生产者）会持续跟踪它们的进度信息，以协调生产者和消费者之间的速度。
    /// 即生产者想使用一个序号时必须等待所有的网关Sequence处理完该序号。
    ///
    /// Add the specified gating sequences to this instance of the Disruptor.  They will
    /// safely and atomically added to the list of gating sequences.
    /// </summary>
    /// <param name="gatingBarriers">要添加的屏障</param>
    void AddGatingBarriers(params SequenceBarrier[] gatingBarriers) {
        ProducerBarrier.AddDependentBarriers(gatingBarriers);
    }

    /// <summary>
    /// 移除这些网关屏障，不再跟踪它们的进度信息；
    /// 特殊用法：如果移除了所有的消费者，那么生产者便不会被阻塞，就能从<see cref="ProducerBarrier.Next()"/>中退出。
    /// </summary>
    /// <param name="gatingBarrier">要删除的屏障</param>
    /// <returns>如果屏障存在，则返回成功</returns>
    bool RemoveGatingBarrier(SequenceBarrier gatingBarrier) {
        return ProducerBarrier.RemoveDependentBarrier(gatingBarrier);
    }

    #endregion

    #region wjybxx

    /// <summary>
    /// 默认等待策略
    /// </summary>
    /// <value></value>
    WaitStrategy WaitStrategy { get; }

    /// <summary>
    /// 获取生产者屏障 --用于生产者申请和发布数据。
    /// </summary>
    /// <value></value>
    ProducerBarrier ProducerBarrier { get; }

    /// <summary>
    /// 使用默认的等待策略创建一个【单线程消费者】使用的屏障。
    /// ps: 用户可以创建自己的自定义实例。
    /// </summary>
    /// <param name="barriersToTrack">该组消费者依赖的屏障</param>
    /// <returns>默认的消费者屏障</returns>
    ConsumerBarrier NewSingleConsumerBarrier(params SequenceBarrier[] barriersToTrack) {
        return new SingleConsumerBarrier(ProducerBarrier, WaitStrategy, barriersToTrack);
    }

    /// <summary>
    /// 使用给定的等待策略创建一个【单线程消费者】使用的屏障。
    /// ps: 用户可以创建自己的自定义实例。
    /// </summary>
    /// <param name="waitStrategy">该组消费者的等待策略</param>
    /// <param name="barriersToTrack">该组消费者依赖的屏障</param>
    /// <returns>默认的消费者屏障</returns>
    ConsumerBarrier NewSingleConsumerBarrier(WaitStrategy? waitStrategy, params SequenceBarrier[] barriersToTrack) {
        if (waitStrategy == null) waitStrategy = WaitStrategy;
        return new SingleConsumerBarrier(ProducerBarrier, waitStrategy, barriersToTrack);
    }

    /// <summary>
    /// 使用默认的等待策略创建一个【多线程消费者】使用的屏障。
    /// ps: 用户可以创建自己的自定义实例。
    /// </summary>
    /// <param name="workerCount">消费者数量</param>
    /// <param name="barriersToTrack">该组消费者依赖的屏障</param>
    /// <returns>默认的消费者屏障</returns>
    ConsumerBarrier NewMultiConsumerBarrier(int workerCount, params SequenceBarrier[] barriersToTrack) {
        return new MultiConsumerBarrier(ProducerBarrier, workerCount, WaitStrategy, barriersToTrack);
    }

    /// <summary>
    ///  使用默认的等待策略创建一个【多线程消费者】使用的屏障。
    /// ps: 用户可以创建自己的自定义实例。
    /// </summary>
    /// <param name="workerCount">消费者数量</param>
    /// <param name="waitStrategy">该组消费者的等待策略</param>
    /// <param name="barriersToTrack">该组消费者依赖的屏障</param>
    /// <returns>默认的消费者屏障</returns>
    ConsumerBarrier NewMultiConsumerBarrier(int workerCount, WaitStrategy? waitStrategy, params SequenceBarrier[] barriersToTrack) {
        if (waitStrategy == null) waitStrategy = WaitStrategy;
        return new MultiConsumerBarrier(ProducerBarrier, workerCount, waitStrategy, barriersToTrack);
    }

    #endregion
}
}