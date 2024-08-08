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
/// 表现：延迟不均匀，吞吐量较低，但是cpu占有率也较低。
/// 算是CPU与性能之间的一个折中，当CPU资源紧张时可以考虑使用该策略。
/// 
/// 1. 先尝试自旋等待一定次数。
/// 2. 然后尝试yield方式自旋一定次数。
/// 3. 然后sleep等待。
///
/// ps: 由于C#的最小睡眠单位为毫秒，因此不需要配置每次的睡眠时间，每次睡眠1毫秒影响已经很大了。
/// </summary>
public class SleepingWaitStrategy : WaitStrategy
{
    private readonly int spinTries;
    private readonly int spinIterations;
    private readonly int yieldTries;

    public SleepingWaitStrategy() {
        this.spinTries = 100;
        this.spinIterations = 10;
        this.yieldTries = 100;
    }

    public SleepingWaitStrategy(int spinTries, int spinIterations, int yieldTries) {
        this.spinTries = spinTries;
        this.spinIterations = spinIterations;
        this.yieldTries = yieldTries;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        int counter = spinTries + yieldTries;
        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > yieldTries) {
                --counter;
                Thread.SpinWait(spinIterations);
            } else if (counter > 0) {
                --counter;
                Thread.Yield();
            } else {
                Thread.Sleep(1);
            }
        }
        return availableSequence;
    }
}
}