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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 无界双端数组队列
/// 修改自<see cref="BoundedArrayDeque{T}"/>
/// </summary>
[NotThreadSafe]
public class ArrayDeque<T> : IDeque<T>
{
    /** 元素类型是否是引用类型 */
    private static readonly bool valueIsRefType = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    private T[] _elements;
    /// <summary>
    /// 无元素的情况下head和tail都指向-1；有元素的情况下head和tail为对应的下标；
    /// 未环绕的情况下，元素数量可表示为<code>Count = tail - head + 1</code>
    /// </summary>
    private int _head;
    private int _tail;
    private int _version;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="capacity">初始容量</param>
    /// <exception cref="ArgumentException"></exception>
    public ArrayDeque(int capacity = 16) {
        if (capacity < 0) throw new ArgumentException(nameof(capacity));
        _elements = new T[Math.Max(8, capacity)]; // 初始容量不能过小，否则影响扩容逻辑
        _head = _tail = -1;
    }

    public bool IsReadOnly => false;
    public int Count => _head < 0 ? 0 : Length(_tail, _head, _elements.Length);
    public bool IsEmpty => _head < 0;
    public bool IsFull => (_tail + 1 == _head) || (_head == 0 && (_tail + 1 == +_elements.Length));

    /// <summary>
    /// 读写特定索引下的元素
    /// </summary>
    /// <param name="index">[0, Count-1]</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public T this[int index] {
        get {
            T[] elements = _elements;
            int head = _head;
            if (index < 0 || head < 0 || index >= Length(_tail, head, elements.Length)) {
                throw new IndexOutOfRangeException($"count {Count}, index {index}");
            }
            return elements[Inc(head, index, elements.Length)];
        }
        set {
            T[] elements = _elements;
            int head = _head;
            if (index < 0 || head < 0 || index >= Length(_tail, head, elements.Length)) {
                throw new IndexOutOfRangeException($"count {Count}, index {index}");
            }
            elements[Inc(head, index, elements.Length)] = value;
        }
    }

    private void Grow(int needed) {
        // Double capacity if small; else grow by 50%
        int oldCapacity = _elements.Length;
        int growUp = oldCapacity < 64 ? oldCapacity : oldCapacity >> 1;
        if (growUp < needed) {
            growUp = needed;
        }

        T[] elements = new T[oldCapacity + growUp];
        CopyTo(elements, 0);

        if (valueIsRefType) {
            Clear(); // help gc
        }
        _elements = elements;

        int count = Count; // 修正head,tail
        if (count > 0) {
            _head = 0;
            _tail = count - 1;
        }
        _version++;
    }

    private static int Length(int tail, int head, int modulus) {
        Debug.Assert(head >= 0);
        if ((tail -= head) < 0) tail += modulus;
        return tail + 1;
    }

    private static int Inc(int i, int distance, int modulus) {
        if ((i += distance) >= modulus) i -= modulus;
        return i;
    }

    private static int Inc(int i, int modulus) {
        if (++i >= modulus) i = 0;
        return i;
    }

    private static int Dec(int i, int modulus) {
        if (--i < 0) i = modulus - 1;
        return i;
    }

    #region sequence

    public T PeekFirst() {
        if (_head < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        return _elements[_head];
    }

    public T PeekLast() {
        if (_tail < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        return _elements[_tail];
    }

    public bool TryPeekFirst(out T item) {
        if (_head < 0) {
            item = default;
            return false;
        }
        item = _elements[_head];
        return true;
    }

    public bool TryPeekLast(out T item) {
        if (_tail < 0) {
            item = default;
            return false;
        }
        item = _elements[_tail];
        return true;
    }

    public void Add(T item) {
        // 队列默认添加到尾部
        if (!TryAddLast(item)) {
            throw ThrowHelper.CollectionFullException();
        }
    }

    public void AddFirst(T item) {
        if (!TryAddFirst(item)) {
            throw ThrowHelper.CollectionFullException();
        }
    }

    public void AddLast(T item) {
        if (!TryAddLast(item)) {
            throw ThrowHelper.CollectionFullException();
        }
    }

    public bool TryAddFirst(T item) {
        T[] elements = _elements;
        if (elements.Length == 0) {
            return false;
        }
        int head = _head;
        if (head >= 0) {
            head = Dec(head, elements.Length);
            if (head == _tail) {
                Grow(1); // 扩展容量，tail和head会变更，要读取最新数据
                head = Dec(_head, elements.Length);
                _tail = Dec(_tail, elements.Length);
            }
            elements[head] = item;
            _head = head;
        } else {
            elements[0] = item;
            _head = _tail = 0;
        }
        _version++;
        return true;
    }

    public bool TryAddLast(T item) {
        T[] elements = _elements;
        if (elements.Length == 0) {
            return false;
        }
        int tail = _tail;
        if (tail >= 0) {
            tail = Inc(tail, elements.Length);
            if (tail == _head) {
                Grow(1); // 扩展容量，tail和head会变更，要读取最新数据
                tail = Inc(_tail, elements.Length);
                _head = Inc(_head, elements.Length);
            }
            elements[tail] = item;
            _tail = tail;
        } else {
            elements[0] = item;
            _head = _tail = 0;
        }
        _version++;
        return true;
    }

    public T RemoveFirst() {
        if (TryRemoveFirst(out T item)) {
            return item;
        }
        throw ThrowHelper.CollectionEmptyException();
    }

    public T RemoveLast() {
        if (TryRemoveLast(out T item)) {
            return item;
        }
        throw ThrowHelper.CollectionEmptyException();
    }

    public bool TryRemoveFirst(out T item) {
        int head = _head;
        if (head < 0) {
            item = default;
            return false;
        }
        T[] elements = _elements;
        item = elements[head];
        elements[head] = default;
        if (head == _tail) {
            _head = _tail = -1;
        } else {
            _head = Inc(head, elements.Length);
        }
        _version++;
        return true;
    }

    public bool TryRemoveLast(out T item) {
        int tail = _tail;
        if (tail < 0) {
            item = default;
            return false;
        }
        T[] elements = _elements;
        item = elements[tail];
        elements[tail] = default;
        if (tail == _head) {
            _head = _tail = -1;
        } else {
            _tail = Dec(tail, elements.Length);
        }
        _version++;
        return true;
    }

    public bool Contains(T item) {
        return _head >= 0 && IndexOf(item) >= 0;
    }

    /** 性能较差，不建议调用 */
    public bool Remove(T item) {
        int index = IndexOf(item);
        if (index < 0) {
            return false;
        }
        RemoveAt(index);
        return true;
    }

    public void Clear() {
        if (_head < 0 || _elements.Length == 0) return;
        int head = _head;
        int tail = _tail;
        if (head <= tail) {
            Array.Fill(_elements, default, head, (tail - head + 1));
        } else {
            Array.Fill(_elements, default, 0, tail + 1);
            Array.Fill(_elements, default, head, _elements.Length - head);
        }
        _tail = _head = -1;
        _version++;
        Array.Fill(_elements, default);
    }

    private void RemoveAt(int index) {
        if (index == _head) {
            RemoveFirst();
            return;
        }
        if (index == _tail) {
            RemoveLast();
            return;
        }
        int head = _head;
        int tail = _tail;
        T[] elements = _elements;
        if (head < tail) { // 哪边元素少拷贝哪边
            if (index - head >= tail - index) {
                MoveFront(elements, index, tail);
            } else {
                MoveBack(elements, index, head);
            }
        } else if (index < tail) { // [0, tail - 1]，向前拷贝
            MoveFront(elements, index, tail);
        } else { // [head + 1, length-1]，向后拷贝
            Debug.Assert(index > _head);
            MoveBack(elements, index, head);
        }
        _version++;
    }

    /** 向前拷贝 -- index不能为tail，+1可能越界 */
    private void MoveFront(T[] elements, int index, int tail) {
        Array.Copy(elements, index + 1, elements, index, tail - index);
        elements[tail] = default;
        _tail = Dec(tail, elements.Length);
    }

    /** 向后拷贝 -- index不能为head，+1可能越界 */
    private void MoveBack(T[] elements, int index, int head) {
        Array.Copy(elements, head, elements, head + 1, index - head);
        elements[head] = default;
        _head = Inc(head, elements.Length);
    }

    /** 返回的是真实索引 */
    private int IndexOf(T item) {
        int head = _head;
        int tail = _tail;
        // item为null的情况不多，这可以简化代码
        IEqualityComparer<T> comparer = item == null ? NullEquality<T>.Default : EqualityComparer<T>.Default;
        if (head <= tail) {
            for (int i = head; i <= tail; i++) {
                if (comparer.Equals(item, _elements[i])) {
                    return i;
                }
            }
            return -1;
        } else {
            for (int i = head; i < _elements.Length; i++) {
                if (comparer.Equals(item, _elements[i])) {
                    return i;
                }
            }
            for (int i = 0; i <= tail; i++) {
                if (comparer.Equals(item, _elements[i])) {
                    return i;
                }
            }
            return -1;
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

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return new Enumerator(this, false);
    }

    IEnumerator<T> ISequencedCollection<T>.GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this, false);
    }

    public Enumerator GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    public void CopyTo(T[] array, int arrayIndex, bool reversed = false) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < Count) throw new ArgumentException("Array is too small");

        int head = _head;
        if (head < 0) {
            return;
        }
        int tail = _tail;
        T[] elements = _elements;
        if (head == tail) {
            array[arrayIndex] = elements[head];
            return;
        }
        if (reversed) {
            if (head <= tail) {
                for (int i = tail; i >= head; i--) {
                    array[arrayIndex++] = elements[i];
                }
            } else {
                for (int i = tail; i >= 0; i--) {
                    array[arrayIndex++] = elements[i];
                }
                for (int i = elements.Length - 1; i >= head; i--) {
                    array[arrayIndex++] = elements[i];
                }
            }
        } else {
            if (head <= tail) {
                Array.Copy(elements, head, array, arrayIndex, (tail - head + 1));
            } else {
                int headLen = (elements.Length - head);
                Array.Copy(elements, head, array, arrayIndex, headLen);
                Array.Copy(elements, 0, array, arrayIndex + headLen, (_tail + 1));
            }
        }
    }

    /// <summary>
    /// 获取Deque中的一段数据
    /// </summary>
    /// <param name="offset">偏移[0, count]</param>
    /// <param name="length">长度</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public T[] GetRange(int offset, int length) {
        if (offset < 0 || length < 0 || (offset + length > Count)) {
            throw new ArgumentException($"offset: {offset}, length: {length}, count: {Count}");
        }
        T[] elements = _elements;
        T[] result = new T[length];
        if (_head <= _tail) {
            Array.Copy(elements, _head + offset, result, 0, length);
            return result;
        }
        int start = Inc(_head, offset, elements.Length);
        int headLen = elements.Length - start;
        if (start > _tail && length > headLen) { // 需要拷贝两部分
            Array.Copy(elements, start, result, 0, headLen);
            Array.Copy(elements, 0, result, headLen, length - headLen);
        } else {
            Array.Copy(elements, start, result, 0, length);
        }
        return result;
    }

    public IDeque<T> Reversed() {
        return new ReversedDequeView<T>(this);
    }

    public void AdjustCapacity(int expectedCount) {
        int growUp = expectedCount - Count;
        if (growUp > 0) {
            Grow(growUp);
        }
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_head)}: {_head}, {nameof(_tail)}: {_tail}, {nameof(Count)}: {Count}";
    }

    public struct Enumerator : ISequentialEnumerator<T>
    {
        private readonly ArrayDeque<T> _arrayDeque;
        private readonly bool _reversed;
        private int _version;
        private int _cursor; // 下一个元素
        private T? _current;

        public Enumerator(ArrayDeque<T> arrayDeque, bool reversed) {
            _arrayDeque = arrayDeque;
            _reversed = reversed;
            _version = arrayDeque._version;

            _cursor = _reversed ? _arrayDeque._tail : _arrayDeque._head;
            _current = default;
        }

        internal bool IsNull => _arrayDeque == null;

        public bool HasNext() {
            return _cursor >= 0;
        }

        public bool MoveNext() {
            if (_version != _arrayDeque._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_cursor < 0) {
                _current = default;
                return false;
            }
            _current = _arrayDeque._elements[_cursor];
            // 需避免一直迭代，到达另一端时结束
            if (_reversed) {
                if (_cursor == _arrayDeque._head) {
                    _cursor = -1;
                } else {
                    _cursor = Dec(_cursor, _arrayDeque._elements.Length);
                }
            } else {
                if (_cursor == _arrayDeque._tail) {
                    _cursor = -1;
                } else {
                    _cursor = Inc(_cursor, _arrayDeque._elements.Length);
                }
            }
            return true;
        }

        public void Reset() {
            _cursor = _reversed ? _arrayDeque._tail : _arrayDeque._head;
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current => _current;

        public void Dispose() {
        }
    }
}
}