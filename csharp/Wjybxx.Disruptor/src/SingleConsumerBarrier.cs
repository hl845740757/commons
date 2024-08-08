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

namespace Wjybxx.Disruptor
{
public class SingleConsumerBarrier : ConsumerBarrier
{
    private readonly ProducerBarrier producerBarrier;
    private readonly WaitStrategy waitStrategy;

    private readonly Sequence _groupSequence = new Sequence(SequenceBarrier.INITIAL_SEQUENCE);
    private readonly SequenceBarrier[] dependentBarriers;
    private volatile bool alerted = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="producerBarrier">生产者屏障</param>
    /// <param name="waitStrategy">该组消费者的等待策略</param>
    /// <param name="dependentBarriers">依赖的屏障</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SingleConsumerBarrier(ProducerBarrier producerBarrier,
                                 WaitStrategy waitStrategy,
                                 params SequenceBarrier[] dependentBarriers) {
        if (producerBarrier == null) throw new ArgumentNullException(nameof(producerBarrier));
        if (waitStrategy == null) throw new ArgumentNullException(nameof(waitStrategy));
        Util.CheckNullElements(dependentBarriers, "dependentBarriers");
        // 如果未显式指定前置依赖，则添加生产者依赖
        if (dependentBarriers.Length == 0) {
            dependentBarriers = new SequenceBarrier[1];
            dependentBarriers[0] = producerBarrier;
        }
        this.producerBarrier = producerBarrier;
        this.waitStrategy = waitStrategy;
        this.dependentBarriers = dependentBarriers;
    }

    #region consumer

    public long WaitFor(long sequence) {
        CheckAlert();

        // available是生产者或前置消费者的进度
        long cursorSequence = waitStrategy.WaitFor(sequence, producerBarrier, this);
        if (cursorSequence < sequence) {
            return cursorSequence;
        }
        // 只要依赖可能包含生产者，都需要检查数据的连续性
        return producerBarrier.GetHighestPublishedSequence(sequence, cursorSequence);
    }

    public bool IsAlerted() {
        return alerted;
    }

    public void Alert() {
        alerted = true;
        producerBarrier.SignalAllWhenBlocking();
    }

    public void ClearAlert() {
        alerted = false;
    }

    public void CheckAlert() {
        if (alerted) {
            throw AlertException.Inst;
        }
    }

    #endregion

    #region barrier

    public void Claim(long sequence) {
        _groupSequence.SetRelease(sequence);
    }

    public Sequence MemberSequence(int index) {
        if (index != 0) throw new IndexOutOfRangeException(index.ToString());
        return _groupSequence;
    }

    public Sequence GroupSequence() {
        return _groupSequence;
    }

    public long Sequence() {
        return _groupSequence.GetVolatile();
    }

    public long DependentSequence() {
        return Util.GetMinimumSequence(dependentBarriers);
    }

    public long MinimumSequence() {
        return Util.GetMinimumSequence(dependentBarriers, _groupSequence.GetVolatile());
    }

    public void AddDependentBarriers(params SequenceBarrier[] barriersToTrack) {
        throw new NotImplementedException();
    }

    public bool RemoveDependentBarrier(SequenceBarrier barrier) {
        return false;
    }

    #endregion
}
}