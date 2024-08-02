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

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// <see cref="Join{T}"/>的完成策略
/// </summary>
/// <typeparam name="T"></typeparam>
public interface JoinPolicy<T> where T : class
{
    /** 重置自身数据 */
    void ResetForRestart();

    /** 启动前初始化 */
    void BeforeEnter(Join<T> join);

    /** 启动 */
    void Enter(Join<T> join);

    /** Join在调用该方法前更新了完成计数和成功计数 */
    void OnChildCompleted(Join<T> join, Task<T> child);

    /** join节点收到外部事件 */
    void OnEvent(Join<T> join, object eventObj);
}
}