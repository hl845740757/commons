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

namespace Wjybxx.BTree.Leaf
{
/// <summary>
/// 条件节点
/// 1. 大多数条件节点都只需要返回bool值，不需要详细的错误码，因此提供该模板实现。
/// 2. 并非所有条件节点都需要继承该类。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ConditionTask2<T> : LeafTask<T> where T : class
{
    protected sealed override void Execute() {
        int status = Test();
        if (status == TaskStatus.SUCCESS) {
            SetSuccess();
        } else {
            SetFailed(status);
        }
    }

    /// <summary>
    /// 检查条件 -- 同步返回
    /// </summary>
    /// <returns></returns>
    protected abstract int Test();

    /** 条件节点正常情况下不会触发事件 */
    public override bool CanHandleEvent(object _) {
        return false;
    }

    /** 条件节点正常情况下不会触发事件 */
    protected override void OnEventImpl(object _) {
    }
}
}