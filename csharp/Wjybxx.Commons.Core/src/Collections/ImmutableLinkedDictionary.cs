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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 保持插入序的不可变字典
/// 与C#系统库的ImmutableDictionary，这里的实现是基于拷贝的，且不支持增删元素；另外这里保持了元素的插入顺序
/// (主要解决Unity的兼容性问题)
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[Serializable]
[Immutable]
public sealed class ImmutableLinkedDictionary<TKey, TValue> : ISequencedDictionary<TKey, TValue>
{
    /** len = 2^n + 1，额外的槽用于存储nullKey */
    private readonly Node[] _table;
    private readonly int _head;
    private readonly int _tail;

    /** 有效元素数量 */
    private readonly int _count;
    private readonly int _mask;
    private readonly IEqualityComparer<TKey> _keyComparer;

    private KeyCollection? _keys;
    private ValueCollection? _values;
    private ReversedDictionaryView<TKey, TValue>? _reversed;

    private ImmutableLinkedDictionary(KeyValuePair<TKey, TValue>[] pairArray, IEqualityComparer<TKey>? keyComparer = null) {
        if (keyComparer == null) {
            keyComparer = EqualityComparer<TKey>.Default;
        }
        this._keyComparer = keyComparer;

        if (pairArray.Length == 0) {
            _table = Array.Empty<Node>();
            _head = _tail = -1;
            _count = 0;
            _mask = 0;
            return;
        }

        _count = pairArray.Length;
        _mask = HashCommon.ArraySize(pairArray.Length, 0.75f) - 1;
        _table = new Node[_mask + 2];

        // 插入的同时构建双向链表
        int preNodePos = -1;
        foreach (KeyValuePair<TKey, TValue> pair in pairArray) {
            TKey key = pair.Key;
            int hash = KeyHash(key, keyComparer);
            int pos = Find(key, hash);
            if (pos >= 0) {
                throw new ArgumentException("duplicate element: " + key);
            }
            pos = -pos - 1;
            _table[pos] = new Node(hash, key, pair.Value, pos, preNodePos);

            if (preNodePos != -1) {
                ref Node preNode = ref _table[preNodePos];
                preNode.next = pos;
            }
            preNodePos = pos;
        }

        KeyValuePair<TKey, TValue> firstPair = pairArray[0];
        KeyValuePair<TKey, TValue> lastPair = pairArray[_count - 1];
        _head = Find(firstPair.Key, KeyHash(firstPair.Key, keyComparer));
        _tail = Find(lastPair.Key, KeyHash(lastPair.Key, keyComparer));
    }

    #region factory

    public static ImmutableLinkedDictionary<TKey, TValue> Empty { get; } = new(Array.Empty<KeyValuePair<TKey, TValue>>());

    public static ImmutableLinkedDictionary<TKey, TValue> Create(TKey key, TValue value,
                                                                 IEqualityComparer<TKey>? keyComparer = null) {
        KeyValuePair<TKey, TValue>[] array = new[] { new KeyValuePair<TKey, TValue>(key, value) };
        return new ImmutableLinkedDictionary<TKey, TValue>(array, keyComparer);
    }

    public static ImmutableLinkedDictionary<TKey, TValue> CreateRange(IEnumerable<KeyValuePair<TKey, TValue>> source,
                                                                      IEqualityComparer<TKey>? keyComparer = null) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        KeyValuePair<TKey, TValue>[] array = source as KeyValuePair<TKey, TValue>[];
        if (array == null) {
            array = source.ToArray();
        }
        return array.Length == 0 ? Empty : new ImmutableLinkedDictionary<TKey, TValue>(array, keyComparer);
    }

    #endregion

    public bool IsReadOnly => true;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    public IGenericCollection<TKey> Keys => CachedKeys();
    public IGenericCollection<TValue> Values => CachedValues();
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => CachedKeys();
    ICollection<TValue> IDictionary<TKey, TValue>.Values => CachedValues();
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => CachedKeys();
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => CachedValues();

    public ISequencedCollection<TKey> SequencedKeys(bool reversed = false) => CachedKeys(reversed);

    public ISequencedCollection<TValue> SequencedValues(bool reversed = false) => CachedValues(reversed);

    private KeyCollection CachedKeys(bool reversed = false) {
        if (reversed) {
            return new KeyCollection(this, true);
        }
        if (_keys == null) {
            _keys = new KeyCollection(this, false);
        }
        return _keys;
    }

    private ValueCollection CachedValues(bool reversed = false) {
        if (reversed) {
            return new ValueCollection(this, true);
        }
        if (_values == null) {
            _values = new ValueCollection(this, false);
        }
        return _values;
    }

    public TValue this[TKey key] {
        get {
            int index = Find(key, KeyHash(key, _keyComparer));
            if (index < 0) {
                throw ThrowHelper.KeyNotFoundException(key);
            }
            ref Node node = ref _table[index];
            return node.value;
        }
        set => throw new NotImplementedException();
    }

    #region peek

    public KeyValuePair<TKey, TValue> PeekFirst() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.AsPair();
    }

    public bool TryPeekFirst(out KeyValuePair<TKey, TValue> pair) {
        if (_count == 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[_head];
        pair = node.AsPair();
        return true;
    }

    public KeyValuePair<TKey, TValue> PeekLast() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.AsPair();
    }

    public bool TryPeekLast(out KeyValuePair<TKey, TValue> pair) {
        if (_count == 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[_tail];
        pair = node.AsPair();
        return true;
    }

    public TKey PeekFirstKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.key;
    }

    public bool TryPeekFirstKey(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[_head];
        key = node.key;
        return true;
    }

    public TKey PeekLastKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.key;
    }

    public bool TryPeekLastKey(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[_tail];
        key = node.key;
        return true;
    }

    #endregion

    #region contains/get

    public bool ContainsKey(TKey key) {
        return Find(key, KeyHash(key, _keyComparer)) >= 0;
    }

    public bool ContainsValue(TValue value) {
        if (value == null) {
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                if (e.value == null) {
                    return true;
                }
                index = e.next;
            }
            return false;
        } else {
            IEqualityComparer<TValue>? valComparer = ValComparer;
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                if (valComparer.Equals(value, e.value)) {
                    return true;
                }
                index = e.next;
            }
            return false;
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        int index = Find(item.Key, KeyHash(item.Key, _keyComparer));
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        return ValComparer.Equals(item.Value, node.value);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        value = node.value;
        return true;
    }

    #endregion

    #region 修改接口

    public void Add(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public bool Remove(TKey key) {
        throw new NotImplementedException();
    }

    public bool TryAdd(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public PutResult<TValue> Put(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public bool Remove(TKey key, out TValue value) {
        throw new NotImplementedException();
    }

    public KeyValuePair<TKey, TValue> RemoveFirst() {
        throw new NotImplementedException();
    }

    public bool TryRemoveFirst(out KeyValuePair<TKey, TValue> item) {
        throw new NotImplementedException();
    }

    public KeyValuePair<TKey, TValue> RemoveLast() {
        throw new NotImplementedException();
    }

    public bool TryRemoveLast(out KeyValuePair<TKey, TValue> item) {
        throw new NotImplementedException();
    }

    public void AddFirst(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public bool TryAddFirst(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public void AddLast(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public bool TryAddLast(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public PutResult<TValue> PutFirst(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public PutResult<TValue> PutLast(TKey key, TValue value) {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    #endregion

    #region sp

    /// <summary>
    /// 查询指定键的后一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="next">接收下一个键</param>
    /// <returns>如果下一个key存在则返回true</returns>
    /// <exception cref="ThrowHelper.KeyNotFoundException">如果当前键不存在</exception>
    public bool NextKey(TKey key, out TKey next) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.next < 0) {
            next = default;
            return false;
        }
        ref Node nextNode = ref _table[node.next];
        next = nextNode.key;
        return true;
    }

    /// <summary>
    /// 查询指定键的前一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="prev">接收前一个键</param>
    /// <returns>如果前一个key存在则返回true</returns>
    /// <exception cref="ThrowHelper.KeyNotFoundException">如果当前键不存在</exception>
    public bool PrevKey(TKey key, out TKey prev) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.prev < 0) {
            prev = default;
            return false;
        }
        ref Node nextNode = ref _table[node.prev];
        prev = nextNode.key;
        return true;
    }

    public void AdjustCapacity(int expectedCount) {
    }

    #endregion

    #region copyto

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, bool reversed = false) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");
        if (_count == 0) {
            return;
        }
        if (reversed) {
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.AsPair();
                index = e.prev;
            }
        } else {
            for (int index = _head; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.AsPair();
                index = e.next;
            }
        }
    }

    public void CopyKeysTo(TKey[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.key;
                index = e.prev;
            }
        } else {
            for (int index = _head; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.key;
                index = e.next;
            }
        }
    }

    public void CopyValuesTo(TValue[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.value;
                index = e.prev;
            }
        } else {
            for (int index = _head; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.value;
                index = e.next;
            }
        }
    }

    #endregion

    #region itr

    public ISequencedDictionary<TKey, TValue> Reversed() {
        if (_reversed == null) {
            _reversed = new ReversedDictionaryView<TKey, TValue>(this);
        }
        return _reversed;
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator<KeyValuePair<TKey, TValue>> ISequencedCollection<KeyValuePair<TKey, TValue>>.GetReversedEnumerator() {
        return GetReversedEnumerator();
    }

    public PairEnumerator GetEnumerator() {
        return new PairEnumerator(this, false);
    }

    public PairEnumerator GetReversedEnumerator() {
        return new PairEnumerator(this, true);
    }

    #endregion

    #region core

    private static IEqualityComparer<TValue> ValComparer => EqualityComparer<TValue>.Default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int KeyHash(TKey? key, IEqualityComparer<TKey> keyComparer) {
        return key == null ? 0 : HashCommon.Mix(keyComparer.GetHashCode(key));
    }

    /// <summary>
    /// 如果key存在，则返回对应的下标(大于等于0)；
    /// 如果key不存在，则返回其hash应该存储的下标的负值再减1，以识别0 -- 或者说 下标 +1 再取相反数。
    /// 该方法只有增删方法元素方法可调用，会导致初始化空间
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hash">key的hash值</param>
    /// <returns></returns>
    private int Find(TKey key, int hash) {
        Node[] table = _table;
        if (table.Length == 0) {
            return -1;
        }
        if (key == null) {
            Node nullNode = table[_mask + 1];
            return nullNode.index == null ? -(_mask + 2) : (_mask + 1);
        }

        IEqualityComparer<TKey> keyComparer = _keyComparer;
        int mask = _mask;
        // 先测试无冲突位置
        int pos = mask & hash;
        ref Node node = ref table[pos];
        if (node.index == null) return -(pos + 1);
        if (node.hash == hash && keyComparer.Equals(node.key, key)) {
            return pos;
        }
        // 线性探测
        // 注意：为了利用空间，线性探测需要在越界时绕回到数组首部(mask取余绕回)；'i'就是探测次数
        // 由于数组满时一定会触发扩容，可保证这里一定有一个槽为null；如果循环一圈失败，上次扩容失败被捕获？
        for (int i = 0; i < mask; i++) {
            pos = (pos + 1) & mask;
            node = ref table[pos];
            if (node.index == null) return -(pos + 1);
            if (node.hash == hash && keyComparer.Equals(node.key, key)) {
                return pos;
            }
        }
        throw new InvalidOperationException("state error");
    }

    #endregion

    #region view

    private abstract class AbstractViewCollection<T> : ISequencedCollection<T>
    {
        protected readonly ImmutableLinkedDictionary<TKey, TValue> _dictionary;
        protected readonly bool _reversed;

        protected AbstractViewCollection(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _dictionary = dictionary;
            _reversed = reversed;
        }

        #region 查询

        public virtual bool IsReadOnly => true;
        public int Count => _dictionary.Count;
        public bool IsEmpty => _dictionary.IsEmpty;

        public abstract bool Contains(T item);

        public abstract bool TryPeekFirst(out T item);

        public abstract T PeekFirst();

        public abstract bool TryPeekLast(out T item);

        public abstract T PeekLast();

        #endregion

        #region itr

        public abstract ISequencedCollection<T> Reversed();

        public abstract IEnumerator<T> GetEnumerator();

        public abstract IEnumerator<T> GetReversedEnumerator();

        public abstract void CopyTo(T[] array, int arrayIndex, bool reversed = false);

        #endregion

        #region modify

        public void AdjustCapacity(int expectedCount) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual void Add(T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual void AddFirst(T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual void AddLast(T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual T RemoveFirst() {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual bool TryRemoveFirst(out T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual T RemoveLast() {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual bool TryRemoveLast(out T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual bool Remove(T item) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public virtual void Clear() {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        #endregion
    }

    private class KeyCollection : AbstractViewCollection<TKey>, ISequencedCollection<TKey>
    {
        public KeyCollection(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed)
            : base(dictionary, reversed) {
        }

        public override TKey PeekFirst() => _reversed ? _dictionary.PeekLastKey() : _dictionary.PeekFirstKey();

        public override TKey PeekLast() => _reversed ? _dictionary.PeekFirstKey() : _dictionary.PeekLastKey();

        public override bool TryPeekFirst(out TKey item) {
            return _reversed ? _dictionary.TryPeekLastKey(out item) : _dictionary.TryPeekFirstKey(out item);
        }

        public override bool TryPeekLast(out TKey item) {
            return _reversed ? _dictionary.TryPeekFirstKey(out item) : _dictionary.TryPeekLastKey(out item);
        }

        public override bool Contains(TKey item) {
            return _dictionary.ContainsKey(item);
        }

        public override void CopyTo(TKey[] array, int arrayIndex, bool reversed = false) {
            _dictionary.CopyKeysTo(array, arrayIndex, _reversed ^ reversed);
        }

        public override ISequencedCollection<TKey> Reversed() {
            return _dictionary.CachedKeys(_reversed);
        }

        public override IEnumerator<TKey> GetEnumerator() {
            return new KeyEnumerator(_dictionary, _reversed);
        }

        public override IEnumerator<TKey> GetReversedEnumerator() {
            return new KeyEnumerator(_dictionary, !_reversed);
        }
    }

    private class ValueCollection : AbstractViewCollection<TValue>
    {
        public ValueCollection(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed)
            : base(dictionary, reversed) {
        }

        private TValue CheckNodeValue(int index) {
            if (index < 0) throw ThrowHelper.CollectionEmptyException();
            ref Node node = ref _dictionary._table[index];
            return node.value;
        }

        private bool PeekNodeValue(int index, out TValue value) {
            if (index < 0) {
                value = default;
                return false;
            }
            ref Node node = ref _dictionary._table[index];
            value = node.value;
            return true;
        }

        public override TValue PeekFirst() => _reversed ? CheckNodeValue(_dictionary._tail) : CheckNodeValue(_dictionary._head);

        public override TValue PeekLast() => _reversed ? CheckNodeValue(_dictionary._head) : CheckNodeValue(_dictionary._tail);

        public override bool TryPeekFirst(out TValue item) {
            return _reversed ? PeekNodeValue(_dictionary._tail, out item) : PeekNodeValue(_dictionary._head, out item);
        }

        public override bool TryPeekLast(out TValue item) {
            return _reversed ? PeekNodeValue(_dictionary._head, out item) : PeekNodeValue(_dictionary._tail, out item);
        }

        public override bool Contains(TValue item) {
            return _dictionary.ContainsValue(item);
        }

        public override void CopyTo(TValue[] array, int arrayIndex, bool reversed = false) {
            _dictionary.CopyValuesTo(array, arrayIndex, _reversed ^ reversed);
        }

        public override ISequencedCollection<TValue> Reversed() {
            return _dictionary.CachedValues(_reversed);
        }

        public override IEnumerator<TValue> GetEnumerator() {
            return new ValueEnumerator(_dictionary, _reversed);
        }

        public override IEnumerator<TValue> GetReversedEnumerator() {
            return new ValueEnumerator(_dictionary, !_reversed);
        }
    }

    #endregion

    #region itr

    /// <summary>
    /// 注意：在修改为结构体组合模式后，外部在调用MoveNext后需要显式设置 _current 字段。
    /// </summary>
    private struct Enumerator
    {
        private readonly ImmutableLinkedDictionary<TKey, TValue> _dictionary;
        private readonly bool _reversed;

        private int _nextNode;
        internal Node _currNode;

        public Enumerator(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _dictionary = dictionary;
            _reversed = reversed;

            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _currNode = default;
        }

        public bool HasNext() {
            return _nextNode != -1;
        }

        public bool MoveNext() {
            if (_nextNode == -1) {
                return false;
            }
            _currNode = _dictionary._table[_nextNode];
            _nextNode = _reversed ? _currNode.prev : _currNode.next;
            return true;
        }

        public void Reset() {
            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _currNode = default;
        }

        public void Dispose() {
        }
    }

    public struct PairEnumerator : ISequentialEnumerator<KeyValuePair<TKey, TValue>>
    {
        private Enumerator _core;
        private KeyValuePair<TKey, TValue> _current;

        public PairEnumerator(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _core = new Enumerator(dictionary, reversed);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode.AsPair();
                return true;
            }
            return false;
        }

        public void Reset() {
            _core.Reset();
        }

        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose() {
            _core.Dispose();
        }
    }

    public struct KeyEnumerator : ISequentialEnumerator<TKey>
    {
        private Enumerator _core;
        private TKey _current;

        public KeyEnumerator(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _core = new Enumerator(dictionary, reversed);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode.key;
                return true;
            }
            return false;
        }

        public void Reset() {
            _core.Reset();
        }

        public TKey Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose() {
            _core.Dispose();
        }
    }

    public struct ValueEnumerator : ISequentialEnumerator<TValue>
    {
        private Enumerator _core;
        private TValue _current;

        public ValueEnumerator(ImmutableLinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _core = new Enumerator(dictionary, reversed);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode.value;
                return true;
            }
            return false;
        }

        public void Reset() {
            _core.Reset();
        }

        public TValue Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose() {
            _core.Dispose();
        }
    }

    #endregion

    /** 在构建table完成之后不再修改 */
    private struct Node
    {
        /** 由于Key的hash使用频率极高，缓存以减少求值开销 */
        internal readonly int hash;
        internal readonly TKey? key;
        internal readonly TValue? value;

        internal readonly int? index; // null表示Node无效，低版本不支持无参构造函数，无法指定为-1
        internal int prev;
        internal int next;

        public Node(int hash, TKey? key, TValue? value, int? index, int prev) {
            this.hash = hash;
            this.key = key;
            this.value = value;
            this.index = index;

            this.prev = prev;
            this.next = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue> AsPair() {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

#if DEBUG
        public override string ToString() {
            return $"{nameof(index)}: {index}, {nameof(key)}: {key}, {nameof(value)}: {value}, {nameof(prev)}: {prev}, {nameof(next)}: {next}";
        }
#else
        public override string ToString() {
            return $"index: {index}, {nameof(key)}: {key}, {nameof(value)}: {value}";
        }
#endif
    }
}
}