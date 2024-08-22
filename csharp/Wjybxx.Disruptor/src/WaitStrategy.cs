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
/// 消费者等待策略
///
/// PS：由于C#只要抛出异常就会导致性能下降（即使我们的异常实现为不捕获堆栈的），因此我们使用<code>sequence-1</code>来表示超时。
/// </summary>
public interface WaitStrategy
{
    /// <summary>
    /// 等待给定的序号可用
    /// 实现类通过<see cref="ProducerBarrier.Sequence()"/>和<see cref="ConsumerBarrier.DependentSequence()"/>进行等待。
    /// </summary>
    /// <param name="sequence">期望消费的序号</param>
    /// <param name="producerBarrier">用于条件等待策略依赖策略感知生产者进度</param>
    /// <param name="barrier">消费者屏障，用于检测终止信号和查询依赖等</param>
    /// <exception cref="AlertException">如果收到了Alert信号</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns>当前可用序号，<code>sequence-1</code>表示等待超时，返回值不可以比<code>sequence -1</code>更小。</returns>
    long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier);
}
}