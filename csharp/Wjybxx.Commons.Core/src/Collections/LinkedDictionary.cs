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
using System.Runtime.Serialization;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 保持插入序的字典
/// 1.使用简单的线性探测法解决Hash冲突，因此在数据量较大的情况下查询性能可能会降低 -- 实际表现很好。
/// 2.算法参考自FastUtil的LinkedOpenHashMap。
/// 3.支持null作为key。
/// 4.非线程安全。
/// 
/// 吐槽：
/// 1.C#的基础库里居然没有保持插入序的高性能字典，这对于编写底层工具的开发者来说太不方便了。
/// 2.C#的集合和字典库接口太差了，泛型集合与非泛型集合兼容性也不够。
/// 
/// </summary>
/// <typeparam name="TKey">键的类型，允许为null</typeparam>
/// <typeparam name="TValue">值的类型，允许为null</typeparam>
[Serializable]
public class LinkedDictionary<TKey, TValue> : ISequencedDictionary<TKey, TValue>, ISerializable
{
    // C#的泛型是独立的类，因此缓存是独立的
    private static readonly bool ValueIsValueType = typeof(TValue).IsValueType;

    /** len = 2^n + 1，额外的槽用于存储nullKey；总是延迟分配空间，以减少创建空实例的开销 */
    private Node?[]? _table; // 这个NullableReference有时真的很烦
    private Node? _head;
    private Node? _tail;

    /** 有效元素数量 */
    private int _count;
    /** 版本号 -- 发生结构性变化的时候增加，即增加和删除元素的时候；替换Key的Value不增加版本号 */
    private int _version;

    /** 当前计算下标使用的掩码，不依赖数组长度；相反，我们可以通过mask获得数组的真实长度 */
    private int _mask;
    /** 负载因子 */
    private float _loadFactor;
    /** 用于代替key自身的equals和hashcode计算；这一点C#的设计做的要好些 */
    private IEqualityComparer<TKey> _keyComparer;
    /** key不存在时的默认值  */
    private TValue _defValue;

    private KeyCollection? _keys;
    private ValueCollection? _values;

    public LinkedDictionary()
        : this(0, HashCommon.DefaultLoadFactor) {
    }

    public LinkedDictionary(IEqualityComparer<TKey> comparer)
        : this(0, HashCommon.DefaultLoadFactor, comparer) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expectedCount">期望存储的元素个数，而不是直接的容量</param>
    /// <param name="loadFactor">有效负载因子</param>
    /// <param name="keyComparer">可用于避免Key比较时装箱</param>
    public LinkedDictionary(int expectedCount, float loadFactor = 0.75f,
                            IEqualityComparer<TKey>? keyComparer = null) {
        if (expectedCount < 0) throw new ArgumentException("The expected number of elements must be nonnegative");
        HashCommon.CheckLoadFactor(loadFactor);
        _loadFactor = loadFactor;
        _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

        if (expectedCount == 0) {
            expectedCount = HashCommon.DefaultInitialSize;
        }
        _mask = HashCommon.ArraySize(expectedCount, loadFactor) - 1;
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// 默认值会序列化
    /// </summary>
    public TValue DefaultValue {
        get => _defValue;
        set => _defValue = value;
    }

    #region keys/values

    public ISequencedCollection<TKey> Keys => CachedKeys();
    public ISequencedCollection<TValue> Values => CachedValues();
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => CachedKeys();
    ICollection<TValue> IDictionary<TKey, TValue>.Values => CachedValues();
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => CachedKeys();
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => CachedValues();

    public ISequencedCollection<TKey> UnsafeKeys(bool reversed = false) {
        return new UnsafeKeyCollection(this, reversed);
    }

    private KeyCollection CachedKeys() {
        if (_keys == null) {
            _keys = new KeyCollection(this, false);
        }
        return _keys;
    }

    private ValueCollection CachedValues() {
        if (_values == null) {
            _values = new ValueCollection(this, false);
        }
        return _values;
    }

    public TValue this[TKey key] {
        get {
            Node? node = GetNode(key);
            if (node == null) throw CollectionUtil.KeyNotFoundException(key);
            return node.value;
        }
        set => TryPut(key, value, PutBehavior.None);
    }

    #endregion

    #region peek

    public bool TryPeekFirst(out KeyValuePair<TKey, TValue> pair) {
        if (_head != null) {
            pair = _head.AsPair();
            return true;
        }
        pair = default;
        return false;
    }

    public KeyValuePair<TKey, TValue> PeekFirst() {
        if (_head == null) throw CollectionUtil.CollectionEmptyException();
        return _head.AsPair();
    }

    public bool TryPeekLast(out KeyValuePair<TKey, TValue> pair) {
        if (_tail != null) {
            pair = _tail.AsPair();
            return true;
        }
        pair = default;
        return false;
    }

    public KeyValuePair<TKey, TValue> PeekLast() {
        if (_tail == null) throw CollectionUtil.CollectionEmptyException();
        return _tail.AsPair();
    }

    public TKey PeekFirstKey() {
        if (_head == null) throw CollectionUtil.CollectionEmptyException();
        return _head.key;
    }

    public bool TryPeekFirstKey(out TKey key) {
        if (_head == null) {
            key = default;
            return false;
        }
        key = _head.key;
        return true;
    }

    public TKey PeekLastKey() {
        if (_tail == null) throw CollectionUtil.CollectionEmptyException();
        return _tail.key;
    }

    public bool TryPeekLastKey(out TKey key) {
        if (_tail == null) {
            key = default;
            return false;
        }
        key = _tail.key;
        return true;
    }

    #endregion

    #region contains/get

    public bool ContainsKey(TKey key) {
        return GetNode(key) != null;
    }

    public bool ContainsValue(TValue value) {
        if (value == null) {
            for (Node e = _head; e != null; e = e.next) {
                if (e.value == null) {
                    return true;
                }
            }
            return false;
        } else {
            IEqualityComparer<TValue>? valComparer = ValComparer;
            for (Node e = _head; e != null; e = e.next) {
                if (valComparer.Equals(e.value, value)) {
                    return true;
                }
            }
            return false;
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        Node node = GetNode(item.Key);
        return node != null && ValComparer.Equals(node.value, item.Value);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        var node = GetNode(key);
        if (node == null) {
            value = _defValue;
            return false;
        }
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
        var node = GetNode(item.Key);
        if (node != null && ValComparer.Equals(node.value, item.Value)) {
            RemoveNode(node);
            return true;
        }
        return false;
    }

    public bool Remove(TKey key) {
        var node = GetNode(key);
        if (node == null) {
            return false;
        }
        RemoveNode(node);
        return true;
    }

    public bool Remove(TKey key, out TValue value) {
        var node = GetNode(key);
        if (node == null) {
            value = default;
            return false;
        }
        value = node.value;
        RemoveNode(node);
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveFirst() {
        if (_count == 0) {
            throw CollectionUtil.CollectionEmptyException();
        }
        TryRemoveFirst(out KeyValuePair<TKey, TValue> r);
        return r;
    }

    public bool TryRemoveFirst(out KeyValuePair<TKey, TValue> pair) {
        Node oldHead = _head;
        if (oldHead == null) {
            pair = default;
            return false;
        }

        pair = oldHead.AsPair();
        _count--;
        _version++;
        FixPointers(oldHead);
        ShiftKeys(oldHead.index);
        oldHead.AfterRemoved();
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveLast() {
        if (_count == 0) {
            throw CollectionUtil.CollectionEmptyException();
        }
        TryRemoveLast(out KeyValuePair<TKey, TValue> r);
        return r;
    }

    public bool TryRemoveLast(out KeyValuePair<TKey, TValue> pair) {
        Node oldTail = _tail;
        if (oldTail == null) {
            pair = default;
            return false;
        }
        pair = oldTail.AsPair();

        _count--;
        _version++;
        FixPointers(oldTail);
        ShiftKeys(oldTail.index);
        oldTail.AfterRemoved();
        return true;
    }

    public void Clear() {
        int count = _count;
        if (count > 0) {
            _count = 0;
            _version++;
            _head = _tail = null;
            Array.Clear(_table!);
        }
    }

    /** 用于子类更新版本号 */
    protected void IncVersion() => _version++;

    #endregion

    #region sp

    /// <summary>
    /// 获取元素，并将元素移动到首部
    /// （这几个接口不适合定义在接口中，因为只有查询效率高的有序字典才可以定义）
    /// </summary>
    /// <param name="key"></param>
    /// <returns>如果key存在，则返回关联值；否则抛出异常</returns>
    public TValue GetAndMoveToFirst(TKey key) {
        var node = GetNode(key);
        if (node == null) {
            throw CollectionUtil.KeyNotFoundException(key);
        }
        MoveToFirst(node);
        return node.value;
    }

    /// <summary>
    /// 获取元素，并将元素移动到首部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>如果元素存在则返回true</returns>
    public bool TryGetAndMoveToFirst(TKey key, out TValue value) {
        var node = GetNode(key);
        if (node == null) {
            value = default;
            return false;
        }
        MoveToFirst(node);
        value = node.value;
        return true;
    }

    /// <summary>
    /// 获取元素，并将元素移动到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <returns>如果key存在，则返回关联值；否则抛出异常</returns>
    public TValue GetAndMoveToLast(TKey key) {
        var node = GetNode(key);
        if (node == null) {
            throw CollectionUtil.KeyNotFoundException(key);
        }
        MoveToLast(node);
        return node.value;
    }

    /// <summary>
    /// 获取元素，并将元素移动到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>如果元素存在则返回true</returns>
    public bool TryGetAndMoveToLast(TKey key, out TValue value) {
        var node = GetNode(key);
        if (node == null) {
            value = default;
            return false;
        }
        MoveToLast(node);
        value = node.value;
        return true;
    }

    /// <summary>
    /// 查询指定键的后一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="next">接收下一个键</param>
    /// <returns></returns>
    /// <exception cref="CollectionUtil.KeyNotFoundException">如果当前键不存在</exception>
    public bool NextKey(TKey key, out TKey next) {
        var node = GetNode(key);
        if (node == null) {
            throw CollectionUtil.KeyNotFoundException(key);
        }
        if (node.next != null) {
            next = node.next.key;
            return true;
        }
        next = default;
        return false;
    }

    /// <summary>
    /// 查询指定键的前一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="prev">接收前一个键</param>
    /// <returns></returns>
    /// <exception cref="CollectionUtil.KeyNotFoundException">如果当前键不存在</exception>
    public bool PrevKey(TKey key, out TKey prev) {
        var node = GetNode(key);
        if (node == null) {
            throw CollectionUtil.KeyNotFoundException(key);
        }
        if (node.prev != null) {
            prev = node.prev.key;
            return true;
        }
        prev = default;
        return false;
    }

    public void AdjustCapacity(int expectedCount) {
        if (expectedCount < _count) {
            throw new ArgumentException($"expectedCount:{expectedCount} < count {_count}");
        }
        int arraySize = HashCommon.ArraySize(expectedCount, _loadFactor);
        if (arraySize <= HashCommon.DefaultInitialSize) {
            return;
        }
        int curArraySize = _mask + 1;
        if (arraySize == curArraySize) {
            return;
        }
        if (arraySize < curArraySize) {
            if (_count > HashCommon.MaxFill(arraySize, _loadFactor)) {
                return; // 避免收缩后空间不足
            }
            if (Math.Abs(arraySize - curArraySize) <= HashCommon.DefaultInitialSize) {
                return; // 避免不必要的收缩
            }
        }
        if (_table == null) {
            _mask = arraySize - 1;
        } else {
            Rehash(arraySize);
        }
    }

    #endregion

    #region copyto

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        CopyTo(array, arrayIndex, false);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (Node e = _tail; e != null; e = e.prev) {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(e.key, e.value);
            }
        } else {
            for (Node e = _head; e != null; e = e.next) {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(e.key, e.value);
            }
        }
    }

    public void CopyKeysTo(TKey[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (Node e = _tail; e != null; e = e.prev) {
                array[arrayIndex++] = e.key;
            }
        } else {
            for (Node e = _head; e != null; e = e.next) {
                array[arrayIndex++] = e.key;
            }
        }
    }

    public void CopyValuesTo(TValue[] array, int arrayIndex, bool reversed) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");

        if (reversed) {
            for (Node e = _tail; e != null; e = e.prev) {
                array[arrayIndex++] = e.value;
            }
        } else {
            for (Node e = _head; e != null; e = e.next) {
                array[arrayIndex++] = e.value;
            }
        }
    }

    #endregion

    #region itr

    public ISequencedDictionary<TKey, TValue> Reversed() {
        return new ReversedDictionaryView<TKey, TValue>(this);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        return new PairIterator(this, false);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetReversedEnumerator() {
        return new PairIterator(this, true);
    }

    #endregion

    #region core

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
        if (table == null) {
            table = _table = new Node[_mask + 2];
        }
        if (key == null) {
            Node nullNode = table[_mask + 1];
            return nullNode == null ? -(_mask + 2) : (_mask + 1);
        }

        IEqualityComparer<TKey> keyComparer = _keyComparer;
        int mask = _mask;
        // 先测试无冲突位置
        int pos = mask & hash;
        Node node = table[pos];
        if (node == null) return -(pos + 1);
        if (node.hash == hash && keyComparer.Equals(node.key, key)) {
            return pos;
        }
        // 线性探测
        // 注意：为了利用空间，线性探测需要在越界时绕回到数组首部(mask取余绕回)；'i'就是探测次数
        // 由于数组满时一定会触发扩容，可保证这里一定有一个槽为null；如果循环一圈失败，上次扩容失败被捕获？
        for (int i = 0; i < mask; i++) {
            pos = (pos + 1) & mask;
            node = table[pos];
            if (node == null) return -(pos + 1);
            if (node.hash == hash && keyComparer.Equals(node.key, key)) {
                return pos;
            }
        }
        throw new InvalidOperationException("state error");
    }

    /** 该接口仅适用于查询方法使用 */
    private Node? GetNode(TKey key) {
        Node[] table = _table;
        if (table == null || _count == 0) {
            return null;
        }
        if (key == null) {
            return table[_mask + 1];
        }
        IEqualityComparer<TKey> keyComparer = _keyComparer;
        int mask = _mask;
        // 先测试无冲突位置
        int hash = KeyHash(key, keyComparer);
        int pos = mask & hash;
        Node node = table[pos];
        if (node == null) return null;
        if (node.hash == hash && keyComparer.Equals(node.key, key)) {
            return node;
        }
        for (int i = 0; i < mask; i++) {
            pos = (pos + 1) & mask;
            node = table[pos];
            if (node == null) return null;
            if (node.hash == hash && keyComparer.Equals(node.key, key)) {
                return node;
            }
        }
        throw new InvalidOperationException("state error");
    }

    /** 如果插入成功(新增元素)，则返回true */
    private bool TryInsert(TKey key, TValue value, InsertionOrder order, InsertionBehavior behavior) {
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos >= 0) {
            if (behavior == InsertionBehavior.ThrowOnExisting) {
                throw new InvalidOperationException("AddingDuplicateWithKey: " + key);
            }
            return false;
        }

        pos = -pos - 1;
        Insert(pos, hash, key, value, order);
        return true;
    }

    /** 如果是insert则返回true */
    private PutResult<TValue> TryPut(TKey key, TValue value, PutBehavior behavior) {
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos >= 0) {
            Node existNode = _table![pos]!;
            PutResult<TValue> result = new PutResult<TValue>(false, existNode.value);
            existNode.value = value;
            if (behavior == PutBehavior.MoveToLast) {
                MoveToLast(existNode);
            } else if (behavior == PutBehavior.MoveToFirst) {
                MoveToFirst(existNode);
            }
            return result;
        }

        pos = -pos - 1;
        switch (behavior) {
            case PutBehavior.MoveToFirst:
                Insert(pos, hash, key, value, InsertionOrder.Head);
                break;
            case PutBehavior.MoveToLast:
                Insert(pos, hash, key, value, InsertionOrder.Tail);
                break;
            case PutBehavior.None:
            default:
                Insert(pos, hash, key, value, InsertionOrder.Default);
                break;
        }
        return new PutResult<TValue>(true, _defValue);
    }

    private void Insert(int pos, int hash, TKey key, TValue value, InsertionOrder order) {
        Node node = new Node(hash, key, value, pos);
        if (_count == 0) {
            _head = _tail = node;
        } else if (order == InsertionOrder.Head) {
            node.next = _head;
            _head!.prev = node;
            _head = node;
        } else {
            node.prev = _tail;
            _tail!.next = node;
            _tail = node;
        }
        _count++;
        _version++;
        _table![pos] = node;

        // 不再缓存maxFill，因为只有插入元素的时候计算，不会太频繁
        int maxFill = HashCommon.MaxFill(_mask + 1, _loadFactor);
        if (_count >= maxFill) {
            Rehash(HashCommon.ArraySize(_count + 1, _loadFactor));
        }
    }

    private void Rehash(int newSize) {
        Debug.Assert(newSize >= _count);
        Node[] oldTable = _table!;
        Node[] newTable = new Node[newSize + 1];

        int mask = newSize - 1;
        int pos;
        // 遍历旧table数组会更快，数据更连续
        int remain = _count;
        for (var i = 0; i < oldTable.Length; i++) {
            var node = oldTable[i];
            if (node == null) {
                continue;
            }
            if (node.key == null) {
                pos = mask + 1;
            } else {
                pos = node.hash & mask;
                while (newTable[pos] != null) {
                    pos = (pos + 1) & mask;
                }
            }
            newTable[pos] = node;
            node.index = pos;
            if (--remain == 0) {
                break;
            }
        }
        this._table = newTable;
        this._mask = mask;
    }

    private void EnsureCapacity(int capacity) {
        int arraySize = HashCommon.ArraySize(capacity, _loadFactor);
        if (arraySize > _mask + 1) {
            Rehash(arraySize);
        }
    }

    private void TryCapacity(int capacity) {
        int arraySize = HashCommon.TryArraySize(capacity, _loadFactor);
        if (arraySize > _mask + 1) {
            Rehash(arraySize);
        }
    }

    /** 删除指定节点 -- 该方法为通用情况；需要处理Head和Tail的情况 */
    private void RemoveNode(Node node) {
        _count--;
        _version++;
        FixPointers(node);
        ShiftKeys(node.index);
        node.AfterRemoved(); // 可以考虑自动收缩空间
    }

    /// <summary>
    /// 删除pos位置的元素，将后续相同hash值的元素前移；
    /// 在调用该方法前，应当先调用 FixPointers 修正被删除节点的索引信息。
    /// </summary>
    /// <param name="pos"></param>
    private void ShiftKeys(int pos) {
        if (pos == _mask + 1) { // nullKey
            return;
        }
        Node[] table = _table!;
        int mask = _mask;

        int last, slot;
        Node curr;
        // 需要双层for循环；因为当前元素移动后，可能引发其它hash值的元素移动
        while (true) {
            last = pos;
            pos = (pos + 1) & mask; // + 1 可能绕回到首部
            while (true) {
                curr = table[pos];
                if (curr == null) {
                    table[last] = null;
                    return;
                }
                // [slot   last .... pos   slot] slot是应该属于的位置，pos是实际的位置，slot在连续区间外则应该移动
                slot = curr.hash & mask;
                if (last <= pos ? (last >= slot || slot > pos) : (last >= slot && slot > pos)) break;
                pos = (pos + 1) & mask;
            }
            table[last] = curr;
            curr.index = last;
        }
    }

    /// <summary>
    /// 解除Node的引用
    /// 在调用该方法前需要先更新count和version，在Node真正删除后才可清理Node数据
    /// </summary>
    /// <param name="node">要解除引用的节点</param>
    private void FixPointers(Node node) {
        if (_count == 0) {
            _head = _tail = null;
        } else if (node == _head) {
            _head = node.next!;
            _head.prev = null;
        } else if (node == _tail) {
            _tail = node.prev!;
            _tail.next = null;
        } else {
            // 删除的是中间元素
            Node prev = node.prev!;
            Node next = node.next!;
            prev.next = next;
            next.prev = prev;
        }
    }

    private void MoveToFirst(Node node) {
        if (node == _head) {
            return;
        }
        if (node == _tail) {
            _tail = node.prev!;
            _tail.next = null;
        } else {
            var prev = node.prev!;
            var next = node.next!;
            prev.next = next;
            next.prev = prev;
        }
        node.next = _head;
        _head!.prev = node;
        _head = node;
    }

    private void MoveToLast(Node node) {
        if (node == _tail) {
            return;
        }
        if (node == _head) {
            _head = node.next!;
            _head.prev = null;
        } else {
            var prev = node.prev!;
            var next = node.next!;
            prev.next = next;
            next.prev = prev;
        }
        node.prev = _tail;
        _tail!.next = node;
        _tail = node;
    }

    #endregion

    #region util

    private IEqualityComparer<TValue> ValComparer => EqualityComparer<TValue>.Default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int KeyHash(TKey? key, IEqualityComparer<TKey> keyComparer) {
        return key == null ? 0 : HashCommon.Mix(keyComparer.GetHashCode(key));
    }

    #endregion

    #region view

    private abstract class AbstractViewCollection<T> : ISequencedCollection<T>
    {
        protected readonly LinkedDictionary<TKey, TValue> _dictionary;
        protected readonly bool _reversed;

        protected AbstractViewCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _dictionary = dictionary;
            _reversed = reversed;
        }

        #region 查询

        public virtual bool IsReadOnly => true;
        public int Count => _dictionary.Count;

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
        public KeyCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed)
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
            return _reversed ? _dictionary.Keys : new KeyCollection(_dictionary, true);
        }

        public override IEnumerator<TKey> GetEnumerator() {
            return new KeyIterator(_dictionary, _reversed);
        }

        public override IEnumerator<TKey> GetReversedEnumerator() {
            return new KeyIterator(_dictionary, !_reversed);
        }
    }

    private class ValueCollection : AbstractViewCollection<TValue>
    {
        public ValueCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed)
            : base(dictionary, reversed) {
        }

        private static TValue CheckNodeValue(Node? node) {
            if (node == null) throw CollectionUtil.CollectionEmptyException();
            return node.value;
        }

        private static bool PeekNodeValue(Node? node, out TValue value) {
            if (node == null) {
                value = default;
                return false;
            }
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
            return _reversed ? _dictionary.Values : new ValueCollection(_dictionary, true);
        }

        public override IEnumerator<TValue> GetEnumerator() {
            return new ValueIterator(_dictionary, _reversed);
        }

        public override IEnumerator<TValue> GetReversedEnumerator() {
            return new ValueIterator(_dictionary, !_reversed);
        }
    }

    private class UnsafeKeyCollection : KeyCollection
    {
        internal UnsafeKeyCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed)
            : base(dictionary, reversed) {
        }

        public override bool IsReadOnly => false;

        public override TKey RemoveFirst() {
            return _reversed ? _dictionary.RemoveLast().Key : _dictionary.RemoveFirst().Key;
        }

        public override TKey RemoveLast() {
            return _reversed ? _dictionary.RemoveFirst().Key : _dictionary.RemoveLast().Key;
        }

        public override bool TryRemoveFirst(out TKey key) {
            return TryRemove(out key, _reversed ? InsertionOrder.Tail : InsertionOrder.Head);
        }

        public override bool TryRemoveLast(out TKey key) {
            return TryRemove(out key, _reversed ? InsertionOrder.Head : InsertionOrder.Tail);
        }

        private bool TryRemove(out TKey key, InsertionOrder order) {
            KeyValuePair<TKey, TValue> pair;
            bool r = order == InsertionOrder.Head ? _dictionary.TryRemoveFirst(out pair) : _dictionary.TryRemoveLast(out pair);
            if (r) {
                key = pair.Key;
                return true;
            }
            key = default;
            return false;
        }

        public override bool Remove(TKey key) {
            return _dictionary.Remove(key);
        }

        public override void Clear() {
            _dictionary.Clear();
        }

        public override IEnumerator<TKey> GetEnumerator() {
            return new UnsafeKeyIterator(_dictionary, _reversed);
        }

        public override IEnumerator<TKey> GetReversedEnumerator() {
            return new UnsafeKeyIterator(_dictionary, !_reversed);
        }
    }

    #endregion

    #region itr

    private abstract class AbstractIterator<T> : IEnumerator<T>
    {
        private readonly LinkedDictionary<TKey, TValue> _dictionary;
        private readonly bool _reversed;
        private int _version;

        private Node? _currNode;
        private Node? _nextNode;
        private T _current;

        protected AbstractIterator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _dictionary = dictionary;
            _reversed = reversed;
            _version = dictionary._version;

            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _current = default;
        }

        public bool MoveNext() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_nextNode == null) {
                _current = default;
                return false;
            }
            Node node = _currNode = _nextNode;
            _nextNode = _reversed ? node.prev : node.next;
            // 其实这期间node的value可能变化，安全的话应该每次创建新的Pair，但c#系统库没这么干
            _current = CurrentOfNode(node);
            return true;
        }

        protected abstract T CurrentOfNode(Node node);

        public void Remove() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_currNode == null || _currNode.index < 0) {
                throw new InvalidOperationException("AlreadyRemoved");
            }
            _dictionary.RemoveNode(_currNode);
            _currNode = null;
            _version = _dictionary._version;
        }

        public void Reset() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            _currNode = null;
            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    private class PairIterator : AbstractIterator<KeyValuePair<TKey, TValue>>, IUnsafeIterator<KeyValuePair<TKey, TValue>>
    {
        public PairIterator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) : base(dictionary, reversed) {
        }

        protected override KeyValuePair<TKey, TValue> CurrentOfNode(Node node) {
            return new KeyValuePair<TKey, TValue>(node.key, node.value);
        }
    }

    private class KeyIterator : AbstractIterator<TKey>
    {
        public KeyIterator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) : base(dictionary, reversed) {
        }

        protected override TKey CurrentOfNode(Node node) {
            return node.key;
        }
    }

    private class ValueIterator : AbstractIterator<TValue>
    {
        public ValueIterator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) : base(dictionary, reversed) {
        }

        protected override TValue CurrentOfNode(Node node) {
            return node.value;
        }
    }

    private class UnsafeKeyIterator : AbstractIterator<TKey>, IUnsafeIterator<TKey>
    {
        public UnsafeKeyIterator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) : base(dictionary, reversed) {
        }

        protected override TKey CurrentOfNode(Node node) {
            return node.key;
        }
    }

    #endregion

    #region node

    private class Node
    {
        /** 由于Key的hash使用频率极高，缓存以减少求值开销 */
        internal readonly int hash;
        internal readonly TKey? key;
        internal TValue? value;
        /** 由于使用线性探测法，删除的元素不一定直接位于hash槽上，需要记录，以便快速删除；-1表示已删除 */
        internal int index;

        internal Node? prev;
        internal Node? next;

        public Node(int hash, TKey? key, TValue value, int index) {
            this.hash = hash;
            this.key = key;
            this.value = value;
            this.index = index;
        }

        public void AfterRemoved() {
            if (!ValueIsValueType) {
                value = default;
            }
            index = -1;
            prev = null;
            next = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue> AsPair() {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public override int GetHashCode() {
            return hash; // 不使用value计算hash，因为value可能在中途变更
        }

        public override string ToString() {
            return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
        }
    }

    #endregion

    #region seril

    private const string NamesMask = "Mask";
    private const string NamesLoadFactor = "LoadFactor";
    private const string NamesComparer = "Comparer";
    private const string NamesPairs = "KeyValuePairs";
    private const string NamesDefaultValue = "DefaultValue";

    public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
        if (info == null) throw new ArgumentNullException(nameof(info));
        info.AddValue(NamesMask, _mask);
        info.AddValue(NamesLoadFactor, _loadFactor);
        info.AddValue(NamesComparer, _keyComparer, typeof(IEqualityComparer<TKey>));
        info.AddValue(NamesDefaultValue, _defValue, typeof(TValue));

        if (_table != null && _count > 0) { // 有数据才序列化
            var array = new KeyValuePair<TKey, TValue>[Count];
            CopyTo(array, 0, false);
            info.AddValue(NamesPairs, array, typeof(KeyValuePair<TKey, TValue>[]));
        }
    }

    protected LinkedDictionary(SerializationInfo info, StreamingContext context) {
        this._mask = info.GetInt32(NamesMask);
        this._loadFactor = info.GetSingle(NamesLoadFactor);
        this._keyComparer = (IEqualityComparer<TKey>)info.GetValue(NamesComparer, typeof(IEqualityComparer<TKey>)) ?? EqualityComparer<TKey>.Default;
        this._defValue = (TValue)info.GetValue(NamesDefaultValue, typeof(TValue));

        HashCommon.CheckLoadFactor(_loadFactor);
        if (_mask + 1 != MathCommon.NextPowerOfTwo(_mask)) {
            throw new Exception("invalid serial data, _mask: " + _mask);
        }

        KeyValuePair<TKey, TValue>[] pairs = (KeyValuePair<TKey, TValue>[])info.GetValue(NamesPairs, typeof(KeyValuePair<TKey, TValue>[]));
        if (pairs != null && pairs.Length > 0) {
            BuildTable(pairs);
        }
    }

    private void BuildTable(KeyValuePair<TKey, TValue>[] pairsArray) {
        // 构建Node链
        IEqualityComparer<TKey> keyComparer = _keyComparer;
        Node head;
        {
            KeyValuePair<TKey, TValue> pair = pairsArray[0];
            int hash = KeyHash(pair.Key, keyComparer);
            head = new Node(hash, pair.Key, pair.Value, -1);
        }
        Node tail = head;
        for (var i = 1; i < pairsArray.Length; i++) {
            KeyValuePair<TKey, TValue> pair = pairsArray[i];
            int hash = KeyHash(pair.Key, keyComparer);
            Node next = new Node(hash, pair.Key, pair.Value, -1);
            //
            tail.next = next;
            next.prev = tail;
            tail = next;
        }
        _head = head;
        _tail = tail;

        // 散列到数组 -- 走正常的Find方法更安全些
        _table = new Node[_mask + 2];
        _count = pairsArray.Length;
        for (Node node = _head; node != null; node = node.next) {
            int pos = Find(node.key, node.hash);
            if (pos >= 0) {
                throw new SerializationException("invalid serial data");
            }
            pos = -pos - 1;
            _table[pos] = node;
        }
    }

    #endregion
}