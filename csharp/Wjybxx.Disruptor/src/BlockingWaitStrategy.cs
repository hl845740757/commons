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
/// 阻塞等待策略 - 可以达到较低的cpu开销。
/// 1. 通过lock等待【生产者】发布数据。
/// 2. 通过sleep等待前置消费者消费数据。
/// 3. 当吞吐量和低延迟不如CPU资源重要时，可以使用此策略。
///
/// 第二阶段未沿用Disruptor的的BusySpin模式，因为：
/// 如果前置消费者消费较慢，而后置消费者速度较快，自旋等待可能消耗较多的CPU，
/// 而Blocking策略的目的是为了降低CPU。
/// </summary>
public class BlockingWaitStrategy : WaitStrategy
{
    public static BlockingWaitStrategy Inst { get; } = new BlockingWaitStrategy();

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        SequenceBlocker blocker = producerBarrier.Blocker ?? throw new ArgumentException("blocker is null");
        // 先通过条件锁等待生产者发布数据
        if (producerBarrier.Sequence() < sequence) {
            blocker.Lock();
            try {
                while (producerBarrier.Sequence() < sequence) {
                    barrier.CheckAlert();
                    blocker.Await();
                }
            }
            finally {
                blocker.Unlock();
            }
        }
        // sleep方式等待前置消费者消费数据，C#的睡眠单位粒度太大，先尝试一定次数的yield
        int counter = 10;
        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > 0) {
                counter--;
                Thread.Yield();
            } else {
                Thread.Sleep(1);
            }
        }
        return availableSequence;
    }
}
}