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
public class RingBufferEventSequencer<T> : EventSequencer<T>
{
    private readonly RingBuffer<T> buffer;
    private readonly RingBufferSequencer _sequencer;

    private RingBufferEventSequencer(Builder builder) {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        this.buffer = new RingBuffer<T>(builder.Factory, builder.BufferLength);
        if (builder.ProducerType == ProducerType.Multi) {
            _sequencer = new MultiProducerSequencer(
                builder.BufferLength,
                builder.ProducerSpinIterations,
                builder.WaitStrategy,
                builder.Blocker);
        } else {
            _sequencer = new SingleProducerSequencer(
                builder.BufferLength,
                builder.ProducerSpinIterations,
                builder.WaitStrategy,
                builder.Blocker);
        }
    }

    public RingBuffer<T> Buffer => buffer;

    #region buffer

    public T Get(long sequence) {
        return buffer.GetElement(sequence);
    }

    public T ProducerGet(long sequence) {
        return buffer.GetElement(sequence);
    }

    public T ConsumerGet(long sequence) {
        return buffer.GetElement(sequence);
    }

    public void ProducerSet(long sequence, T data) {
        buffer.SetElement(sequence, data);
    }

    public void ConsumerSet(long sequence, T data) {
        buffer.SetElement(sequence, data);
    }

    public ref T ProducerGetRef(long sequence) {
        return ref buffer.GetElementRef(sequence);
    }

    public ref T ConsumerGetRef(long sequence) {
        return ref buffer.GetElementRef(sequence);
    }

    public int Capacity => buffer.BufferLength;

    public long RemainingCapacity => _sequencer.RemainingCapacity();

    public Sequencer Sequencer => _sequencer;

    public ProducerBarrier ProducerBarrier => _sequencer;

    public DataProvider<T> DataProvider => buffer;

    #endregion

    #region producer

    public bool HasAvailableCapacity(int requiredCapacity) {
        return _sequencer.HasAvailableCapacity(requiredCapacity);
    }

    public long Next() {
        return _sequencer.Next(1); // 传入1可减少调用
    }

    public long Next(int n) {
        return _sequencer.Next(n);
    }

    public long? TryNext() {
        return _sequencer.TryNext(1);
    }

    public long? TryNext(int n) {
        return _sequencer.TryNext(n);
    }

    public long NextInterruptibly() {
        return _sequencer.NextInterruptibly(1);
    }

    public long NextInterruptibly(int n) {
        return _sequencer.NextInterruptibly(n);
    }

    public long? TryNext(int n, TimeSpan timeout) {
        return _sequencer.TryNext(n, timeout);
    }

    public void Publish(long sequence) {
        _sequencer.Publish(sequence);
    }

    public void Publish(long lo, long hi) {
        _sequencer.Publish(lo, hi);
    }

    #endregion

    #region builder

    /** 多线程生产者builder */
    public static Builder NewMultiProducer(Func<T> factory) {
        return new Builder(factory)
        {
            ProducerType = ProducerType.Multi
        };
    }

    /** 单线程生产者builder */
    public static Builder NewSingleProducer(Func<T> factory) {
        return new Builder(factory)
        {
            ProducerType = ProducerType.Single
        };
    }

    public class Builder : EventSequencerBuilder<T>
    {
        private ProducerType producerType = ProducerType.Multi;
        private int bufferLength = 8192;

        public Builder(Func<T> factory) : base(factory) {
        }

#if NET5_0_OR_GREATER
        public override RingBufferEventSequencer<T> Build() {
            return new RingBufferEventSequencer<T>(this);
        }
#else
        public override EventSequencer<T> Build() {
            return new RingBufferEventSequencer<T>(this);
        }
#endif

        /// <summary>
        /// 生产者的类型
        /// </summary>
        public ProducerType ProducerType {
            get => producerType;
            set => producerType = value;
        }

        /// <summary>
        /// 环形缓冲区的大小
        /// </summary>
        public int BufferLength {
            get => bufferLength;
            set => bufferLength = value;
        }
    }

    #endregion
}
}