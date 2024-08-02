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
using System.Linq;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 不可变List
/// 与C#系统库的ImmutableList不同，这里的实现是基于拷贝的，且不支持增删元素接口。
/// (主要解决Unity的兼容性问题)
/// (系统库的不可变集合是基于平衡树的)
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ImmutableList<T> : IList<T>, ISequencedCollection<T>
{
    private readonly T[] _elements;
    private readonly ReversedCollectionView<T> _reversed;

    private ImmutableList(T element) {
        this._elements = new[] { element };
        this._reversed = new ReversedCollectionView<T>(this);
    }

    private ImmutableList(T[] elements, bool copy = true) {
        if (elements == null) throw new ArgumentNullException(nameof(elements));
        this._elements = copy ? elements.Copy() : elements;
        this._reversed = new ReversedCollectionView<T>(this);
    }

    #region factory

    public static ImmutableList<T> Empty { get; } = new ImmutableList<T>(Array.Empty<T>());

    public static ImmutableList<T> Create(T source) {
        return new ImmutableList<T>(source);
    }

    public static ImmutableList<T> CreateRange(IEnumerable<T> source) {
        // 对于受信任的集合，不再二次拷贝
        bool trusted = source is List<T> || source is HashSet<T>
                                         || source is LinkedHashSet<T>;
        return new ImmutableList<T>(source.ToArray(), !trusted);
    }

    #endregion

    public bool IsReadOnly => true;
    public int Count => _elements.Length;
    public bool IsEmpty => _elements.Length == 0;

    public T this[int index] {
        get => _elements[index];
        set => throw new NotImplementedException();
    }

    public T PeekFirst() {
        if (_elements.Length == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        return _elements[0];
    }

    public T PeekLast() {
        if (_elements.Length == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        return _elements[_elements.Length - 1];
    }

    public bool TryPeekFirst(out T item) {
        if (_elements.Length == 0) {
            item = default;
            return false;
        }
        item = _elements[0];
        return true;
    }

    public bool TryPeekLast(out T item) {
        if (_elements.Length == 0) {
            item = default;
            return false;
        }
        item = _elements[_elements.Length - 1];
        return true;
    }

    public bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public int IndexOf(T item) {
        return Array.IndexOf(_elements, item);
    }

    public int LastIndexOf(T item) {
        return Array.LastIndexOf(_elements, item);
    }

    #region 修改接口

    public void Add(T item) {
        throw new NotImplementedException();
    }

    public bool Remove(T item) {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item) {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    public void AddFirst(T item) {
        throw new NotImplementedException();
    }

    public void AddLast(T item) {
        throw new NotImplementedException();
    }

    public T RemoveFirst() {
        throw new NotImplementedException();
    }

    public bool TryRemoveFirst(out T item) {
        throw new NotImplementedException();
    }

    public T RemoveLast() {
        throw new NotImplementedException();
    }

    public bool TryRemoveLast(out T item) {
        throw new NotImplementedException();
    }

    #endregion

    public void AdjustCapacity(int expectedCount) {
    }

    public void CopyTo(T[] array, int arrayIndex, bool reversed = false) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _elements.Length) throw new ArgumentException("Array is too small");
        if (reversed) {
            for (int i = _elements.Length - 1; i >= 0; i--) {
                array[arrayIndex++] = _elements[i];
            }
        } else {
            Array.Copy(_elements, 0, array, arrayIndex, _elements.Length);
        }
    }

    public ISequencedCollection<T> Reversed() {
        return _reversed;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator<T> ISequencedCollection<T>.GetReversedEnumerator() {
        return GetReversedEnumerator();
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this, false);
    }

    public Enumerator GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    public struct Enumerator : ISequentialEnumerator<T>
    {
        private readonly ImmutableList<T> _list;
        private readonly bool _reversed;
        private int _cursor; // 下一个元素
        private T? _current;

        public Enumerator(ImmutableList<T> list, bool reversed) {
            _list = list;
            _reversed = reversed;
            if (list.Count == 0) {
                _cursor = -1;
            } else {
                _cursor = _reversed ? _list.Count - 1 : 0;
            }
            _current = default;
        }

        public bool HasNext() {
            return _cursor >= 0;
        }

        public bool MoveNext() {
            if (_cursor < 0) {
                _current = default;
                return false;
            }
            _current = _list._elements[_cursor];
            // 需避免一直迭代，到达另一端时结束
            if (_reversed) {
                _cursor = (_cursor == 0) ? -1 : _cursor - 1;
            } else {
                _cursor = (_cursor == _list.Count) ? -1 : _cursor + 1;
            }
            return true;
        }

        public void Reset() {
            if (_list.Count == 0) {
                _cursor = -1;
            } else {
                _cursor = _reversed ? _list.Count - 1 : 0;
            }
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current => _current;

        public void Dispose() {
        }
    }
}
}