﻿#region LICENSE

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

using System.Collections.Generic;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 序列字典的反转视图
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ReversedDictionaryView<TKey, TValue> : ReversedCollectionView<KeyValuePair<TKey, TValue>>, ISequencedDictionary<TKey, TValue>
{
    private ISequencedCollection<TKey>? _keys;
    private ISequencedCollection<TValue>? _values;

    public ReversedDictionaryView(ISequencedDictionary<TKey, TValue> delegated)
        : base(delegated) {
    }

    private ISequencedDictionary<TKey, TValue> Delegated => (ISequencedDictionary<TKey, TValue>)delegated;

    public new ISequencedDictionary<TKey, TValue> Reversed() {
        return Delegated;
    }

    #region key/values

    public IGenericCollection<TKey> Keys => CachedKeys();
    public IGenericCollection<TValue> Values => CachedValues();

    public ISequencedCollection<TKey> SequencedKeys(bool reversed = false) => CachedKeys(reversed);

    public ISequencedCollection<TValue> SequencedValues(bool reversed = false) => CachedValues(reversed);

    private ISequencedCollection<TKey> CachedKeys(bool reversed = false) {
        if (reversed) {
            return Delegated.SequencedKeys();
        }
        if (_keys == null) {
            _keys = Delegated.SequencedKeys(true);
        }
        return _keys;
    }

    private ISequencedCollection<TValue> CachedValues(bool reversed = false) {
        if (reversed) {
            return Delegated.SequencedValues();
        }
        if (_values == null) {
            _values = Delegated.SequencedValues(true);
        }
        return _values;
    }

    public virtual TValue this[TKey key] {
        get => Delegated[key];
        set => Delegated[key] = value; // 允许重写
    }

    #endregion

    #region get

    public TKey PeekFirstKey() => Delegated.PeekLastKey();

    public TKey PeekLastKey() => Delegated.PeekFirstKey();

    public bool TryPeekFirstKey(out TKey key) {
        return Delegated.TryPeekLastKey(out key);
    }

    public bool TryPeekLastKey(out TKey key) {
        return Delegated.TryPeekFirstKey(out key);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        return Delegated.TryGetValue(key, out value);
    }

    public bool ContainsKey(TKey key) {
        return Delegated.ContainsKey(key);
    }

    public bool ContainsValue(TValue value) {
        return Delegated.ContainsValue(value);
    }

    #endregion

    #region add

    public virtual void Add(TKey key, TValue value) {
        Delegated.Add(key, value); // 允许重写
    }

    public virtual bool TryAdd(TKey key, TValue value) {
        return Delegated.TryAdd(key, value); // 允许重写
    }

    public void AddFirst(TKey key, TValue value) {
        Delegated.AddLast(key, value);
    }

    public void AddLast(TKey key, TValue value) {
        Delegated.AddFirst(key, value);
    }

    public bool TryAddFirst(TKey key, TValue value) {
        return Delegated.TryAddLast(key, value);
    }

    public bool TryAddLast(TKey key, TValue value) {
        return Delegated.TryAddFirst(key, value);
    }

    public PutResult<TValue> PutFirst(TKey key, TValue value) {
        return Delegated.PutLast(key, value);
    }

    public PutResult<TValue> PutLast(TKey key, TValue value) {
        return Delegated.PutFirst(key, value);
    }

    public virtual PutResult<TValue> Put(TKey key, TValue value) {
        return Delegated.Put(key, value); // 允许重写
    }

    #endregion

    #region remove

    public bool Remove(TKey key) {
        return Delegated.Remove(key);
    }

    public bool Remove(TKey key, out TValue value) {
        return Delegated.Remove(key, out value);
    }

    #endregion
}
}