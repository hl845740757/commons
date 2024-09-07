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

namespace Wjybxx.BTree.Branch.Join
{
/// <summary>
/// 等待所有任务完成后返回成功
/// 相当于并发编程中的WaitAll
/// </summary>
/// <typeparam name="T"></typeparam>
public class JoinWaitAll<T> : JoinPolicy<T> where T : class
{
    /** 单例 */
    private static readonly JoinWaitAll<T> INST = new JoinWaitAll<T>();

    public static JoinWaitAll<T> GetInstance() => INST;

    public void ResetForRestart() {
    }

    public void BeforeEnter(Join<T> join) {
    }

    public void Enter(Join<T> join) {
        if (join.ChildCount == 0) {
            join.SetSuccess();
        }
    }

    public void OnChildCompleted(Join<T> join, Task<T> child) {
        if (join.IsAllChildCompleted) {
            join.SetSuccess();
        }
    }

    public void OnEvent(Join<T> join, object eventObj) {
    }
}
}