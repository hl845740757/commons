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
/// 保持插入序的Set
/// 1.由<see cref="LinkedDictionary{TKey,TValue}"/>修改而来，保留起特性。
/// 2.使用拷贝而不是封装的方式，以减少使用开销。
/// </summary>
/// <typeparam name="TKey">元素类型，允许为null</typeparam>
[Serializable]
[NotThreadSafe]
public class LinkedHashSet<TKey> : ISequencedSet<TKey>
{
#nullable disable
    /** len = 2^n + 1，额外的槽用于存储nullKey；总是延迟分配空间，以减少创建空实例的开销 */
    private Node[] _table;
    private int _head = -1;
    private int _tail = -1;
#nullable enable

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

    private ReversedSequenceSetView<TKey>? _reversed;

    public LinkedHashSet()
        : this(0, HashCommon.DefaultLoadFactor) {
    }

    public LinkedHashSet(ICollection<TKey> src)
        : this(src.Count, HashCommon.DefaultLoadFactor) {
        foreach (var key in src) {
            Add(key);
        }
    }

    public LinkedHashSet(IEqualityComparer<TKey> comparer)
        : this(0, HashCommon.DefaultLoadFactor, comparer) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expectedCount">期望存储的元素个数，而不是直接的容量</param>
    /// <param name="loadFactor">有效负载因子</param>
    /// <param name="keyComparer">可用于避免Key比较时装箱</param>
    public LinkedHashSet(int expectedCount, float loadFactor = 0.75f,
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
    internal int Capacity => _mask + 1;

#nullable disable

    #region peek

    public TKey PeekFirst() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.key;
    }

    public bool TryPeekFirst(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[_head];
        key = node.key;
        return true;
    }

    public TKey PeekLast() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.key;
    }

    public bool TryPeekLast(out TKey key) {
        if (_count == 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[_tail];
        key = node.key;
        return true;
    }

    #endregion

    #region contains

    public bool Contains(TKey key) {
        return Find(key, KeyHash(key, _keyComparer)) >= 0;
    }

    #endregion

    #region add

    public bool Add(TKey key) {
        return TryPut(key, PutBehavior.None);
    }

    public bool AddFirst(TKey key) {
        return TryPut(key, PutBehavior.MoveToFirst);
    }

    public bool AddLast(TKey key) {
        return TryPut(key, PutBehavior.MoveToLast);
    }

    public bool AddFirstIfAbsent(TKey key) {
        return TryInsert(key, InsertionOrder.Head, InsertionBehavior.None);
    }

    public bool AddLastIfAbsent(TKey key) {
        return TryInsert(key, InsertionOrder.Tail, InsertionBehavior.None);
    }

    [Obsolete("Instead AddAll")]
    public bool AddRange(IEnumerable<TKey> collection) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (collection is ICollection<TKey> c) {
            if (_loadFactor <= 0.5f) {
                EnsureCapacity(c.Count); // 负载小于0.5，数组的长度将大于等于count的2倍，就能放下所有元素
            } else {
                TryCapacity(_count + c.Count);
            }
        }
        bool r = false;
        foreach (TKey key in collection) {
            r |= TryPut(key, PutBehavior.None);
        }
        return r;
    }

    #endregion

    #region remove

    public bool Remove(TKey key) {
        int index = Find(key, KeyHash(key, _keyComparer));
        if (index < 0) {
            return false;
        }
        ref Node node = ref _table[index];
        RemoveNode(in node);
        return true;
    }

    public TKey RemoveFirst() {
        int oldHead = _head;
        if (oldHead < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[oldHead];
        TKey key = node.key;
        RemoveNode(in node);
        return key;
    }

    public bool TryRemoveFirst(out TKey key) {
        int oldHead = _head;
        if (oldHead < 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[oldHead];
        key = node.key;
        RemoveNode(in node);
        return true;
    }

    public TKey RemoveLast() {
        int oldTail = _tail;
        if (oldTail < 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[oldTail];
        TKey key = node.key;
        RemoveNode(in node);
        return key;
    }

    public bool TryRemoveLast(out TKey key) {
        int oldTail = _tail;
        if (oldTail < 0) {
            key = default;
            return false;
        }
        ref Node node = ref _table[oldTail];
        key = node.key;
        RemoveNode(in node);
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

    /** 用于子类更新版本号 */
    protected void IncVersion() => _version++;

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

    public void CopyTo(TKey[] array, int arrayIndex) {
        CopyTo(array, arrayIndex, false);
    }

    public void CopyTo(TKey[] array, int arrayIndex, bool reversed) {
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

    #endregion

    #region itr

    public ISequencedSet<TKey> Reversed() {
        if (_reversed == null) {
            _reversed = new ReversedSequenceSetView<TKey>(this);
        }
        return _reversed;
    }

    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
        return new Enumerator(this, false);
    }

    IEnumerator<TKey> ISequencedCollection<TKey>.GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this, false);
    }

    public Enumerator GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    #endregion

    #region core

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int KeyHash(TKey key, IEqualityComparer<TKey> keyComparer) {
        return key == null ? 0 : HashCommon.Mix(keyComparer.GetHashCode(key));
    }

    /// <summary>
    /// 如果Table尚未初始化，固定返回-1；如果要插入元素，应当先初始化Table再查询。
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
                throw new IllegalStateException("key: " + node.key);
            }
            pos = -pos - 1;
            newTable[pos] = new Node(node.hash, node.key, pos, preNodePos);

            if (preNodePos != -1) {
                ref Node preNode = ref newTable[preNodePos];
                preNode.next = pos;
            }
            if (head == -1) {
                head = pos;
            }
            preNodePos = pos;
            nextIndex = node.next;
        }
        this._head = head;
        this._tail = preNodePos;
    }

    /** 如果是insert则返回true */
    private bool TryInsert(TKey key, InsertionOrder order, InsertionBehavior behavior) {
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
        Insert(pos, hash, key, order);
        return true;
    }

    /** 如果是insert则返回true */
    private bool TryPut(TKey key, PutBehavior behavior) {
        if (_table == null) {
            _table = new Node[_mask + 2];
        }
        int hash = KeyHash(key, _keyComparer);
        int pos = Find(key, hash);
        if (pos >= 0) {
            ref Node existNode = ref _table[pos];
            if (behavior == PutBehavior.MoveToLast) {
                MoveToLast(ref existNode);
            } else if (behavior == PutBehavior.MoveToFirst) {
                MoveToFirst(ref existNode);
            }
            return false;
        }

        pos = -pos - 1;
        switch (behavior) {
            case PutBehavior.MoveToFirst:
                Insert(pos, hash, key, InsertionOrder.Head);
                break;
            case PutBehavior.MoveToLast:
                Insert(pos, hash, key, InsertionOrder.Tail);
                break;
            case PutBehavior.None:
            default:
                Insert(pos, hash, key, InsertionOrder.Default);
                break;
        }
        return true;
    }

    private void Insert(int pos, int hash, TKey key, InsertionOrder order) {
        Node node = new Node(hash, key, pos);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveNode(in Node node) {
        _count--;
        _version++;

        FixPointers(in node);
        ShiftKeys(node.index!.Value);
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
                if (curr.index == null) {
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
            FixPointers(pos, last); // fix pointers
        }
    }

    /// <summary>
    /// 解除Node的引用
    /// 在调用该方法前需要先更新count和version，在Node真正删除后才可清理Node数据
    /// </summary>
    /// <param name="node">要解除引用的节点</param>
    private void FixPointers(in Node node) {
        if (_count == 0) {
            _head = _tail = -1;
        } else if (node.index!.Value == _head) {
            // 删除的是首部
            _head = node.next;
            ref Node nextNode = ref _table[node.next];
            nextNode.prev = -1;
        } else if (node.index.Value == _tail) {
            // 删除的是尾部
            _tail = node.prev;
            ref Node prevNode = ref _table[node.prev];
            prevNode.next = -1;
        } else {
            // 删除的是中间元素
            ref Node prevNode = ref _table[node.prev];
            ref Node nextNode = ref _table[node.next];
            prevNode.next = nextNode.index!.Value;
            nextNode.prev = prevNode.index!.Value;
        }
    }

    /// <summary>
    /// node从source移动到dest后，修正相关索引
    /// </summary>
    /// <param name="source">元素移动前位置</param>
    /// <param name="dest">元素移动后位置</param>
    private void FixPointers(int source, int dest) {
        if (_count == 1) {
            _head = _tail = dest;
            ref Node node = ref _table[dest];
            node.prev = -1;
            node.next = -1;
            return;
        }
        if (_head == source) {
            _head = source;
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
        if (node.index!.Value == _head) {
            return;
        }
        // 先断开链接，再插入到首部
        if (node.index.Value == _tail) {
            _tail = node.prev;
            ref Node prevNode = ref _table[node.prev];
            prevNode.next = -1;
        } else {
            ref Node prevNode = ref _table[node.prev];
            ref Node nextNode = ref _table[node.next];
            prevNode.next = nextNode.index!.Value;
            nextNode.prev = prevNode.index!.Value;
        }

        ref Node headNode = ref _table[_head];
        headNode.prev = node.index.Value;
        node.next = _head;
        _head = node.index.Value;
        _version++;
    }

    private void MoveToLast(ref Node node) {
        if (node.index!.Value == _tail) {
            return;
        }
        // 先断开链接，再插入到尾部
        if (node.index.Value == _head) {
            _head = node.next;
            ref Node nextNode = ref _table[node.next];
            nextNode.prev = -1;
        } else {
            ref Node prevNode = ref _table[node.prev];
            ref Node nextNode = ref _table[node.next];
            prevNode.next = nextNode.index!.Value;
            nextNode.prev = prevNode.index!.Value;
        }

        ref Node tailNode = ref _table[_tail];
        tailNode.next = node.index.Value;
        node.prev = _tail;
        _tail = node.index.Value;
        _version++;
    }

    #endregion

    #region view

    public struct Enumerator : IUnsafeIterator<TKey>, ISequentialEnumerator<TKey>
    {
        private readonly LinkedHashSet<TKey> _hashSet;
        private readonly bool _reversed;
        private int _version;

        private int _nextNode;
        private Node _currNode; // 支持remove
        private TKey _current;

        internal Enumerator(LinkedHashSet<TKey> hashSet, bool reversed) {
            _hashSet = hashSet;
            _reversed = reversed;
            _version = hashSet._version;

            _nextNode = _reversed ? _hashSet._tail : _hashSet._head;
            _currNode = default;
            _current = default;
        }

        public bool HasNext() {
            return _nextNode != -1;
        }

        public bool MoveNext() {
            if (_version != _hashSet._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_nextNode == -1) {
                _current = default;
                return false;
            }
            _currNode = _hashSet._table[_nextNode];
            _nextNode = _reversed ? _currNode.prev : _currNode.next;
            // 其实这期间node的value可能变化，安全的话应该每次创建新的Pair，但c#系统库没这么干
            _current = _currNode.key;
            return true;
        }

        public void Remove() {
            if (_version != _hashSet._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            if (_currNode.index == null) {
                throw new InvalidOperationException("AlreadyRemoved");
            }
            _hashSet.RemoveNode(_currNode);
            _currNode = default;
            _version = _hashSet._version;
        }

        public void Reset() {
            if (_version != _hashSet._version) {
                throw new InvalidOperationException("EnumFailedVersion");
            }
            _nextNode = _reversed ? _hashSet._tail : _hashSet._head;
            _currNode = default;
            _current = default;
        }

        public TKey Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion

    #region node

    private struct Node
    {
#nullable disable
        /** 由于Key的hash使用频率极高，缓存以减少求值开销 */
        internal int hash;
        internal TKey key;

        internal int? index; // null表示Node无效，低版本不支持无参构造函数，无法指定为-1
        internal int prev;
        internal int next;

        public Node(int hash, TKey key, int index) {
            this.hash = hash;
            this.key = key;
            this.index = index;

            this.prev = -1;
            this.next = -1;
        }

        public Node(int hash, TKey key, int index, int prev) {
            this.hash = hash;
            this.key = key;
            this.index = index;
            this.prev = prev;
            this.next = -1;
        }

#if DEBUG
        public override string ToString() {
            return $"{nameof(index)}: {index}, {nameof(key)}: {key}, {nameof(prev)}: {prev}, {nameof(next)}: {next}";
        }
#else
        public override string ToString() {
            return $"index: {index}, {nameof(key)}: {key}";
        }
#endif
    }

    #endregion
}
}