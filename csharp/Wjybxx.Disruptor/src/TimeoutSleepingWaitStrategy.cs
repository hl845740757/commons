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
/// 睡眠等待策略。
/// 在<see cref="SleepingWaitStrategy"/>的基础上增加了超时，让消费者可以从等待中醒来干其它的事情（比如处理定时任务）。
/// 
/// 1. 先尝试自旋等待一定次数。
/// 2. 然后尝试yield方式自旋一定次数。
/// 3. 然后sleep等待一定次数。
/// 4. 如果数据仍不可用，抛出<see cref="TimeoutException"/>
///
/// </summary>
public class TimeoutSleepingWaitStrategy : WaitStrategy
{
    public static TimeoutSleepingWaitStrategy Inst { get; } = new TimeoutSleepingWaitStrategy();

    private readonly int spinTries;
    private readonly int spinIterations;
    private readonly int yieldTries;
    private readonly int sleepTries;

    public TimeoutSleepingWaitStrategy()
        : this(100, 10, 100) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="spinTries">自旋次数</param>
    /// <param name="spinIterations">自旋参数</param>
    /// <param name="yieldTries">yield次数</param>
    /// <param name="sleepTries">sleep次数</param>
    public TimeoutSleepingWaitStrategy(int spinTries, int spinIterations, int yieldTries, int sleepTries = 1) {
        this.spinTries = spinTries;
        this.spinIterations = spinIterations;
        this.yieldTries = yieldTries;
        this.sleepTries = sleepTries;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        int counter = spinTries + yieldTries + sleepTries;
        int yieldThreshold = yieldTries + sleepTries;
        // windows上sleep的延迟很高，sleep(1)可能延迟16ms，不处理的话会导致不能及时调度定时任务
        long deadline = Util.SystemTickMillis() + sleepTries;

        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > yieldThreshold) {
                --counter;
                Thread.SpinWait(spinIterations);
            } else if (counter > sleepTries) {
                --counter;
                Thread.Yield();
            } else if (counter > 0) {
                --counter;

                if (deadline <= Util.SystemTickMillis()) {
                    throw StacklessTimeoutException.Inst;
                }
                Thread.Sleep(1);
            } else {
                throw StacklessTimeoutException.Inst;
            }
        }
        return availableSequence;
    }
}
}