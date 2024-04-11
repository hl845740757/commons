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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 序列集合的反转视图
/// </summary>
/// <typeparam name="TKey">元素的类型</typeparam>
public class ReversedCollectionView<TKey> : ISequencedCollection<TKey>
{
    protected readonly ISequencedCollection<TKey> delegated;

    public ReversedCollectionView(ISequencedCollection<TKey> delegated) {
        this.delegated = delegated ?? throw new ArgumentNullException(nameof(delegated));
    }

    public int Count => delegated.Count;
    public bool IsEmpty => delegated.IsEmpty;
    public bool IsReadOnly => false;

    public virtual ISequencedCollection<TKey> Reversed() {
        return delegated;
    }

    public void AdjustCapacity(int expectedCount) {
        delegated.AdjustCapacity(expectedCount);
    }

    #region get

    public TKey PeekFirst() => delegated.PeekLast();

    public TKey PeekLast() => delegated.PeekFirst();

    public bool TryPeekFirst(out TKey item) {
        return delegated.TryPeekLast(out item);
    }

    public bool TryPeekLast(out TKey item) {
        return delegated.TryPeekFirst(out item);
    }

    public bool Contains(TKey item) {
        return delegated.Contains(item);
    }

    #endregion

    #region add

    public virtual void Add(TKey item) {
        delegated.Add(item); // 允许重写-部分集合的Add是隐含方向的
    }

    public void AddFirst(TKey item) {
        delegated.AddLast(item);
    }

    public void AddLast(TKey item) {
        delegated.AddFirst(item);
    }

    #endregion

    #region remove

    public bool Remove(TKey item) {
        return delegated.Remove(item);
    }

    public TKey RemoveFirst() {
        return delegated.RemoveLast();
    }

    public TKey RemoveLast() {
        return delegated.RemoveFirst();
    }

    public bool TryRemoveFirst(out TKey item) {
        return delegated.TryRemoveLast(out item);
    }

    public bool TryRemoveLast(out TKey item) {
        return delegated.TryRemoveFirst(out item);
    }

    public void Clear() {
        delegated.Clear();
    }

    #endregion

    #region copyto

    public void CopyTo(TKey[] array, int arrayIndex) {
        delegated.CopyTo(array, arrayIndex, true);
    }

    public void CopyTo(TKey[] array, int arrayIndex, bool reversed) {
        delegated.CopyTo(array, arrayIndex, !reversed); // 取反
    }

    #endregion

    #region itr

    public IEnumerator<TKey> GetEnumerator() {
        return delegated.GetReversedEnumerator();
    }

    public IEnumerator<TKey> GetReversedEnumerator() {
        return delegated.GetEnumerator();
    }

    #endregion
}