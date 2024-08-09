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
public class MpUnboundedEventSequencer<T> : EventSequencer<T>
{
    private readonly MpUnboundedBuffer<T> buffer;
    private readonly MpUnboundedBufferSequencer<T> _sequencer;

    public MpUnboundedEventSequencer(Builder builder) {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        buffer = new MpUnboundedBuffer<T>(builder.Factory,
            builder.ChunkLength,
            builder.MaxPooledChunks);
        _sequencer = new MpUnboundedBufferSequencer<T>(buffer,
            builder.WaitStrategy,
            builder.Blocker);
    }

    /** buffer */
    public MpUnboundedBuffer<T> Buffer => buffer;

    /** 判断两个序号是否在同一个块 */
    public bool InSameChunk(long seq1, long seq2) {
        return buffer.InSameChunk(seq1, seq2);
    }

    /** 手动触发回收 -- 该方法可安全调用 */
    public bool TryMoveHeadToNext() {
        return buffer.TryMoveHeadToNext(_sequencer.MinimumSequence());
    }

    /** 手动触发回收，慎重调用该方法，序号错误将导致严重bug */
    public bool TryMoveHeadToNext(long gatingSequence) {
        return buffer.TryMoveHeadToNext(gatingSequence);
    }

    #region buffer

    public T Get(long sequence) {
        return buffer.Get(sequence);
    }

    public T ProducerGet(long sequence) {
        return buffer.ProducerGet(sequence);
    }

    public T ConsumerGet(long sequence) {
        return buffer.ConsumerGet(sequence);
    }

    public void ProducerSet(long sequence, T data) {
        buffer.ProducerSet(sequence, data);
    }

    public void ConsumerSet(long sequence, T data) {
        buffer.ConsumerSet(sequence, data);
    }

    public ref T ProducerGetRef(long sequence) {
        return ref buffer.ProducerGetRef(sequence);
    }

    public ref T ConsumerGetRef(long sequence) {
        return ref buffer.ConsumerGetRef(sequence);
    }

    public int Capacity => -1;

    public long RemainingCapacity => int.MaxValue;

    public Sequencer Sequencer => _sequencer;

    public ProducerBarrier ProducerBarrier => _sequencer;

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

    public static Builder NewBuilder(Func<T> factory) {
        return new Builder(factory);
    }

    public class Builder : EventSequencerBuilder<T>
    {
        private int chunkLength = 1024;
        private int maxPooledChunks = 8;

        public Builder(Func<T> factory)
            : base(factory) {
        }

#if UNITY_EDITOR
        public override EventSequencer<T> Build() {
            return new MpUnboundedEventSequencer<T>(this);
        }
#else
        public override MpUnboundedEventSequencer<T> Build() {
            return new MpUnboundedEventSequencer<T>(this);
        }
#endif

        /// <summary>
        /// 单个块大小
        /// </summary>
        public int ChunkLength {
            get => chunkLength;
            set => chunkLength = value;
        }

        /// <summary>
        /// 缓存块数量
        /// </summary>
        public int MaxPooledChunks {
            get => maxPooledChunks;
            set => maxPooledChunks = value;
        }
    }

    #endregion
}
}