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
/// 序列集合的反转视图
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class ReversedSequenceSetView<TKey> : ReversedCollectionView<TKey>, ISequencedSet<TKey>
{
    public ReversedSequenceSetView(ISequencedSet<TKey> hashSet) :
        base(hashSet) {
    }

    private ISequencedSet<TKey> Delegated => (ISequencedSet<TKey>)_delegated;

    public override ISequencedSet<TKey> Reversed() {
        return (ISequencedSet<TKey>)_delegated;
    }

    public new virtual bool Add(TKey item) {
        return Delegated.Add(item); // 允许重写
    }

    public new bool AddFirst(TKey item) {
        return Delegated.AddLast(item);
    }

    public new bool AddLast(TKey item) {
        return Delegated.AddFirst(item);
    }

    public bool AddFirstIfAbsent(TKey item) {
        return Delegated.AddLastIfAbsent(item);
    }

    public bool AddLastIfAbsent(TKey item) {
        return Delegated.AddFirstIfAbsent(item);
    }
}