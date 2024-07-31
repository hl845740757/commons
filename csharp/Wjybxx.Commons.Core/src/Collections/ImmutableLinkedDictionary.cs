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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 不可变的保持插入序的字典
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
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
    private readonly ReversedDictionaryView<TKey, TValue> _reversed;

    public int Count => _count;
    public bool IsReadOnly => true;
    public bool IsEmpty => _count == 0;

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
        if (_count > 0) {
            ref Node node = ref _table[_head];
            pair = node.AsPair();
            return true;
        }
        pair = default;
        return false;
    }

    public KeyValuePair<TKey, TValue> PeekLast() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.AsPair();
    }

    public bool TryPeekLast(out KeyValuePair<TKey, TValue> pair) {
        if (_count > 0) {
            ref Node node = ref _table[_tail];
            pair = node.AsPair();
            return true;
        }
        pair = default;
        return false;
    }

    public TKey PeekFirstKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_head];
        return node.key;
    }

    public bool TryPeekFirstKey(out TKey key) {
        if (_count > 0) {
            ref Node node = ref _table[_head];
            key = node.key;
            return true;
        }
        key = default;
        return false;
    }

    public TKey PeekLastKey() {
        if (_count == 0) {
            throw ThrowHelper.CollectionEmptyException();
        }
        ref Node node = ref _table[_tail];
        return node.key;
    }

    public bool TryPeekLastKey(out TKey key) {
        if (_count > 0) {
            ref Node node = ref _table[_tail];
            key = node.key;
            return true;
        }
        key = default;
        return false;
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

    public ISequencedCollection<TKey> UnsafeKeys(bool reversed = false) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    #endregion

    public void AdjustCapacity(int expectedCount) {
    }

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
            for (int index = _tail; index >= 0;) {
                ref Node e = ref _table[index];
                array[arrayIndex++] = e.AsPair();
                index = e.next;
            }
        }
    }

    public ISequencedDictionary<TKey, TValue> Reversed() {
        return _reversed;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetReversedEnumerator() {
        throw new NotImplementedException();
    }

    public ISequencedCollection<TKey> Keys { get; }
    public ISequencedCollection<TValue> Values { get; }

    #region core

    private IEqualityComparer<TValue> ValComparer => EqualityComparer<TValue>.Default;

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

    /** 在构建table完成之后不再修改 */
    private struct Node
    {
        /** 由于Key的hash使用频率极高，缓存以减少求值开销 */
        internal readonly int hash;
        internal readonly TKey? key;
        internal readonly TValue? value;
        internal readonly int? index; // null表未Node无效，低版本不支持无参构造函数，无法指定为-1

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

        public override string ToString() {
            return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
        }
    }
}
}