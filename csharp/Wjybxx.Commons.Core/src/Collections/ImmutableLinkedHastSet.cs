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
/// 不可变的Set
/// 与C#系统库的ImmutableList不同，这里的实现是基于拷贝的，且不支持增删元素；另外这里保持了元素的插入顺序
/// (主要解决Unity的兼容性问题)
/// </summary>
/// <typeparam name="TKey"></typeparam>
[Serializable]
[Immutable]
public sealed class ImmutableLinkedHastSet<TKey> : ISequencedSet<TKey>
{
    /** len = 2^n + 1，额外的槽用于存储nullKey */
    private readonly Node[] _table;
    private readonly int _head;
    private readonly int _tail;

    /** 有效元素数量 */
    private readonly int _count;
    private readonly int _mask;
    private readonly IEqualityComparer<TKey> _keyComparer;
    private ReversedSequenceSetView<TKey>? _reversed;

    private ImmutableLinkedHastSet(TKey[] keyArray, IEqualityComparer<TKey>? keyComparer = null) {
        if (keyComparer == null) {
            keyComparer = EqualityComparer<TKey>.Default;
        }
        this._keyComparer = keyComparer;

        if (keyArray.Length == 0) {
            _table = Array.Empty<Node>();
            _head = _tail = -1;
            _count = 0;
            _mask = 0;
            return;
        }

        _count = keyArray.Length;
        _mask = HashCommon.ArraySize(keyArray.Length, 0.75f) - 1;
        _table = new Node[_mask + 2];

        // 插入的同时构建双向链表
        int preNodePos = -1;
        foreach (TKey key in keyArray) {
            int hash = KeyHash(key, keyComparer);
            int pos = Find(key, hash);
            if (pos >= 0) {
                throw new ArgumentException("duplicate element: " + key);
            }
            pos = -pos - 1;
            _table[pos] = new Node(hash, key, pos, preNodePos);

            if (preNodePos != -1) {
                ref Node preNode = ref _table[preNodePos];
                preNode.next = pos;
            }
            preNodePos = pos;
        }

        _head = Find(keyArray[0], KeyHash(keyArray[0], keyComparer));
        _tail = Find(keyArray[_count - 1], KeyHash(keyArray[_count - 1], keyComparer));
    }

    #region factory

    public static readonly ImmutableLinkedHastSet<TKey> Empty = new ImmutableLinkedHastSet<TKey>(Array.Empty<TKey>());

    public static ImmutableLinkedHastSet<TKey> Create(TKey source, IEqualityComparer<TKey>? keyComparer = null) {
        return new ImmutableLinkedHastSet<TKey>(new[] { source }, keyComparer);
    }

    public static ImmutableLinkedHastSet<TKey> CreateRange(IEnumerable<TKey> source, IEqualityComparer<TKey>? keyComparer = null) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        TKey[] array = source as TKey[];
        if (array == null) {
            array = source.ToArray();
        }
        return array.Length == 0 ? Empty : new ImmutableLinkedHastSet<TKey>(array, keyComparer);
    }

    #endregion

    public int Count => _count;
    public bool IsReadOnly => true;
    public bool IsEmpty => _count == 0;

    public bool Contains(TKey item) {
        return Find(item, KeyHash(item, _keyComparer)) >= 0;
    }

    public TKey PeekFirst() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.key;
    }

    public bool TryPeekFirst(out TKey item) {
        if (_count == 0) {
            item = default;
            return false;
        }
        ref Node node = ref _table[_head];
        item = node.key;
        return true;
    }

    public TKey PeekLast() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.key;
    }

    public bool TryPeekLast(out TKey item) {
        if (_count == 0) {
            item = default;
            return false;
        }
        ref Node node = ref _table[_tail];
        item = node.key;
        return true;
    }

    #region 修改接口

    public bool Add(TKey item) {
        throw new NotImplementedException();
    }

    public bool AddFirst(TKey item) {
        throw new NotImplementedException();
    }

    public bool AddLast(TKey item) {
        throw new NotImplementedException();
    }

    public bool AddFirstIfAbsent(TKey item) {
        throw new NotImplementedException();
    }

    public bool AddLastIfAbsent(TKey item) {
        throw new NotImplementedException();
    }

    public bool Remove(TKey item) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    public TKey RemoveFirst() {
        throw new NotImplementedException();
    }

    public bool TryRemoveFirst(out TKey item) {
        throw new NotImplementedException();
    }

    public TKey RemoveLast() {
        throw new NotImplementedException();
    }

    public bool TryRemoveLast(out TKey item) {
        throw new NotImplementedException();
    }

    #endregion

    public void AdjustCapacity(int expectedCount) {
    }

    public void CopyTo(TKey[] array, int arrayIndex, bool reversed = false) {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < _count) throw new ArgumentException("Array is too small");
        if (_count == 0) {
            return;
        }
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

    public ISequencedSet<TKey> Reversed() {
        if (_reversed == null) {
            _reversed = new ReversedSequenceSetView<TKey>(this);
        }
        return _reversed;
    }

    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator<TKey> ISequencedCollection<TKey>.GetReversedEnumerator() {
        return GetReversedEnumerator();
    }

    public Enumerator GetEnumerator() {
        return new Enumerator(this, false);
    }

    public Enumerator GetReversedEnumerator() {
        return new Enumerator(this, true);
    }

    #region core

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

    public struct Enumerator : ISequentialEnumerator<TKey>
    {
        private readonly ImmutableLinkedHastSet<TKey> _hashSet;
        private readonly bool _reversed;

        private int _nextNode;
        private TKey _current;

        internal Enumerator(ImmutableLinkedHastSet<TKey> hashSet, bool reversed) {
            _hashSet = hashSet;
            _reversed = reversed;

            _nextNode = _reversed ? _hashSet._tail : _hashSet._head;
            _current = default;
        }

        public bool HasNext() {
            return _nextNode != -1;
        }

        public bool MoveNext() {
            if (_nextNode == -1) {
                _current = default;
                return false;
            }
            ref Node node = ref _hashSet._table[_nextNode];
            _nextNode = _reversed ? node.prev : node.next;
            _current = node.key;
            return true;
        }

        public void Reset() {
            _nextNode = _reversed ? _hashSet._tail : _hashSet._head;
            _current = default;
        }

        public TKey Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion

    /** 在构建table完成之后不再修改 */
    private struct Node
    {
        /** 由于Key的hash使用频率极高，缓存以减少求值开销 */
        internal readonly int hash;
        internal readonly TKey? key;

        internal readonly int? index; // null表未Node无效，低版本不支持无参构造函数，无法指定为-1
        internal int prev;
        internal int next;

        public Node(int hash, TKey? key, int? index, int prev) {
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
}
}