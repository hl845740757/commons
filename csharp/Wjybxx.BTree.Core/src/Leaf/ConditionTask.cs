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

using Wjybxx.Commons;

namespace Wjybxx.BTree.Leaf
{
/// <summary>
/// 条件节点
/// 注意：并非条件节点必须继承该类。
/// 
/// <h3>开销问题</h3>
/// Task类是比较大的，如果项目中有大量的条件，需要考虑开销问题。
/// 一种解决方案是：使用Task类做壳，作为条件测试的入口，内部使用自定义类型。
///<code>
/// public class ConditionEntry&lt;T> extends LeafTask&lt;T> {
///     private int type;
///     private List&lt;ICondition> children = new List&lt;ICondition>(4);
/// }
/// </code>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ConditionTask<T> : LeafTask<T> where T : class
{
    protected sealed override void Execute() {
        int status = Test();
        switch (status) {
            case TaskStatus.NEW:
            case TaskStatus.RUNNING:
            case TaskStatus.CANCELLED: {
                throw new IllegalStateException("Illegal condition status: " + status);
            }
            case TaskStatus.SUCCESS: {
                SetSuccess();
                break;
            }
            default: {
                SetFailed(status);
                break;
            }
        }
    }

    /// <summary>
    /// 检查条件 -- 同步返回
    /// </summary>
    /// <returns>状态码</returns>
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