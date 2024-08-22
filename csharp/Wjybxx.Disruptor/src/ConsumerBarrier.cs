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

using System.Threading;

namespace Wjybxx.Disruptor
{
/// <summary>
/// 消费者序号屏障
/// 1. 消费者屏障负责的是【当前消费者和生产者】、【当前消费者和前置消费者】之间的协调。
/// 2. <see cref="SequenceBarrier.DependentSequence"/>为【生产者的生产进度】和【前置消费者消费进度】之间的最小值。
/// 3. 一个屏障关联一个<see cref="IConsumerGroup"/>，消费者组负责更新进度。
/// 4. 消费者通过屏障检测终止信号，即停止以Barrier为单位。
///
/// <h3>接口差异的缘由</h3>
/// 消费者与生产者不同，生产者明确知道自己要生产的数据数量，因此可以简单使用 tryNext(N) 申请序号；
/// 而消费者并不知道可消费的数量，盲目调度tryNext(n)来申请序号可能导致阻塞 —— 另外，实时查询可消费序号会产生极高的开销。
/// 安全的方式是每次只申请1个，但如果每次只申请一个，将大幅度限制消费者的吞吐量，会在申请序号上产生大量的开销。
/// 因此，在接口层面的<see cref="WaitFor"/>为批量查询接口，多线程消费者内部的协调由用户自身实现。
/// </summary>
public interface ConsumerBarrier : SequenceBarrier
{
    #region disruptor

    /// <summary>
    /// 等待给定的序号可消费
    ///
    /// 警告：多生产者模式下该操作十分消耗性能，如果在{@code waitFor}获取sequence之后不完全消费，
    /// 而是每次消费一点，再拉取一点，则会在该操作上形成巨大的开销 —— 极端情况是每次拉取1个，性能将差到极致。
    /// 建议的的方式：先拉取到本地，然后在本地分批处理，避免频繁调用{@code waitFor}。
    /// </summary>
    /// <param name="sequence">期望消费的序号</param>
    /// <exception cref="AlertException">如果收到了Alert信号</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns></returns>
    long WaitFor(long sequence);

    /// <summary>
    /// 【当前屏障】是否收到了Alert信号
    /// </summary>
    /// <returns></returns>
    bool IsAlerted();

    /// <summary>
    /// 通知屏障关联的生产者/消费者状态产生变化，该信号将保留至被清理 -- <see cref="ClearAlert"/>。
    /// 1. 该信号的作用类似于线程的中断信号，通常用于停止线程。
    /// 2. 用于其它目的时请慎重。
    /// </summary>
    void Alert();

    /// <summary>
    /// 清除【屏障】的alert状态。
    /// </summary>
    void ClearAlert();

    /// <summary>
    /// 检查【屏障】的的alert状态，如果收到信号，则抛出{@link AlertException}。
    /// </summary>
    /// <exception cref="AlertException">如果收到了Alert信号</exception>
    void CheckAlert();

    #endregion

    #region wjybxx

    /// <summary>
    /// 获取消费者成员的sequence
    /// (反转依赖)
    /// </summary>
    /// <param name="index">消费者索引</param>
    /// <returns></returns>
    Sequence MemberSequence(int index);

    #endregion
}
}