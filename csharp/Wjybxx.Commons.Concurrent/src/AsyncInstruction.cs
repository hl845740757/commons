#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 异步操作命令
/// (以后再扩展其它行为)
/// </summary>
internal sealed class AsyncInstruction : IIndexedElement
{
    public static Func<AsyncInstruction> Factory { get; } = () => new AsyncInstruction();
    public static Action<AsyncInstruction> Cleaner { get; } = e => e.Reset();

#nullable disable
    private int qIndex = -1;
    internal long id;
    internal long triggerTime;
    internal ValuePromise<int> promise;
    internal ICancelToken? cancelToken;
#nullable restore

    private void Reset() {
        qIndex = -1;
        id = 0;
        triggerTime = 0;
        promise = null;
        cancelToken = null;
    }

    public int CollectionIndex(object collection) {
        return qIndex;
    }

    public void CollectionIndex(object collection, int index) {
        qIndex = index;
    }
}

internal class InstructionComparer : IComparer<AsyncInstruction>
{
    public static readonly InstructionComparer Inst = new InstructionComparer();

    public int Compare(AsyncInstruction? lhs, AsyncInstruction? rhs) {
        if (lhs == null) throw new ArgumentNullException(nameof(lhs));
        if (rhs == null) throw new ArgumentNullException(nameof(rhs));
        if (ReferenceEquals(lhs, rhs)) {
            return 0;
        }
        int r = lhs.triggerTime.CompareTo(rhs.triggerTime);
        if (r != 0) {
            return r;
        }
        // 再按id排序
        r = lhs.id.CompareTo(rhs.id);
        if (r == 0) {
            throw new InvalidOperationException($"lhs.id: {lhs.id}, rhs.id: {rhs.id}");
        }
        return r;
    }
}
}