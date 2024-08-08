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
/// 自旋等待策略
/// 特征：极低的延迟，极高的吞吐量，以及极高的CPU占用。
/// 
/// 1. 该策略通过占用CPU资源去比避免系统调用带来的延迟抖动。最好在线程能绑定到特定的CPU核心时使用。
/// 2. 会持续占用CPU资源，基本不会让出CPU资源。
/// 3. 如果你要使用该等待策略，确保有足够的CPU资源，且你能接受它带来的CPU使用率。
/// </summary>
public class BusySpinWaitStrategy : WaitStrategy
{
    public static BusySpinWaitStrategy Inst { get; } = new BusySpinWaitStrategy();

    // 入乡随俗，C#提供了一个参数，我们开放给用户
    private readonly int iterations;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iterations">用于控制SpinWait的等待时间</param>
    public BusySpinWaitStrategy(int iterations = 1) {
        this.iterations = iterations;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();
            Thread.SpinWait(iterations);
        }
        return availableSequence;
    }
}
}