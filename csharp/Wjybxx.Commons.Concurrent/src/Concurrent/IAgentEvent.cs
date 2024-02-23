#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// <see cref="IEventLoopAgent{T}"/>接收的事件类型。
///
/// ps：实现类可能还需要实现<see cref="ITask"/>。
/// </summary>
public interface IAgentEvent
{
    /** 表示事件无效 */
    const int TYPE_INVALID = -1;
    /** 表示普通的任务 */
    const int TYPE_RUNNABLE = 0;

    /// <summary>
    /// 事件的类型
    ///
    /// ps：应当慎用负数类型，否否则可能影响事件循环的工作。
    /// </summary>
    int Type { get; set; }

    /// <summary>
    /// 事件或任务的调度选项
    ///
    /// 1.将options存储在Event上，可支持自定义事件中的调度选项；冗余存储，可解除耦合。
    /// 2.可避免对部分类型事件的封装
    /// </summary>
    int Options { get; set; }

    /// <summary>
    /// 事件的第一个参数
    /// </summary>
    object Obj0 { get; set; }

    /// <summary>
    /// 清理事件的引用数据 -- 避免内存泄漏
    /// </summary>
    void Clean();

    /// <summary>
    /// 清理事件的所有数据 -- 基础值也重置
    /// </summary>
    void CleanAll();
}