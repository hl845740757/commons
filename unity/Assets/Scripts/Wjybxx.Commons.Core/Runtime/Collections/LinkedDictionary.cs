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
/// 注：新版本删除了defaultValue，因为不符合C#风格，C#的this[index]默认需要抛出异常。此外，容易和TryGetValue造成混淆。
/// </summary>
/// <typeparam name="TKey">键的类型，允许为null</typeparam>
/// <typeparam name="TValue">值的类型，允许为null</typeparam>
[Serializable]
[NotThreadSafe]
public class LinkedDictionary<TKey, TValue> : ISequencedDictionary<TKey, TValue>
{
#nullable disable
    /** len = 2^n + 1，额外的槽用于存储nullKey；总是延迟分配空间，以减少创建空实例的开销 */
    private Node[] _table;
    private int _head = -1;
    private int _tail = -1;
#nullable restore
    /** 有效元素数量 */
    private int _count;
    /** 版本号 -- 发生结构性变化的时候增加，即增加和删除元素的时候；替换Key的Value不增加版本号 */
    private int _version;

    /** 当前计算下标使用的掩码，不依赖数组长度；相反，我们可以通过mask获得数组的真实长度；null槽在回环外 */
    private int _mask;
    /** 负载因子 */
    private float _loadFactor;
    /** 用于代替key自身的equals和hashcode计算；这一点C#的设计做的要好些 */
    private IEqualityComparer<TKey> _keyComparer;

    private KeyCollection? _keys;
    private ValueCollection? _values;
    // private ReversedDictionaryView<TKey, TValue>? _reversed;

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
        if (expectedCount == 0) {
            expectedCount = HashCommon.DefaultInitialSize;
        }
        HashCommon.CheckLoadFactor(loadFactor);
        _loadFactor = loadFactor;
        _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        _mask = HashCommon.ArraySize(expectedCount, loadFactor) - 1;
    }

    public LinkedDictionary(IDictionary<TKey, TValue> dictionary)
        : this(dictionary.Count, HashCommon.DefaultLoadFactor) {
        if (dictionary.Count == 0) return;
        foreach (var pair in dictionary) {
            Put(pair.Key, pair.Value);
        }
    }

    public bool IsReadOnly => false;
    public int Count => _count;
    public bool IsEmpty => _count == 0;

    /** 用于子类感知数组大小 */
    internal int Capacity => _mask + 1;

    /** 用于子类更新版本号 */
    protected void IncVersion() => _version++;
#nullable disable

    #region keys/values

    [DebuggerHidden] IGenericCollection<TKey> IGenericDictionary<TKey, TValue>.Keys => Keys;
    [DebuggerHidden] IGenericCollection<TValue> IGenericDictionary<TKey, TValue>.Values => Values;
    [DebuggerHidden] ICollection<TKey> IDictionary<TKey, TValue>.Keys => CachedKeys();
    [DebuggerHidden] ICollection<TValue> IDictionary<TKey, TValue>.Values => CachedValues();
    [DebuggerHidden] IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => CachedKeys();
    [DebuggerHidden] IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => CachedValues();

    [DebuggerHidden] ISequencedCollection<TKey> ISequencedDictionary<TKey, TValue>.SequencedKeys(bool reversed) {
        return SequencedKeys(reversed);
    }

    [DebuggerHidden] ISequencedCollection<TValue> ISequencedDictionary<TKey, TValue>.SequencedValues(bool reversed) {
        return SequencedValues(reversed);
    }

    public KeyCollection Keys => CachedKeys();
    public ValueCollection Values => CachedValues();

    public KeyCollection SequencedKeys(bool reversed = false) => CachedKeys(reversed);

    public ValueCollection SequencedValues(bool reversed = false) => CachedValues(reversed);

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
        set => TryPut(key, value, PutBehavior.None);
    }

    #endregion

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

    //
    private TValue PeekFirstValue() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.value;
    }

    private bool TryPeekFirstValue(out TValue value) {
        if (_count == 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[_head];
        value = node.value;
        return true;
    }

    private TValue PeekLastValue() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.value;
    }

    private bool TryPeekLastValue(out TValue value) {
        if (_count == 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[_tail];
        value = node.value;
        return true;
    }

    #endregion

    #region contains/get

    public bool ContainsKey(TKey key) {
        return Find(key, KeyHash(key, _keyComparer)) >= 0;
    }

    public bool ContainsValue(TValue value) {
        IEqualityComparer<TValue> valComparer = ValComparer;
        for (int index = _head; index >= 0;) {
            ref Node e = ref _table[index];
            if (valComparer.Equals(value, e.value)) {
                return true;
            }
            index = e.next;
        }
        return false;
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
        int index = Find(item.Key, KeyHash(item.Key, _keyComparer));
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        if (ValComparer.Equals(node.value, item.Value)) {
            RemoveNode(ref node);
            return true;
        }
        return false;
    }

    public bool Remove(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        RemoveNode(ref node);
        return true;
    }

    public bool Remove(TKey key, out TValue value) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        value = node.value;
        RemoveNode(ref node);
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveFirst() {
        int oldHead = _head;
        if (oldHead < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[oldHead];
        KeyValuePair<TKey, TValue> pair = node.AsPair();
        RemoveNode(ref node);
        return pair;
    }

    public bool TryRemoveFirst(out KeyValuePair<TKey, TValue> pair) {
        int oldHead = _head;
        if (oldHead < 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[oldHead];
        pair = node.AsPair();
        RemoveNode(ref node);
        return true;
    }

    public KeyValuePair<TKey, TValue> RemoveLast() {
        int oldTail = _tail;
        if (oldTail < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[oldTail];
        KeyValuePair<TKey, TValue> pair = node.AsPair();
        RemoveNode(ref node);
        return pair;
    }

    public bool TryRemoveLast(out KeyValuePair<TKey, TValue> pair) {
        int oldTail = _tail;
        if (oldTail < 0) {
            pair = default;
            return false;
        }
        ref Node node = ref _table[oldTail];
        pair = node.AsPair();
        RemoveNode(ref node);
        return true;
    }

    public void Clear() {
        int count = _count;
        if (count > 0) {
            _count = 0;
            _version++;
            _head = _tail = -1;
            Array.Clear(_table, 0, _table.Length);
        }
    }

    #endregion

    #region sp

#if NET6_0_OR_GREATER
    /// <summary>
    /// 获取Key关联的Value的地址，key不存在时返回默认的无效值
    /// 
    /// 注：
    /// 1.必须检查是否为空引用，否则可能导致进程崩溃，而不是简单的NPE。
    /// 2.也可以通过<see cref="Unsafe.IsNullRef"/>返回值的有效性。
    /// </summary>
    /// <returns></returns>
    public ref TValue GetValueRefOrNullRef(TKey key, out bool isNullRef) {
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos < 0) {
            isNullRef = true;
            return ref Unsafe.NullRef<TValue>();
        } else {
            isNullRef = false;
            ref Node node = ref _table[pos];
            return ref node.value;
        }
    }
#endif

    /// <summary>
    /// 获取Key关联的Value的地址
    /// 注意：不可以长期持有Value的地址，字典结构变化时可能指向错误的地址。
    /// </summary>
    public ref TValue GetValueRefOrAddDefault(TKey key, out bool exists) {
        _table ??= new Node[_mask + 2];
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos < 0) {
            exists = false;
            pos = -pos - 1;
            Insert(pos, hash, key, default, InsertionOrder.Default);
        } else {
            exists = true;
        }
        return ref _table[pos].value;
    }

    /// <summary>
    /// 获取元素，并将元素移动到首部
    /// （这几个接口不适合定义在接口中，因为只有查询效率高的有序字典才可以定义）
    /// </summary>
    /// <param name="key"></param>
    /// <returns>如果key存在，则返回关联值；否则抛出异常</returns>
    public TValue GetAndMoveToFirst(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        MoveToFirst(ref node);
        return node.value;
    }

    /// <summary>
    /// 获取元素，并将元素移动到首部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>如果元素存在则返回true</returns>
    public bool TryGetAndMoveToFirst(TKey key, out TValue value) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        MoveToFirst(ref node);
        value = node.value;
        return true;
    }

    /// <summary>
    /// 获取元素，并将元素移动到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <returns>如果key存在，则返回关联值；否则抛出异常</returns>
    public TValue GetAndMoveToLast(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        MoveToLast(ref node);
        return node.value;
    }

    /// <summary>
    /// 获取元素，并将元素移动到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>如果元素存在则返回true</returns>
    public bool TryGetAndMoveToLast(TKey key, out TValue value) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            value = default;
            return false;
        }
        ref Node node = ref _table[index];
        MoveToLast(ref node);
        value = node.value;
        return true;
    }

    /// <summary>
    /// 将给定key添加到目标key之后
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void AddAfter(TKey key, TValue value, TKey targetKey) {
        if (!ContainsKey(targetKey)) {
            throw new KeyNotFoundException(nameof(targetKey));
        }
        if (_keyComparer.Equals(key, targetKey)) {
            throw new ArgumentException("key == targetKey");
        }
        // 此处不进行任何优化，因为插入过程中Node的索引可能变化
        TryInsert(key, value, InsertionOrder.Default, InsertionBehavior.ThrowOnExisting);
        MoveToAfter(key, targetKey);
    }

    /// <summary>
    /// 将给定key添加到目标key之前
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void AddBefore(TKey key, TValue value, TKey targetKey) {
        if (!ContainsKey(targetKey)) {
            throw new KeyNotFoundException(nameof(targetKey));
        }
        if (_keyComparer.Equals(key, targetKey)) {
            throw new ArgumentException("key == targetKey");
        }
        // 此处不进行任何优化，因为插入过程中Node的索引可能变化
        TryInsert(key, value, InsertionOrder.Default, InsertionBehavior.ThrowOnExisting);
        MoveToBefore(key, targetKey);
    }

    /// <summary>
    /// 将给定key添加到目标key之后
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public PutResult<TValue> PutAfter(TKey key, TValue value, TKey targetKey) {
        if (!ContainsKey(targetKey)) {
            throw new KeyNotFoundException(nameof(targetKey));
        }
        if (_keyComparer.Equals(key, targetKey)) {
            throw new ArgumentException("key == targetKey");
        }
        PutResult<TValue> r = TryPut(key, value, PutBehavior.None);
        MoveToAfter(key, targetKey);
        return r;
    }

    /// <summary>
    /// 将给定key添加到目标key之前
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public PutResult<TValue> PutBefore(TKey key, TValue value, TKey targetKey) {
        if (!ContainsKey(targetKey)) {
            throw new KeyNotFoundException(nameof(targetKey));
        }
        if (_keyComparer.Equals(key, targetKey)) {
            throw new ArgumentException("key == targetKey");
        }
        PutResult<TValue> r = TryPut(key, value, PutBehavior.None);
        MoveToBefore(key, targetKey);
        return r;
    }

    /// <summary>
    /// 将Key移动到首部
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    public void MoveToFirst(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        if (index == _head) {
            return;
        }
        ref Node node = ref _table[index];
        MoveToFirst(ref node);
    }

    /// <summary>
    /// 将Key移动到尾部
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    public void MoveToLast(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        if (index == _tail) {
            return;
        }
        ref Node node = ref _table[index];
        MoveToLast(ref node);
    }

    /// <summary>
    /// 将指定key的元素移动到给定Key之后
    /// </summary>
    /// <param name="key"></param>
    /// <param name="targetKey"></param>
    public void MoveToAfter(TKey key, TKey targetKey) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        int index2 = Find(targetKey, KeyHash(targetKey, _keyComparer));
        if (index2 < 0) {
            throw ThrowHelper.KeyNotFoundException(targetKey);
        }
        if (index == index2) {
            throw new InvalidOperationException("key == target");
        }
        ref Node node = ref _table[index];
        if (node.prev == index2) {
            return;
        }
        if (index2 == _tail) {
            MoveToLast(ref node);
            return;
        }
        _version++;
        FixPointers(ref node); // 从原始节点解除引用

        ref Node targetNode = ref _table[index2];
        ref Node nextNode = ref _table[targetNode.next];
        nextNode.prev = index;
        node.next = nextNode.index;
        node.prev = index2;
        targetNode.next = index;
    }

    /// <summary>
    /// 将指定key的元素移动到给定Key之前
    /// </summary>
    /// <param name="key"></param>
    /// <param name="targetKey"></param>
    public void MoveToBefore(TKey key, TKey targetKey) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        int index2 = Find(targetKey, KeyHash(targetKey, _keyComparer));
        if (index2 < 0) {
            throw ThrowHelper.KeyNotFoundException(targetKey);
        }
        if (index == index2) {
            throw new InvalidOperationException("key == target");
        }
        ref Node node = ref _table[index];
        if (node.next == index2) {
            return;
        }
        if (index2 == _head) {
            MoveToFirst(ref node);
            return;
        }
        _version++;
        FixPointers(ref node); // 从原始节点解除引用

        ref Node targetNode = ref _table[index2];
        ref Node prevNode = ref _table[targetNode.prev];
        prevNode.next = index;
        node.prev = prevNode.index;
        node.next = index2;
        targetNode.prev = index;
    }

    /// <summary>
    /// 查询指定键的后一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="nextKey">接收下一个键</param>
    /// <returns>如果下一个key存在则返回true</returns>
    /// <exception cref="ThrowHelper.KeyNotFoundException">如果当前键不存在</exception>
    public bool NextKey(TKey key, out TKey nextKey) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.next < 0) {
            nextKey = default;
            return false;
        }
        ref Node nextNode = ref _table[node.next];
        nextKey = nextNode.key;
        return true;
    }

    /// <summary>
    /// 查询指定键的前一个键
    /// </summary>
    /// <param name="key">当前键</param>
    /// <param name="prevKey">接收前一个键</param>
    /// <returns>如果前一个key存在则返回true</returns>
    /// <exception cref="ThrowHelper.KeyNotFoundException">如果当前键不存在</exception>
    public bool PrevKey(TKey key, out TKey prevKey) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.prev < 0) {
            prevKey = default;
            return false;
        }
        ref Node nextNode = ref _table[node.prev];
        prevKey = nextNode.key;
        return true;
    }

    public bool NextKey(TKey key, out TKey nextKey, out TValue nextValue) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.next < 0) {
            nextKey = default;
            nextValue = default;
            return false;
        }
        ref Node nextNode = ref _table[node.next];
        nextKey = nextNode.key;
        nextValue = nextNode.value;
        return true;
    }

    public bool PrevKey(TKey key, out TKey prevKey, out TValue prevValue) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            throw ThrowHelper.KeyNotFoundException(key);
        }
        ref Node node = ref _table[index];
        if (node.prev < 0) {
            prevKey = default;
            prevValue = default;
            return false;
        }
        ref Node nextNode = ref _table[node.prev];
        prevKey = nextNode.key;
        prevValue = nextNode.value;
        return true;
    }

    public void EnsureCapacity(int expectedCount) {
        int curArraySize = _mask + 1;
        int newArraySize = HashCommon.ArraySize(expectedCount, _loadFactor);
        if (newArraySize <= curArraySize) {
            return;
        }
        if (_table == null) {
            _mask = newArraySize - 1;
        } else {
            Rehash(newArraySize);
        }
    }

    public void TrimCapacity(int expectedCount = -1) {
        if (_table == null) {
            return;
        }
        if (expectedCount < _count) {
            expectedCount = _count;
        }
        int curArraySize = _mask + 1;
        int newArraySize = HashCommon.ArraySize(expectedCount, _loadFactor);
        if (newArraySize >= curArraySize) {
            return;
        }
        // 避免调整后空间不足
        if (_count > HashCommon.MaxFill(newArraySize, _loadFactor)) {
            return;
        }
        Rehash(newArraySize);
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
        return new ReversedDictionaryView<TKey, TValue>(this);
        // if (_reversed == null) {
            // _reversed = new ReversedDictionaryView<TKey, TValue>(this);
        // }
        // return _reversed;
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
    private static int KeyHash(TKey key, IEqualityComparer<TKey> keyComparer) {
        if (typeof(TKey).IsValueType) {
            // 高版本C#会内联值内联的GetHashCode方法路径
            if (keyComparer == EqualityComparer<TKey>.Default) {
                return HashCommon.Mix(key.GetHashCode());
            }
        } else {
            if (key == null) return 0;
        }
        return HashCommon.Mix(keyComparer.GetHashCode(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Find(TKey key) {
        return Find(key, KeyHash(key, _keyComparer));
    }

    /// <summary>
    /// 如果Table尚未初始化，固定返回-1；如果要插入元素，应当先初始化Table再查询。
    /// 如果key存在，则返回对应的下标(大于等于0)；
    /// 如果key不存在，则返回其hash应该存储的下标的负值再减1，以识别0 -- 或者说 下标 +1 再取相反数。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hash">key的hash值</param>
    /// <returns></returns>
    private int Find(TKey key, int hash) {
        Node[] table = _table;
        if (table == null) {
            return -1;
        }
        if (!typeof(TKey).IsValueType && key == null) {
            Node nullNode = table[_mask + 1];
            return !nullNode.hasKey ? -(_mask + 2) : (_mask + 1);
        }

        IEqualityComparer<TKey> keyComparer = _keyComparer;
        int mask = _mask;
        // 先测试无冲突位置
        int pos = mask & hash;
        ref Node node = ref table[pos];
        if (!node.hasKey) return -(pos + 1);
        if (node.hash == hash && keyComparer.Equals(node.key, key)) {
            return pos;
        }
        // 线性探测
        // 注意：为了利用空间，线性探测需要在越界时绕回到数组首部(mask取余绕回)；'i'就是探测次数
        // 由于数组满时一定会触发扩容，可保证这里一定有一个槽为null；如果循环一圈失败，上次扩容失败被捕获？
        for (int i = 0; i < mask; i++) {
            pos = (pos + 1) & mask;
            node = ref table[pos];
            if (!node.hasKey) return -(pos + 1);
            if (node.hash == hash && keyComparer.Equals(node.key, key)) {
                return pos;
            }
        }
        throw new InvalidOperationException("state error");
    }

    private void Rehash(int newSize) {
        Debug.Assert(newSize >= _count);
        Node[] oldTable = _table;
        Node[] newTable = new Node[newSize + 1];
        this._table = newTable;
        this._mask = newSize - 1;

        int head = -1;
        int preNodePos = -1;
        for (int nextIndex = _head; nextIndex >= 0;) {
            ref Node node = ref oldTable[nextIndex];
            int pos = Find(node.key, node.hash);
            if (pos >= 0) {
                throw new InvalidOperationException("key: " + node.key);
            }
            pos = -pos - 1;
            newTable[pos] = new Node(node.hash, node.key, node.value, pos, preNodePos);

            if (preNodePos != -1) {
                ref Node preNode = ref newTable[preNodePos];
                preNode.next = pos;
            }
            if (head == -1) {
                head = pos;
            }
            preNodePos = pos;
            nextIndex = node.next;

            node = default; // help gc
        }
        this._head = head;
        this._tail = preNodePos;
        this._version++;
    }

    /** 如果插入成功(新增元素)，则返回true */
    private bool TryInsert(TKey key, TValue value, InsertionOrder order, InsertionBehavior behavior) {
        if (_table == null) {
            _table = new Node[_mask + 2];
        }
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
        if (_table == null) {
            _table = new Node[_mask + 2];
        }
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos >= 0) {
            ref Node existNode = ref _table[pos];
            PutResult<TValue> result = new PutResult<TValue>(false, existNode.value);
            existNode.value = value;
            if (behavior == PutBehavior.MoveToLast) {
                MoveToLast(ref existNode);
            } else if (behavior == PutBehavior.MoveToFirst) {
                MoveToFirst(ref existNode);
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
        return new PutResult<TValue>(true, default);
    }

    private void Insert(int pos, int hash, TKey key, TValue value, InsertionOrder order) {
        Node node = new Node(hash, key, value, pos);
        if (_count == 0) {
            _head = _tail = pos;
        } else if (order == InsertionOrder.Head) {
            // MoveToFirst
            ref Node headNode = ref _table[_head];
            headNode.prev = pos;
            node.next = _head;
            _head = pos;
        } else {
            // MoveToLast
            ref Node tailNode = ref _table[_tail];
            tailNode.next = pos;
            node.prev = _tail;
            _tail = pos;
        }
        _count++;
        _version++;
        _table[pos] = node;

        // 不再缓存maxFill，因为只有插入元素的时候计算，不会太频繁
        int maxFill = HashCommon.MaxFill(_mask + 1, _loadFactor);
        if (_count >= maxFill) {
            Rehash(HashCommon.ArraySize(_count + 1, _loadFactor));
        }
    }

    /** 删除指定节点 -- 该方法为通用情况；需要处理Head和Tail的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveNode(ref Node node) {
        _count--;
        _version++;

        FixPointers(ref node);
        ShiftKeys(node.index);
    }

    /// <summary>
    /// 删除pos位置的元素，将后续相同hash值的元素前移，才能保证线性探测法的有效性；
    /// 在调用该方法前，应当先调用 FixPointers 修正被删除节点的索引信息。
    /// 
    /// </summary>
    /// <param name="pos"></param>
    private void ShiftKeys(int pos) {
        if (pos == _mask + 1) { // nullKey
            _table[pos] = default; // 由于未Shift，我们显式置null
            return;
        }

        int mask = _mask;
        Node[] table = _table;
        int last, slot;
        // 需要双层for循环；因为当前元素移动后，可能引发其它hash值的元素移动
        while (true) {
            last = pos;
            pos = (pos + 1) & mask; // + 1 可能绕回到首部
            while (true) {
                ref Node curr = ref table[pos];
                if (!curr.hasKey) {
                    table[last] = default;
                    return;
                }
                slot = curr.hash & mask;
                if (last <= pos ? (last >= slot || slot > pos) : (last >= slot && slot > pos)) break;
                pos = (pos + 1) & mask;
            }

            ref Node curr2 = ref table[pos];
            curr2.index = last; // set index before copy
            table[last] = curr2;
            table[pos] = default;
            FixPointers(pos, last); // fix pointers
        }
    }

    /// <summary>
    /// 解除Node的引用
    /// 在调用该方法前需要先更新count和version，在Node真正删除后才可清理Node数据
    /// </summary>
    /// <param name="node">要解除引用的节点</param>
    private void FixPointers(ref Node node) {
        int pos = node.index;
        if (_count == 0) {
            _head = _tail = -1;
        } else if (pos == _head) {
            // 删除的是首部
            _head = node.next;
            ref Node nextNode = ref _table[node.next];
            nextNode.prev = -1;
        } else if (pos == _tail) {
            // 删除的是尾部
            _tail = node.prev;
            ref Node prevNode = ref _table[node.prev];
            prevNode.next = -1;
        } else {
            // 删除的是中间元素
            ref Node prevNode = ref _table[node.prev];
            ref Node nextNode = ref _table[node.next];
            prevNode.next = node.next;
            nextNode.prev = node.prev;
        }
        node.prev = -1;
        node.next = -1;
    }

    /// <summary>
    /// node从source移动到dest后，修正相关索引
    /// </summary>
    /// <param name="source">元素移动前位置</param>
    /// <param name="dest">元素移动后位置</param>
    private void FixPointers(int source, int dest) {
        if (_count == 1) {
            _head = _tail = dest;
            return;
        }
        if (_head == source) {
            _head = dest;
            ref Node node = ref _table[dest];
            ref Node nextNode = ref _table[node.next];
            nextNode.prev = dest;
        } else if (_tail == source) {
            _tail = dest;
            ref Node node = ref _table[dest];
            ref Node prevNode = ref _table[node.prev];
            prevNode.next = dest;
        } else {
            ref Node node = ref _table[dest];
            ref Node prevNode = ref _table[node.prev];
            ref Node nextNode = ref _table[node.next];
            prevNode.next = dest;
            nextNode.prev = dest;
        }
    }

    private void MoveToFirst(ref Node node) {
        int pos = node.index;
        if (pos == _head) {
            return;
        }
        FixPointers(ref node);

        ref Node oldHead = ref _table[_head];
        oldHead.prev = pos;
        node.next = _head;
        _head = pos;
        _version++;
    }

    private void MoveToLast(ref Node node) {
        int pos = node.index;
        if (pos == _tail) {
            return;
        }
        FixPointers(ref node);

        ref Node oldTail = ref _table[_tail];
        oldTail.next = pos;
        node.prev = _tail;
        _tail = pos;
        _version++;
    }

    #endregion

    #region view

    public abstract class AbstractViewCollection<T>
    {
        protected readonly LinkedDictionary<TKey, TValue> _dictionary;
        protected readonly bool _reversed;

        internal AbstractViewCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
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

        // public abstract ISequencedCollection<T> Reversed();
        //
        // public abstract IEnumerator<T> GetEnumerator();
        //
        // public abstract IEnumerator<T> GetReversedEnumerator();

        public abstract void CopyTo(T[] array, int arrayIndex, bool reversed = false);

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

    public class KeyCollection : AbstractViewCollection<TKey>, ISequencedCollection<TKey>
    {
        internal KeyCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed)
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

        #region itr

        public KeyCollection Reversed() {
            return _dictionary.CachedKeys(!_reversed);
        }

        public KeyEnumerator GetEnumerator() {
            return new KeyEnumerator(_dictionary, _reversed);
        }

        public KeyEnumerator GetReversedEnumerator() {
            return new KeyEnumerator(_dictionary, !_reversed);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
            return GetEnumerator();
        }

        ISequencedCollection<TKey> ISequencedCollection<TKey>.Reversed() {
            return Reversed();
        }

        IEnumerator<TKey> ISequencedCollection<TKey>.GetReversedEnumerator() {
            return GetReversedEnumerator();
        }

        #endregion
    }

    public class ValueCollection : AbstractViewCollection<TValue>, ISequencedCollection<TValue>
    {
        internal ValueCollection(LinkedDictionary<TKey, TValue> dictionary, bool reversed)
            : base(dictionary, reversed) {
        }

        public override TValue PeekFirst() => _reversed ? _dictionary.PeekLastValue() : _dictionary.PeekFirstValue();

        public override TValue PeekLast() => _reversed ? _dictionary.PeekFirstValue() : _dictionary.PeekLastValue();

        public override bool TryPeekFirst(out TValue item) {
            return _reversed ? _dictionary.TryPeekLastValue(out item) : _dictionary.TryPeekFirstValue(out item);
        }

        public override bool TryPeekLast(out TValue item) {
            return _reversed ? _dictionary.TryPeekFirstValue(out item) : _dictionary.TryPeekLastValue(out item);
        }

        public override bool Contains(TValue item) {
            return _dictionary.ContainsValue(item);
        }

        public override void CopyTo(TValue[] array, int arrayIndex, bool reversed = false) {
            _dictionary.CopyValuesTo(array, arrayIndex, _reversed ^ reversed);
        }

        #region itr

        public ValueCollection Reversed() {
            return _dictionary.CachedValues(!_reversed);
        }

        public ValueEnumerator GetEnumerator() {
            return new ValueEnumerator(_dictionary, _reversed);
        }

        public ValueEnumerator GetReversedEnumerator() {
            return new ValueEnumerator(_dictionary, !_reversed);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return GetEnumerator();
        }

        ISequencedCollection<TValue> ISequencedCollection<TValue>.Reversed() {
            return Reversed();
        }

        IEnumerator<TValue> ISequencedCollection<TValue>.GetReversedEnumerator() {
            return GetReversedEnumerator();
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
        private readonly LinkedDictionary<TKey, TValue> _dictionary;
        private readonly bool _reversed;
        private int _version;

        private int _nextNode;
        internal Node _currNode; // 支持remove

        public Enumerator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
            _dictionary = dictionary;
            _reversed = reversed;
            _version = dictionary._version;

            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _currNode = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNext() {
            return _nextNode != -1;
        }

        public bool MoveNext() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_nextNode == -1) {
                return false;
            }
            _currNode = _dictionary._table[_nextNode];
            _nextNode = _reversed ? _currNode.prev : _currNode.next;
            // 其实这期间node的value可能变化，安全的话应该每次创建新的Pair，但c#系统库没这么干 -- 保持不变也是一种策略
            // _current = CurrentOfNode(node);
            return true;
        }

        public void Remove() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (!_currNode.hasKey) {
                throw new InvalidOperationException("AlreadyRemoved");
            }
            TKey nextKey = default;
            if (_nextNode != -1) {
                ref Node nextNode = ref _dictionary._table[_nextNode];
                nextKey = nextNode.key;
            }
            _dictionary.RemoveNode(ref _currNode);
            _currNode = default;
            _version = _dictionary._version;
            // 修正索引
            if (_nextNode != -1) {
                _nextNode = _dictionary.Find(nextKey);
            }
        }

        public void Reset() {
            if (_version != _dictionary._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            _nextNode = _reversed ? _dictionary._tail : _dictionary._head;
            _currNode = default;
        }

        public void Dispose() {
        }
    }

    public struct PairEnumerator : ISequentialEnumerator<KeyValuePair<TKey, TValue>>, IUnsafeIterator<KeyValuePair<TKey, TValue>>
    {
        private Enumerator _core;
        private KeyValuePair<TKey, TValue> _current;

        public PairEnumerator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
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

        public KeyEnumerator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
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

        public ValueEnumerator(LinkedDictionary<TKey, TValue> dictionary, bool reversed) {
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

    #region node

    private struct Node
    {
#nullable disable
        internal readonly TKey key;
        internal TValue value;

        internal readonly bool hasKey; // 判断node是否有效，代替将index封装为Nullable<int>
        internal readonly int hash; // Key的hash使用频率极高，缓存以减少求值开销
        internal int index;
        internal int prev;
        internal int next;

        public Node(int hash, TKey key, TValue value, int index) {
            this.key = key;
            this.value = value;

            this.hasKey = true;
            this.hash = hash;
            this.index = index;
            this.prev = -1;
            this.next = -1;
        }

        public Node(int hash, TKey key, TValue value, int index, int prev) {
            this.key = key;
            this.value = value;

            this.hasKey = true;
            this.hash = hash;
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

    #endregion
}
}