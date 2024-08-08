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
/// 消费者等待策略
/// </summary>
public interface WaitStrategy
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sequence">期望消费的序号</param>
    /// <param name="producerBarrier">用于条件等待策略依赖策略感知生产者进度</param>
    /// <param name="barrier">消费者屏障，用于检测终止信号和查询依赖等</param>
    /// <exception cref="AlertException">如果收到了Alert信号</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <exception cref="TimeoutException">等待超时</exception>
    /// <returns></returns>
    long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier);
}
}