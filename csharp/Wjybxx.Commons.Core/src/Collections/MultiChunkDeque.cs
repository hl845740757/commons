#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 基于多分块的无界双端队列
/// 
/// 分块的主要优点：
/// 1.可以降低扩容成本。
/// 2.可以及时回收内存。
/// 3.可以降低删除中间元素的成本。
///
/// 主要缺点：
/// 1.有一定的转发开销。
/// 2.查找或随机访问的效率低；不过，我们通常都是队首队尾操作，因此影响较小。
/// </summary>
public class MultiChunkDeque<T> : IDeque<T>
{
    private const int MinChunkSize = 4;
    private static readonly Stack<Chunk> EmptyChunkPool = new();

    private readonly int _chunkSize;
    private readonly int _poolSize;
    /** 缓存块 */
    private readonly Stack<Chunk> _chunkPool;

    /** 队首所在块 */
    private Chunk? _headChunk;
    /** 队尾所在块 */
    private Chunk? _tailChunk;
    /** 元素数量缓存 -- 无需额外版本号，每个Chunk都有版本 */
    private int _count;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chunkSize">单个块的大小</param>
    /// <param name="poolSize">Chunk池大小；0表示不缓存</param>
    public MultiChunkDeque(int chunkSize = 16, int poolSize = 4) {
        if (chunkSize < MinChunkSize) throw new ArgumentException("chunk is too small");
        if (poolSize < 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        _chunkSize = chunkSize;
        _poolSize = poolSize;
        _chunkPool = poolSize > 0 ? new Stack<Chunk>(poolSize) : EmptyChunkPool;
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    #region dequeue

    public T PeekFirst() {
        if (_headChunk == null) {
            throw CollectionUtil.CollectionEmptyException();
        }
        return _headChunk.PeekFirst();
    }

    public bool TryPeekFirst(out T item) {
        if (_headChunk == null) {
            item = default;
            return false;
        }
        return _headChunk.TryPeekFirst(out item);
    }

    public T PeekLast() {
        if (_tailChunk == null) {
            throw CollectionUtil.CollectionEmptyException();
        }
        return _tailChunk.PeekLast();
    }

    public bool TryPeekLast(out T item) {
        if (_tailChunk == null) {
            item = default;
            return false;
        }
        return _tailChunk.TryPeekLast(out item);
    }

    public void AddFirst(T item) {
        TryAddFirst(item); // 调用tryAdd减少维护代码
    }

    public void AddLast(T item) {
        TryAddLast(item);
    }

    public T RemoveFirst() {
        if (TryRemoveFirst(out T item)) { // 调用tryRemove减少维护代码
            return item;
        }
        throw CollectionUtil.CollectionEmptyException();
    }

    public T RemoveLast() {
        if (TryRemoveLast(out T item)) {
            return item;
        }
        throw CollectionUtil.CollectionEmptyException();
    }

    public bool TryAddFirst(T item) {
        Chunk headChunk = _headChunk;
        if (headChunk == null) {
            headChunk = _headChunk = _tailChunk = AllocChunk();
        } else if (headChunk.IsFull) {
            headChunk = AllocChunk();
            headChunk.next = _headChunk;
            _headChunk!.prev = headChunk;
            _headChunk = headChunk;
        }
        headChunk.AddFirst(item);
        _count++;
        return true;
    }

    public bool TryAddLast(T item) {
        Chunk tailChunk = _tailChunk;
        if (tailChunk == null) {
            tailChunk = _headChunk = _tailChunk = AllocChunk();
        } else if (tailChunk.IsFull) {
            tailChunk = AllocChunk();
            tailChunk.prev = _tailChunk;
            _tailChunk!.next = tailChunk;
            _tailChunk = tailChunk;
        }
        tailChunk.AddLast(item);
        _count++;
        return true;
    }

    public bool TryRemoveFirst(out T item) {
        Chunk headChunk = _headChunk;
        if (headChunk == null) {
            throw CollectionUtil.CollectionEmptyException();
        }
        if (headChunk.TryRemoveFirst(out item)) {
            _count--;
            if (headChunk.IsEmpty) {
                FixPointers(headChunk);
                ReleaseChunk(headChunk);
            }
            return true;
        }
        return false;
    }

    public bool TryRemoveLast(out T item) {
        Chunk tailChunk = _tailChunk;
        if (tailChunk == null) {
            throw CollectionUtil.CollectionEmptyException();
        }
        if (tailChunk.TryRemoveLast(out item)) {
            _count--;
            if (tailChunk.IsEmpty) {
                FixPointers(tailChunk);
                ReleaseChunk(tailChunk);
            }
            return true;
        }
        return false;
    }

    /** 性能差，不建议使用 */
    public bool Contains(T item) {
        for (Chunk chunk = _headChunk; chunk != null; chunk = chunk.next) {
            if (chunk.Contains(item)) {
                return true;
            }
        }
        return false;
    }

    /** 性能差，不建议使用 */
    public bool Remove(T item) {
        for (Chunk chunk = _headChunk; chunk != null; chunk = chunk.next) {
            if (!chunk.Remove(item)) {
                continue;
            }
            _count--;
            if (chunk.IsEmpty) {
                FixPointers(chunk);
                ReleaseChunk(chunk);
            }
            return true;
        }
        return false;
    }

    public void Clear() {
        if (_count <= 0) {
            return;
        }
        // 回收所有chunk
        for (Chunk chunk = _headChunk; chunk != null;) {
            Chunk nextChunk = chunk.next; // Release会清理引用，先暂存下来
            ReleaseChunk(chunk);
            chunk = nextChunk;
        }
        _headChunk = _tailChunk = null;
        _count = 0;
    }

    public void AdjustCapacity(int expectedCount) {
        // 无需响应
    }

    /// <summary>
    /// 解除chunk的引用
    /// </summary>
    /// <param name="chunk"></param>
    private void FixPointers(Chunk chunk) {
        if (_headChunk == _tailChunk) {
            Debug.Assert(chunk == _headChunk);
            _headChunk = _tailChunk = null;
        } else if (chunk == _headChunk) {
            _headChunk = chunk.next;
            _headChunk!.prev = null;
        } else if (chunk == _tailChunk) {
            _tailChunk = chunk.prev;
            _tailChunk!.next = null;
        } else {
            // 删除的是中间块
            Chunk prev = chunk.prev!;
            Chunk next = chunk.next!;
            prev.next = next;
            next.prev = prev;
        }
    }

    #endregion

    #region queue

    public void Enqueue(T item) {
        AddLast(item);
    }

    public bool TryEnqueue(T item) {
        return TryAddLast(item);
    }

    public T Dequeue() {
        return RemoveFirst();
    }

    public bool TryDequeue(out T item) {
        return TryRemoveFirst(out item);
    }

    public T PeekHead() {
        return PeekFirst();
    }

    public bool TryPeekHead(out T item) {
        return TryPeekFirst(out item);
    }

    #endregion

    #region stack

    public void Push(T item) {
        AddFirst(item);
    }

    public bool TryPush(T item) {
        return TryAddFirst(item);
    }

    public T Pop() {
        return RemoveFirst();
    }

    public bool TryPop(out T item) {
        return TryRemoveFirst(out item);
    }

    public T PeekTop() {
        return PeekFirst();
    }

    public bool TryPeekTop(out T item) {
        return TryPeekFirst(out item);
    }

    #endregion

    #region itr

    public IEnumerator<T> GetEnumerator() {
        return new DequeItr(this, false);
    }

    public IEnumerator<T> GetReversedEnumerator() {
        return new DequeItr(this, true);
    }

    public void CopyTo(T[] array, int arrayIndex, bool reversed = false) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < Count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (Chunk chunk = _tailChunk; chunk != null; chunk = chunk.prev) {
                chunk.CopyTo(array, arrayIndex, true);
                arrayIndex += chunk.Count;
            }
        } else {
            for (Chunk chunk = _headChunk; chunk != null; chunk = chunk.next) {
                chunk.CopyTo(array, arrayIndex);
                arrayIndex += chunk.Count;
            }
        }
    }

    public IDeque<T> Reversed() {
        return new ReversedDequeView<T>(this);
    }

    #endregion

    #region util

    private void ReleaseChunk(Chunk chunk) {
        chunk.Reset();
        if (_chunkPool.Count < _poolSize) {
            _chunkPool.Push(chunk);
        }
    }

    private Chunk AllocChunk() {
        Chunk chunk;
        if (_chunkPool.Count > 0) {
            chunk = _chunkPool.Pop();
        } else {
            chunk = new Chunk(_chunkSize);
        }
        return chunk;
    }

    #endregion

    private class DequeItr : IEnumerator<T>
    {
        private readonly MultiChunkDeque<T> _deque;
        private readonly bool _reversed;

        private Chunk? _chunk;
        private IEnumerator<T>? _chunkItr;

        public DequeItr(MultiChunkDeque<T> deque, bool reversed) {
            this._deque = deque;
            this._reversed = reversed;
            this.Reset();
        }

        public bool MoveNext() {
            if (_chunkItr == null) {
                return false;
            }
            if (_chunkItr.MoveNext()) {
                return true;
            }
            if (_reversed) {
                // 可能是个空块
                while (_chunk!.prev != null) {
                    _chunk = _chunk.prev;
                    _chunkItr = _chunk.GetReversedEnumerator();
                    if (_chunkItr.MoveNext()) {
                        return true;
                    }
                }
                this._chunk = null;
                this._chunkItr = null;
                return false;
            }
            while (_chunk!.next != null) {
                _chunk = _chunk.next;
                _chunkItr = _chunk.GetEnumerator();
                if (_chunkItr.MoveNext()) {
                    return true;
                }
            }
            this._chunk = null;
            this._chunkItr = null;
            return false;
        }

        public void Reset() {
            if (_deque.Count == 0) {
                this._chunk = null;
                this._chunkItr = null;
            } else if (_reversed) {
                this._chunk = _deque._tailChunk;
                this._chunkItr = _chunk!.GetReversedEnumerator();
            } else {
                this._chunk = _deque._headChunk;
                this._chunkItr = _chunk!.GetEnumerator();
            }
        }

        public T Current => _chunkItr == null ? default : _chunkItr.Current;
        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    /** 每一个Chunk是一个有界环形队列 */
    private class Chunk : BoundedArrayDeque<T>
    {
        internal Chunk? prev;
        internal Chunk? next;

        public Chunk(int length) : base(length) {
        }

        public void Reset() {
            Clear();
            // chunkIndex不立即重置，而是重用时分配
            prev = null;
            next = null;
        }
    }
}