#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BTree
{
/// <summary>
/// 叶子节点的超类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class LeafTask<T> : Task<T> where T : class
{
    protected sealed override void OnChildRunning(Task<T> child, bool starting) {
        throw new AssertionError();
    }

    protected sealed override void OnChildCompleted(Task<T> child) {
        throw new AssertionError();
    }

#nullable disable

    #region child

    public override void VisitChildren(TaskVisitor<T> visitor, object param) {
    }

    public sealed override int IndexChild(Task<T> task) {
        return -1;
    }

    public override int ChildCount => 0;

    public sealed override Task<T> GetChild(int index) {
        throw new IndexOutOfRangeException("Leaf task can not have any children");
    }

    protected sealed override int AddChildImpl(Task<T> task) {
        throw new IllegalStateException("Leaf task can not have any children");
    }

    protected sealed override Task<T> SetChildImpl(int index, Task<T> task) {
        throw new IllegalStateException("Leaf task can not have any children");
    }

    protected sealed override Task<T> RemoveChildImpl(int index) {
        throw new IndexOutOfRangeException("Leaf task can not have any children");
    }

    #endregion
}
}