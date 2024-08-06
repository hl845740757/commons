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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 参考自Netty
/// </summary>
/// <typeparam name="T"></typeparam>
public class BetterIndexedPriorityQueue<T> : IIndexedPriorityQueue<T> where T : class
{
    private readonly IComparer<T> _comparator;
    private readonly IIndexedElementHelper<T> _helper;

    private T[] _items;
    private int _count;

    public BetterIndexedPriorityQueue(IComparer<T> comparator, IIndexedElementHelper<T> helper, int initCapacity = 11) {
        this._comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
        this._helper = helper ?? throw new ArgumentNullException(nameof(helper));
        this._items = new T[initCapacity];
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    public void Clear() {
        for (int i = 0; i < _count; i++) {
            var item = _items[i];
            if (item == null) {
                continue;
            }
            _helper.CollectionIndex(this, item, -1);
            _items[i] = null!;
        }
        _count = 0;
    }

    public void ClearIgnoringIndexes() {
        for (int i = 0; i < _count; i++) {
            _items[i] = null!;
        }
        _count = 0;
    }

    public bool Contains(T item) {
        if (item == null) {
            return false;
        }
        return Contains(item, _helper.CollectionIndex(this, item));
    }

    #region queue

    public void Add(T item) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        TryEnqueue(item);
    }

    public void Enqueue(T item) {
        TryEnqueue(item);
    }

    public T Dequeue() {
        if (TryDequeue(out T item)) {
            return item;
        }
        throw new InvalidOperationException("Queue empty");
    }

    public bool TryEnqueue(T item) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (_helper.CollectionIndex(this, item) != -1) {
            throw new InvalidOperationException($"item.Index: {_helper.CollectionIndex(this, item)}, expected: -1");
        }
        if (_count >= _items.Length) {
            int grow = (_items.Length < 64) ? (_items.Length + 2) : (_items.Length >> 1);
            Resize(grow + _items.Length);
        }
        BubbleUp(_count++, item);
        return true;
    }

    public bool TryDequeue(out T item) {
        if (_count == 0) {
            item = default!;
            return false;
        }
        item = _items[0];
        RemoveAt(item, 0);
        return true;
    }

    public T PeekHead() {
        if (_count == 0) {
            throw new InvalidOperationException("Collection is empty");
        }
        return _items[0];
    }

    public bool TryPeekHead(out T item) {
        if (_count == 0) {
            item = default!;
            return false;
        }
        item = _items[0];
        return true;
    }

    #endregion

    public bool Remove(T item) {
        if (item == null) {
            return false;
        }
        int idx = _helper.CollectionIndex(this, item);
        if (Contains(item, idx)) {
            RemoveAt(item, idx);
            return true;
        }
        return false;
    }

    public void PriorityChanged(T node) {
        if (node == null) {
            throw new ArgumentNullException(nameof(node));
        }
        int idx = _helper.CollectionIndex(this, node);
        if (!Contains(node, idx)) {
            return;
        }

        if (idx == 0) {
            BubbleDown(idx, node);
        } else {
            int iParent = (idx - 1) >> 1;
            T parent = _items[iParent];
            if (_comparator.Compare(node, parent) < 0) {
                BubbleUp(idx, node);
            } else {
                BubbleDown(idx, node);
            }
        }
    }

    public void AdjustCapacity(int expectedCount) {
        if (expectedCount < _count) throw new ArgumentException(nameof(expectedCount));
        int delta = expectedCount - _items.Length;
        if (delta == 0) {
            return;
        }
        if (delta < 0) {
            // 避免不必要的收缩
            if (-delta >= 8) {
                Resize(expectedCount);
            }
        } else {
            // 避免过小的扩容
            if (delta < 4) {
                delta = 4;
            }
            Resize(_items.Length + delta);
        }
    }

    #region itr

    public void CopyTo(T[] array, int arrayIndex) {
        if (_count == 0) {
            return;
        }
        // 暂时未实现有序迭代，Copy时也不保证顺序
        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return GetEnumerator();
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this);
    }

    #endregion

    #region Internal

    private void Resize(int newSize) {
        Debug.Assert(newSize >= _count);
        Array.Resize(ref _items, newSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Contains(T item, int idx) {
        return idx >= 0 && idx < _count && ReferenceEquals(item, _items[idx]);
    }

    private void RemoveAt(T item, int idx) {
        _helper.CollectionIndex(this, item, -1);

        int newSize = --_count;
        if (newSize == idx) { // 如果删除的是最后一个元素则无需交换
            _items[idx] = default!;
            return;
        }

        T moved = _items[idx] = _items[newSize];
        _items[newSize] = default!;

        if (idx == 0 || _comparator.Compare(item, moved) < 0) {
            BubbleDown(idx, moved);
        } else {
            BubbleUp(idx, moved);
        }
    }

    private void BubbleDown(int k, T node) {
        T[] items = _items;
        int half = _count >> 1;
        while (k < half) {
            int iChild = (k << 1) + 1;
            T child = items[iChild];

            // 找到最小的子节点，如果父节点大于最小子节点，则与最小子节点交换
            int iRightChild = iChild + 1;
            if (iRightChild < _count && _comparator.Compare(child, items[iRightChild]) > 0) {
                child = items[iChild = iRightChild];
            }
            if (_comparator.Compare(node, child) <= 0) {
                break;
            }

            items[k] = child;
            _helper.CollectionIndex(this, child, k);

            k = iChild;
        }

        items[k] = node;
        _helper.CollectionIndex(this, node, k);
    }

    private void BubbleUp(int k, T node) {
        T[] items = _items;
        while (k > 0) {
            int iParent = (k - 1) >> 1;
            T parent = items[iParent];

            // 如果node小于父节点，则node要与父节点进行交换
            if (_comparator.Compare(node, parent) >= 0) {
                break;
            }

            items[k] = parent;
            _helper.CollectionIndex(this, parent, k);

            k = iParent;
        }

        items[k] = node;
        _helper.CollectionIndex(this, node, k);
    }

    /** 这里暂没有按照优先级迭代，实现较为麻烦；由于未有序迭代，这里也没支持删除 */
    public struct Enumerator : ISequentialEnumerator<T>
    {
        private readonly BetterIndexedPriorityQueue<T> _queue;
        private int _index;
        private T? _current;

        public Enumerator(BetterIndexedPriorityQueue<T> queue) {
            _queue = queue;
            _index = -1;
            _current = null;
        }

        public bool HasNext() {
            return _index + 1 < _queue._count;
        }

        public bool MoveNext() {
            if (_index + 1 < _queue._count) {
                _index++;
                _current = _queue._items[_index];
                return true;
            }
            return false;
        }

        public void Reset() {
            _index = -1;
        }

        public T Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion
}
}