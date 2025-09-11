#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
/// 非Hash结构的字典，适用于小数据量场景
///
/// 注：未实现<see cref="ISequencedDictionary{TKey,TValue}"/>，因为可以直接按照数组方式迭代；也用于减少开销。
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public sealed class ArrayDictionary<TKey, TValue> : IGenericDictionary<TKey, TValue>
{
#nullable disable
    private Node[] _table;
#nullable restore
    /** 有效元素数量 */
    private int _count;
    /** 版本号 -- 发生结构性变化的时候增加，即增加和删除元素的时候；替换Key的Value不增加版本号 */
    private int _version;

    private KeyCollection? _keys;
    private ValueCollection? _values;

    public ArrayDictionary(int expectedCount = 0) {
        if (expectedCount > 0) {
            _table = new Node[expectedCount];
        }
    }

    public ArrayDictionary(IDictionary<TKey, TValue> dictionary) {
        if (dictionary.Count > 0) { // 避免创建Table，但并发字典的Count测试可能是不精确的
            EnsureCapacity(dictionary.Count);
        }
        foreach (var pair in dictionary) {
            Put(pair.Key, pair.Value);
        }
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    #region keys/values

    [DebuggerHidden] IGenericCollection<TKey> IGenericDictionary<TKey, TValue>.Keys => Keys;
    [DebuggerHidden] IGenericCollection<TValue> IGenericDictionary<TKey, TValue>.Values => Values;
    [DebuggerHidden] ICollection<TKey> IDictionary<TKey, TValue>.Keys => CachedKeys();
    [DebuggerHidden] ICollection<TValue> IDictionary<TKey, TValue>.Values => CachedValues();
    [DebuggerHidden] IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => CachedKeys();
    [DebuggerHidden] IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => CachedValues();

    public KeyCollection Keys => CachedKeys();
    public ValueCollection Values => CachedValues();

    private KeyCollection CachedKeys() {
        if (_keys == null) {
            _keys = new KeyCollection(this);
        }
        return _keys;
    }

    private ValueCollection CachedValues() {
        if (_values == null) {
            _values = new ValueCollection(this);
        }
        return _values;
    }

    public TValue this[TKey key] {
        get {
            int index = Find(key);
            if (index < 0) {
                throw ThrowHelper.KeyNotFoundException(key);
            }
            ref Node node = ref _table[index];
            return node.value;
        }
        set => TryPut(key, value, PutBehavior.None);
    }

    #endregion


    #region peek

    public KeyValuePair<TKey, TValue> PeekFirst() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[0];
        return node.AsPair();
    }

    public bool TryPeekFirst(out KeyValuePair<TKey, TValue> pair) {
        if (_count == 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[0];
        pair = node.AsPair();
        return true;
    }

    public KeyValuePair<TKey, TValue> PeekLast() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_count - 1];
        return node.AsPair();
    }

    public bool TryPeekLast(out KeyValuePair<TKey, TValue> pair) {
        if (_count == 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[_count - 1];
        pair = node.AsPair();
        return true;
    }

    public TKey PeekFirstKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[0];
        return node.key;
    }

    public bool TryPeekFirstKey(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[0];
        key = node.key;
        return true;
    }

    public TKey PeekLastKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_count - 1];
        return node.key;
    }

    public bool TryPeekLastKey(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[_count - 1];
        key = node.key;
        return true;
    }

    #endregion

    #region contains/get

    public bool ContainsKey(TKey key) {
        return Find(key) >= 0;
    }

    public bool ContainsValue(TValue value) {
        if (!typeof(TValue).IsValueType && value == null) {
            for (int index = _count - 1; index >= 0; index--) {
                ref Node e = ref _table[index];
                if (e.value == null) {
                    return true;
                }
            }
            return false;
        } else {
            IEqualityComparer<TValue> valComparer = ValComparer;
            for (int index = _count - 1; index >= 0; index--) {
                ref Node e = ref _table[index];
                if (valComparer.Equals(value, e.value)) {
                    return true;
                }
            }
            return false;
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        int index = Find(item.Key);
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        return ValComparer.Equals(item.Value, node.value);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        int index = Find(key);
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        value = node.value;
        return true;
    }

    #endregion

    #region add

    public void Add(TKey key, TValue value) {
        bool inserted = TryInsert(key, value, InsertionOrder.Default, InsertionBehavior.ThrowOnExisting);
        Debug.Assert(inserted);
    }

    public bool TryAdd(TKey key, TValue value) {
        return TryInsert(key, value, InsertionOrder.Default, InsertionBehavior.None);
    }

    public void AddFirst(TKey key, TValue value) {
        bool inserted = TryInsert(key, value, InsertionOrder.Head, InsertionBehavior.ThrowOnExisting);
        Debug.Assert(inserted);
    }

    public bool TryAddFirst(TKey key, TValue value) {
        return TryInsert(key, value, InsertionOrder.Head, InsertionBehavior.None);
    }

    public void AddLast(TKey key, TValue value) {
        bool inserted = TryInsert(key, value, InsertionOrder.Tail, InsertionBehavior.ThrowOnExisting);
        Debug.Assert(inserted);
    }

    public bool TryAddLast(TKey key, TValue value) {
        return TryInsert(key, value, InsertionOrder.Tail, InsertionBehavior.None);
    }

    public PutResult<TValue> Put(TKey key, TValue value) {
        return TryPut(key, value, PutBehavior.None);
    }

    public PutResult<TValue> PutFirst(TKey key, TValue value) {
        return TryPut(key, value, PutBehavior.MoveToFirst);
    }

    public PutResult<TValue> PutLast(TKey key, TValue value) {
        return TryPut(key, value, PutBehavior.MoveToLast);
    }

    #endregion

    #region remove

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        int index = Find(item.Key);
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        if (ValComparer.Equals(node.value, item.Value)) {
            RemoveNode(index);
            return true;
        }
        return false;
    }

    public bool Remove(TKey key) {
        int index = Find(key);
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        RemoveNode(index);
        return true;
    }

    public bool Remove(TKey key, out TValue value) {
        int index = Find(key);
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        value = node.value;
        RemoveNode(index);
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveFirst() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[0];
        KeyValuePair<TKey, TValue> pair = node.AsPair();
        RemoveNode(0);
        return pair;
    }

    public bool TryRemoveFirst(out KeyValuePair<TKey, TValue> pair) {
        if (_count == 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[0];
        pair = node.AsPair();
        RemoveNode(0);
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveLast() {
        int oldTail = _count - 1;
        if (oldTail < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[oldTail];
        KeyValuePair<TKey, TValue> pair = node.AsPair();
        RemoveNode(oldTail);
        return pair;
    }

    public bool TryRemoveLast(out KeyValuePair<TKey, TValue> pair) {
        int oldTail = _count - 1;
        if (oldTail < 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[oldTail];
        pair = node.AsPair();
        RemoveNode(oldTail);
        return true;
    }

    public void Clear() {
        int count = _count;
        if (count > 0) {
            _count = 0;
            _version++;
            Array.Clear(_table, 0, _table.Length);
        }
    }

    #endregion

    #region sp

    public TKey GetKey(int index) {
        if (index < 0 || index >= _count) {
            throw new ArgumentOutOfRangeException();
        }
        ref Node node = ref _table[index];
        return node.key;
    }

    public KeyValuePair<TKey, TValue> GetPair(int index) {
        if (index < 0 || index >= _count) {
            throw new ArgumentOutOfRangeException();
        }
        ref Node node = ref _table[index];
        return node.AsPair();
    }

    public void GetPair(int index, out TKey key, out TValue value) {
        if (index < 0 || index >= _count) {
            throw new ArgumentOutOfRangeException();
        }
        ref Node node = ref _table[index];
        key = node.key;
        value = node.value;
    }

    public void EnsureCapacity(int expectedCount) {
        if (_table == null) {
            _table = new Node[expectedCount < 4 ? 4 : expectedCount];
            return;
        }
        // 保持小步增长
        int oldCapacity = _table.Length;
        int minGrowUp = expectedCount - oldCapacity;
        int growUp = oldCapacity <= 16 ? 4 : 8;
        if (growUp < minGrowUp) {
            growUp = minGrowUp;
        }
        Resize(oldCapacity + growUp);
    }

    public void TrimCapacity(int expectedCount = -1) {
        if (_table == null) {
            return;
        }
        if (expectedCount < _table.Length) {
            Resize(expectedCount);
        }
    }

    private void Resize(int newSize) {
        Debug.Assert(newSize >= _count);
        Array.Resize(ref _table, newSize);
        _version++;
    }

    #endregion

    #region copyto

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        CopyTo(array, arrayIndex, false);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");
        if (_count == 0) {
            return;
        }
        if (reversed) {
            for (int index = _count - 1; index >= 0; index--) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.AsPair();
            }
        } else {
            for (int index = 0; index < _count; index++) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.AsPair();
            }
        }
    }

    public void CopyKeysTo(TKey[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (int index = _count - 1; index >= 0; index--) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.key;
            }
        } else {
            for (int index = 0; index < _count; index++) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.key;
            }
        }
    }

    public void CopyValuesTo(TValue[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (int index = _count - 1; index >= 0; index--) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.value;
            }
        } else {
            for (int index = 0; index < _count; index++) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.value;
            }
        }
    }

    #endregion

    #region itr

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
        return GetEnumerator();
    }

    public PairEnumerator GetEnumerator() {
        return new PairEnumerator(this);
    }

    #endregion

    #region core

    private static IEqualityComparer<TValue> ValComparer => EqualityComparer<TValue>.Default;
    private static readonly bool IsKeyValueType = typeof(TKey).IsValueType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private int Find(TKey key) {
        Node[] table = _table;
        if (table == null) {
            return -1;
        }
        // 顺序测试
        if (!IsKeyValueType && key == null) {
            for (int index = _count - 1; index >= 0; index--) {
                ref Node node = ref _table[index];
                if (node.hasKey && node.key == null) return index;
            }
            return -1;
        }
        EqualityComparer<TKey> keyComparer = EqualityComparer<TKey>.Default;
        for (int index = _count - 1; index >= 0; index--) {
            ref Node node = ref _table[index];
            if (node.hasKey && keyComparer.Equals(node.key, key)) return index;
        }
        return -1;
    }

    /** 如果插入成功(新增元素)，则返回true */
    private bool TryInsert(TKey key, TValue value, InsertionOrder order, InsertionBehavior behavior) {
        int pos = Find(key);
        if (pos >= 0) {
            if (behavior == InsertionBehavior.ThrowOnExisting) {
                throw new InvalidOperationException("AddingDuplicateWithKey: " + key);
            }
            return false;
        }
        Insert(key, value, order);
        return true;
    }

    /** 如果是insert则返回true */
    private PutResult<TValue> TryPut(TKey key, TValue value, PutBehavior behavior) {
        int pos = Find(key);
        if (pos >= 0) {
            ref Node existNode = ref _table[pos];
            PutResult<TValue> result = new PutResult<TValue>(false, existNode.value);
            existNode.value = value;
            if (behavior == PutBehavior.MoveToLast) {
                MoveToLast(pos);
            } else if (behavior == PutBehavior.MoveToFirst) {
                MoveToFirst(pos);
            }
            return result;
        }
        switch (behavior) {
            case PutBehavior.MoveToFirst:
                Insert(key, value, InsertionOrder.Head);
                break;
            case PutBehavior.MoveToLast:
                Insert(key, value, InsertionOrder.Tail);
                break;
            case PutBehavior.None:
            default:
                Insert(key, value, InsertionOrder.Default);
                break;
        }
        return new PutResult<TValue>(true, default);
    }

    private void Insert(TKey key, TValue value, InsertionOrder order) {
        if (_table == null) {
            _table = new Node[4];
        } else if (_count == _table.Length) {
            EnsureCapacity(_count + 1);
        }
        Node node = new Node(key, value);
        int pos;
        if (order == InsertionOrder.Head) {
            Array.Copy(_table, 0, _table, 1, _count);
            pos = 0;
        } else {
            pos = _count;
        }
        _count++;
        _version++;
        _table[pos] = node;
    }

    /// <summary>
    /// 删除指定节点
    /// 注意：由于外部使用的是<code>ref Node</code>，因此调用该方法后，Node指向的地址将产生变化，因此不可在调用该方法后继续访问Node数据。
    /// </summary>
    /// <param name="index"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveNode(int index) {
        _count--;
        _version++;
        if (index < _count) {
            Array.Copy(_table, index + 1, _table, index, _count - index);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>()) {
            _table[_count] = default!;
        }
    }

    private void MoveToFirst(int index) {
        if (index == 0) {
            return;
        }
        ArrayUtil.MoveTo(_table, index, 0);
    }

    private void MoveToLast(int index) {
        if (index == _count - 1) {
            return;
        }
        ArrayUtil.MoveTo(_table, index, _count - 1);
    }

    #endregion

    #region view

    public abstract class AbstractViewCollection<T>
    {
        protected readonly ArrayDictionary<TKey, TValue> _dictionary;

        internal AbstractViewCollection(ArrayDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
        }

        #region 查询

        public virtual bool IsReadOnly => true;
        public int Count => _dictionary.Count;
        public bool IsEmpty => _dictionary.IsEmpty;

        public abstract bool Contains(T item);

        #endregion

        #region itr

        //
        // public abstract IEnumerator<T> GetEnumerator();
        //
        public abstract void CopyTo(T[] array, int arrayIndex);

        #endregion

        #region modify

        public void EnsureCapacity(int expectedCount) {
            throw new InvalidOperationException("NotSupported_KeyOrValueCollectionSet");
        }

        public void TrimCapacity(int expectedCount) {
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

    public class KeyCollection : AbstractViewCollection<TKey>, IGenericCollection<TKey>
    {
        internal KeyCollection(ArrayDictionary<TKey, TValue> dictionary)
            : base(dictionary) {
        }

        public override bool Contains(TKey item) {
            return _dictionary.ContainsKey(item);
        }

        public override void CopyTo(TKey[] array, int arrayIndex) {
            _dictionary.CopyKeysTo(array, arrayIndex, false);
        }

        #region itr

        public KeyEnumerator GetEnumerator() {
            return new KeyEnumerator(_dictionary);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion
    }

    public class ValueCollection : AbstractViewCollection<TValue>, IGenericCollection<TValue>
    {
        internal ValueCollection(ArrayDictionary<TKey, TValue> dictionary)
            : base(dictionary) {
        }

        public override bool Contains(TValue item) {
            return _dictionary.ContainsValue(item);
        }

        public override void CopyTo(TValue[] array, int arrayIndex) {
            _dictionary.CopyValuesTo(array, arrayIndex, false);
        }

        #region itr

        public ValueEnumerator GetEnumerator() {
            return new ValueEnumerator(_dictionary);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion

    #region itr

    /// <summary>
    /// 注意：在修改为结构体组合模式后，外部在调用MoveNext后需要显式设置 _current 字段。
    /// </summary>
    private struct Enumerator
    {
        private readonly ArrayDictionary<TKey, TValue> _dictionary;
        private int _version;

        private int _nextNode;
        internal Node _currNode; // 支持remove

        public Enumerator(ArrayDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _version = dictionary._version;

            _nextNode = 0;
            _currNode = default;
        }

        public bool HasNext() {
            return _nextNode < _dictionary.Count;
        }

        public bool MoveNext() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_nextNode >= _dictionary.Count) {
                return false;
            }
            _currNode = _dictionary._table[_nextNode];
            _nextNode++;
            // 其实这期间node的value可能变化，安全的话应该每次创建新的Pair，但c#系统库没这么干 -- 保持不变也是一种策略
            // _current = CurrentOfNode(node);
            return true;
        }

        public void Remove() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_currNode.IsNull()) {
                throw new InvalidOperationException("AlreadyRemoved");
            }
            _dictionary.RemoveNode(_nextNode - 1);
            _currNode = default;
            _version = _dictionary._version;
        }

        public void Reset() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            _nextNode = 0;
            _currNode = default;
        }

        public void Dispose() {
        }
    }

    public struct PairEnumerator : ISequentialEnumerator<KeyValuePair<TKey, TValue>>, IUnsafeIterator<KeyValuePair<TKey, TValue>>
    {
        private Enumerator _core;
        private KeyValuePair<TKey, TValue> _current;

        public PairEnumerator(ArrayDictionary<TKey, TValue> dictionary) {
            _core = new Enumerator(dictionary);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode!.AsPair();
                return true;
            }
            return false;
        }

        public void Remove() {
            _core.Remove();
        }

        public void Reset() {
            _core.Reset();
            _current = default;
        }

        public KeyValuePair<TKey, TValue> Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose() {
            _core.Dispose();
        }
    }

    public struct KeyEnumerator : ISequentialEnumerator<TKey>, IUnsafeIterator<TKey>
    {
        private Enumerator _core;
        private TKey _current;

        public KeyEnumerator(ArrayDictionary<TKey, TValue> dictionary) {
            _core = new Enumerator(dictionary);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode!.key;
                return true;
            }
            return false;
        }

        public void Remove() {
            _core.Remove();
        }

        public void Reset() {
            _core.Reset();
            _current = default;
        }

        public TKey Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose() {
            _core.Dispose();
        }
    }

    public struct ValueEnumerator : ISequentialEnumerator<TValue>, IUnsafeIterator<TValue>
    {
        private Enumerator _core;
        private TValue _current;

        public ValueEnumerator(ArrayDictionary<TKey, TValue> dictionary) {
            _core = new Enumerator(dictionary);
            _current = default;
        }

        public bool HasNext() {
            return _core.HasNext();
        }

        public bool MoveNext() {
            if (_core.MoveNext()) {
                _current = _core._currNode!.value;
                return true;
            }
            return false;
        }

        public void Remove() {
            _core.Remove();
            _current = default;
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

    private struct Node
    {
        internal readonly TKey key;
        internal TValue value;
        internal readonly bool hasKey;

        public Node(TKey key, TValue value) {
            this.key = key;
            this.value = value;
            this.hasKey = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull() => hasKey == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue> AsPair() {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public override string ToString() {
            return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
        }
    }
}
}