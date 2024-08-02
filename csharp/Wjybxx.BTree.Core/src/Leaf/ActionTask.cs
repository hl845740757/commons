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
/// 行为节点抽象
/// (并非所有行为节点都需要继承该类)
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ActionTask<T> : LeafTask<T> where T : class
{
    protected sealed override void Execute() {
        int reentryId = ReentryId;
        int status = ExecuteImpl();
        if (IsExited(reentryId)) {
            return;
        }
        switch (status) {
            case TaskStatus.NEW: {
                throw new IllegalStateException("Illegal action status: " + status);
            }
            case TaskStatus.RUNNING: {
                break;
            }
            case TaskStatus.SUCCESS: {
                SetSuccess();
                break;
            }
            case TaskStatus.CANCELLED: {
                SetCancelled();
                break;
            }
            default: {
                SetFailed(status);
                break;
            }
        }
    }

    /// <summary>
    /// 我们的大多数行为节点逻辑都较为简单，不需要事件驱动特性，因而可以转换为同步返回的节点。
    /// </summary>
    /// <returns>状态码</returns>
    protected abstract int ExecuteImpl();
}
}