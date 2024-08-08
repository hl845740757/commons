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
/// 该策略在尝试一定次数的自旋等待(空循环)之后使用尝试让出cpu。
/// 该策略将会占用大量的CPU资源(100%)，但是比{@link BusySpinWaitStrategy}策略更容易在其他线程需要CPU时让出CPU。
/// 
/// 它有着较低的延迟、较高的吞吐量，以及较高CPU占用率。当CPU数量足够时，可以使用该策略。
/// </summary>
public class YieldingWaitStrategy : WaitStrategy
{
    private readonly int spinTries;
    private readonly int spinIterations;

    public YieldingWaitStrategy() {
        this.spinTries = 100;
        this.spinIterations = 10;
    }

    public YieldingWaitStrategy(int spinTries, int spinIterations) {
        this.spinTries = spinTries;
        this.spinIterations = spinIterations;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        int counter = spinTries;
        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > 0) {
                --counter;
                Thread.SpinWait(spinIterations);
            } else {
                Thread.Yield();
            }
        }
        return availableSequence;
    }
}
}