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

using System.Collections.Generic;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 序列字典的反转视图
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ReversedDictionaryView<TKey, TValue> : ReversedCollectionView<KeyValuePair<TKey, TValue>>, ISequencedDictionary<TKey, TValue>
{
    public ReversedDictionaryView(ISequencedDictionary<TKey, TValue> delegated)
        : base(delegated) {
    }

    private ISequencedDictionary<TKey, TValue> Delegated => (ISequencedDictionary<TKey, TValue>)_delegated;

    public ISequencedCollection<TKey> Keys => Delegated.Keys;
    public ISequencedCollection<TValue> Values => Delegated.Values;

    public ISequencedCollection<TKey> UnsafeKeys(bool reversed = false) {
        return Delegated.UnsafeKeys(reversed);
    }

    public virtual TValue this[TKey key] {
        get => Delegated[key];
        set => Delegated[key] = value; // 允许重写
    }

    public override ISequencedDictionary<TKey, TValue> Reversed() {
        return Delegated;
    }

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