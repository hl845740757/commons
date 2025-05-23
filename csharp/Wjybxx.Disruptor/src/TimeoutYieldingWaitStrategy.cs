#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
/// Yield等待策略
/// 在<see cref="YieldingWaitStrategy"/>的基础上增加了超时，让消费者可以从等待中醒来干其它的事情（比如处理定时任务）。
/// 
/// 1. 先尝试自旋等待一定次数。
/// 2. 然后尝试yield方式自旋一定次数。
/// 3.如果数据仍不可用，返回超时（sequence-1）。
/// </summary>
public class TimeoutYieldingWaitStrategy : WaitStrategy
{
    public static readonly TimeoutYieldingWaitStrategy Inst = new TimeoutYieldingWaitStrategy();

    private readonly int spinTries;
    private readonly int spinIterations;
    private readonly int yieldTries;

    public TimeoutYieldingWaitStrategy() {
        this.spinTries = 100;
        this.spinIterations = 1;
        this.spinTries = 10;
    }

    public TimeoutYieldingWaitStrategy(int spinTries, int spinIterations,
                                       int yieldTries) {
        this.spinTries = spinTries;
        this.spinIterations = spinIterations;
        this.yieldTries = yieldTries;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        int counter = spinTries + yieldTries;
        int yieldThreshold = yieldTries;

        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > yieldThreshold) {
                --counter;
                Thread.SpinWait(spinIterations);
            } else if (counter > 0) {
                --counter;
                Thread.Yield();
            } else {
                return sequence - 1;
            }
        }
        return availableSequence;
    }
}
}