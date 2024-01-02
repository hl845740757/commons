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
using System.Collections.Generic;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 序列集合的反转视图
/// </summary>
/// <typeparam name="TKey">元素的类型</typeparam>
public class ReversedCollectionView<TKey> : ISequencedCollection<TKey>
{
    protected readonly ISequencedCollection<TKey> _delegated;

    public ReversedCollectionView(ISequencedCollection<TKey> delegated) {
        _delegated = delegated ?? throw new ArgumentNullException(nameof(delegated));
    }

    public int Count => _delegated.Count;
    public bool IsReadOnly => false;

    public virtual ISequencedCollection<TKey> Reversed() {
        return _delegated;
    }

    public void AdjustCapacity(int expectedCount) {
        _delegated.AdjustCapacity(expectedCount);
    }

    #region get

    public TKey PeekFirst() => _delegated.PeekLast();

    public TKey PeekLast() => _delegated.PeekFirst();

    public bool TryPeekFirst(out TKey item) {
        return _delegated.TryPeekLast(out item);
    }

    public bool TryPeekLast(out TKey item) {
        return _delegated.TryPeekFirst(out item);
    }

    public bool Contains(TKey item) {
        return _delegated.Contains(item);
    }

    #endregion

    #region add

    public virtual void Add(TKey item) {
        _delegated.Add(item); // 允许重写-部分集合的Add是隐含方向的
    }

    public void AddFirst(TKey item) {
        _delegated.AddLast(item);
    }

    public void AddLast(TKey item) {
        _delegated.AddFirst(item);
    }

    #endregion

    #region remove

    public bool Remove(TKey item) {
        return _delegated.Remove(item);
    }

    public TKey RemoveFirst() {
        return _delegated.RemoveLast();
    }

    public TKey RemoveLast() {
        return _delegated.RemoveFirst();
    }

    public bool TryRemoveFirst(out TKey item) {
        return _delegated.TryRemoveLast(out item);
    }

    public bool TryRemoveLast(out TKey item) {
        return _delegated.TryRemoveFirst(out item);
    }

    public void Clear() {
        _delegated.Clear();
    }

    #endregion

    #region copyto

    public void CopyTo(TKey[] array, int arrayIndex) {
        _delegated.CopyTo(array, arrayIndex, true);
    }

    public void CopyTo(TKey[] array, int arrayIndex, bool reversed) {
        _delegated.CopyTo(array, arrayIndex, !reversed); // 取反
    }

    #endregion

    #region itr

    public IEnumerator<TKey> GetEnumerator() {
        return _delegated.GetReversedEnumerator();
    }

    public IEnumerator<TKey> GetReversedEnumerator() {
        return _delegated.GetEnumerator();
    }

    #endregion
}