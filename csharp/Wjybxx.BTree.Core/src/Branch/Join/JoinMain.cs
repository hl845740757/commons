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

using System.Diagnostics;

namespace Wjybxx.BTree.Branch.Join
{
/// <summary>
/// Main策略，当第一个任务完成时就完成。
/// 类似<see cref="SimpleParallel{T}"/>，但Join在得出结果前不重复运行已完成的子节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class JoinMain<T> : JoinPolicy<T> where T : class
{
    /** 单例 */
    private static readonly JoinMain<T> INST = new JoinMain<T>();

    public static JoinMain<T> GetInstance() => INST;

    public void ResetForRestart() {
    }

    public void BeforeEnter(Join<T> join) {
    }

    public void Enter(Join<T> join) {
        if (join.GetChildCount() == 0) {
            join.SetFailed(TaskStatus.CHILDLESS);
        }
    }

    public void OnChildCompleted(Join<T> join, Task<T> child) {
        if (join.IsFirstChild(child)) {
            join.SetCompleted(child.Status, true);
        }
    }

    public void OnEvent(Join<T> join, object eventObj) {
        Task<T> mainTask = join.GetFirstChild();
        ParallelChildHelper<T> childHelper = Parallel<T>.GetChildHelper(mainTask);

        Task<T> inlinedChild = childHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else {
            join.GetFirstChild().OnEvent(eventObj);
        }
    }
}
}