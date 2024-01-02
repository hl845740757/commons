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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// Deque反转视图
/// 注意：Queue和Stack接口中的方法都是明确方向的，因此可反转；
/// 不过，Add方法只能按照Queue的语义反转，不能按照Stack的语义反转。
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class ReversedDequeView<TKey> : ReversedCollectionView<TKey>, IDeque<TKey>
{
    public ReversedDequeView(IDeque<TKey> deque) :
        base(deque) {
    }

    private IDeque<TKey> Delegated => (IDeque<TKey>)_delegated;

    public override IDeque<TKey> Reversed() {
        return (IDeque<TKey>)_delegated;
    }

    #region dequeue

    /** Deque的Add操作插在队列的尾部，是明确方向的 */
    public override void Add(TKey item) {
        Delegated.AddFirst(item);
    }

    public bool TryAddFirst(TKey item) {
        return Delegated.TryAddLast(item);
    }

    public bool TryAddLast(TKey item) {
        return Delegated.TryAddFirst(item);
    }

    #endregion

    #region queue

    public void Enqueue(TKey item) {
        Delegated.AddFirst(item);
    }

    public bool TryEnqueue(TKey item) {
        return Delegated.TryAddFirst(item);
    }

    public TKey Dequeue() {
        return Delegated.RemoveLast();
    }

    public bool TryDequeue(out TKey item) {
        return Delegated.TryRemoveLast(out item);
    }

    public TKey PeekHead() {
        return Delegated.PeekLast();
    }

    public bool TryPeekHead(out TKey item) {
        return Delegated.TryPeekLast(out item);
    }

    #endregion

    #region stack

    public void Push(TKey item) {
        Delegated.AddLast(item);
    }

    public bool TryPush(TKey item) {
        return Delegated.TryAddLast(item);
    }

    public TKey Pop() {
        return Delegated.RemoveLast();
    }

    public bool TryPop(out TKey item) {
        return Delegated.TryRemoveLast(out item);
    }

    public TKey PeekTop() {
        return Delegated.PeekLast();
    }

    public bool TryPeekTop(out TKey item) {
        return Delegated.TryPeekLast(out item);
    }

    #endregion
}