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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 不可变List
///
/// 重复实现，用于APT项目解除依赖，打包2.0
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class ImmutableList<T> : IList<T>
{
    private readonly T[] _elements;

    private ImmutableList(T element) {
        this._elements = new[] { element };
    }

    private ImmutableList(T[] elements, bool copy = true) {
        if (elements == null) throw new ArgumentNullException(nameof(elements));
        this._elements = copy ? Util.CopyOf(elements) : elements;
    }

    #region factory

    public static ImmutableList<T> Empty { get; } = new ImmutableList<T>(Array.Empty<T>());

    public static ImmutableList<T> Create(T source) {
        return new ImmutableList<T>(source);
    }

    public static ImmutableList<T> CreateRange(IEnumerable<T> source) {
        T[] array = source as T[];
        if (array != null) {
            return new ImmutableList<T>(array, true);
        } else {
            return new ImmutableList<T>(source.ToArray(), true);
        }
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
            throw new InvalidOperationException("Collection is empty");
        }
        return _elements[0];
    }

    public T PeekLast() {
        if (_elements.Length == 0) {
            throw new InvalidOperationException("Collection is empty");
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

    #endregion

    public void CopyTo(T[] array, int arrayIndex) {
        CopyTo(array, arrayIndex, false);
    }

    public void CopyTo(T[] array, int arrayIndex, bool reversed) {
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

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return GetEnumerator();
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this, false);
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ImmutableList<T> _list;
        private readonly bool _reversed;
        private int _cursor; // 下一个元素
        private T? _current;

        public Enumerator(ImmutableList<T> list, bool reversed) {
            _list = list;
            _reversed = reversed;
            _cursor = _list.Count == 0 ? -1 : _reversed ? _list.Count - 1 : 0;
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
                _cursor = (_cursor == _list.Count - 1) ? -1 : _cursor + 1;
            }
            return true;
        }

        public void Reset() {
            _cursor = _list.Count == 0 ? -1 : _reversed ? _list.Count - 1 : 0;
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current => _current;

        public void Dispose() {
        }
    }
}
}