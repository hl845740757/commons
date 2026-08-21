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
using Wjybxx.Commons;

namespace Wjybxx.BTree.Condition
{
public class ConditionNot<T> : ICondition<T>
{
#nullable disable
    [SerializeReference]
    private ICondition<T> child;
#nullable restore
    public ConditionNot() {
    }

    public ConditionNot(ICondition<T> child) {
        this.child = child;
    }

    public int Test(T blackboard) {
        if (child == null) {
            throw new InvalidOperationException("child is null");
        }
        return child.Test(blackboard) == 0 ? TaskStatus.ERROR : 0;
    }

    public ICondition<T> Child {
        get => child;
        set => child = value;
    }
}
}