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
using System.Runtime.CompilerServices;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 参考自Netty
/// </summary>
/// <typeparam name="T"></typeparam>
public class IndexedPriorityQueue<T> : IIndexedPriorityQueue<T> where T : class, IIndexedElement
{
    private readonly IComparer<T> _comparator;
    private T[] _items;
    private int _count;

    public IndexedPriorityQueue(IComparer<T> comparator, int initCapacity = 11) {
        this._comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
        this._items = new T[initCapacity];
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    public void Clear() {
        for (var i = 0; i < _count; i++) {
            var item = _items[i];
            if (item == null) {
                continue;
            }
            item.CollectionIndex(this, -1);
            _items[i] = null!;
        }
        _count = 0;
    }

    public void ClearIgnoringIndexes() {
        for (var i = 0; i < _count; i++) {
            _items[i] = null!;
        }
        _count = 0;
    }

    public bool Contains(T item) {
        if (item == null) {
            return false;
        }
        return Contains(item, item.CollectionIndex(this));
    }

    #region queue

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
        if (item.CollectionIndex(this) != -1) { // NPE
            throw new InvalidOperationException($"item.Index: {item.CollectionIndex(this)}, expected: -1");
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
        int idx = item.CollectionIndex(this);
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
        int idx = node.CollectionIndex(this);
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

    public IEnumerator<T> GetEnumerator() {
        return new Itr(this);
    }

    #endregion

    #region Internal

    private void Resize(int newSize) {
        Debug.Assert(newSize >= _count);
        Array.Resize(ref _items, newSize);
    }

    private bool Contains(T item, int idx) {
        return idx >= 0 && idx < _count && ReferenceEquals(item, _items[idx]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetNodeIndex(T item, int idx) {
        item.CollectionIndex(this, idx);
        Debug.Assert(item.CollectionIndex(this) == idx, "item.Index != idx");
    }

    private void RemoveAt(T item, int idx) {
        SetNodeIndex(item, -1);

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
        int half = _count >> 1;
        while (k < half) {
            int iChild = (k << 1) + 1;
            T child = _items[iChild];

            // 找到最小的子节点，如果父节点大于最小子节点，则与最小子节点交换
            int iRightChild = iChild + 1;
            if (iRightChild < _count && _comparator.Compare(child, _items[iRightChild]) > 0) {
                child = _items[iChild = iRightChild];
            }
            if (_comparator.Compare(node, child) <= 0) {
                break;
            }

            _items[k] = child;
            SetNodeIndex(child, k);

            k = iChild;
        }

        _items[k] = node;
        SetNodeIndex(node, k);
    }

    private void BubbleUp(int k, T node) {
        while (k > 0) {
            int iParent = (k - 1) >> 1;
            T parent = _items[iParent];

            // 如果node小于父节点，则node要与父节点进行交换
            if (_comparator.Compare(node, parent) >= 0) {
                break;
            }

            _items[k] = parent;
            SetNodeIndex(parent, k);

            k = iParent;
        }

        _items[k] = node;
        SetNodeIndex(node, k);
    }

    /** 这里暂没有按照优先级迭代，实现较为麻烦；由于未有序迭代，这里也没支持删除 */
    private class Itr : IEnumerator<T>
    {
        private readonly IndexedPriorityQueue<T> _queue;
        private int _index = -1;
        private T? _current;

        public Itr(IndexedPriorityQueue<T> queue) {
            _queue = queue;
            _current = null;
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