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
/// 任务被事件循环拒绝时的策略
/// </summary>
public interface RejectedExecutionHandler
{
    /// <summary>
    /// 当任务被拒绝时调用
    /// </summary>
    /// <param name="task">被拒绝的任务</param>
    /// <param name="eventLoop">拒绝任务的事件循环</param>
    void Rejected(ITask task, IEventLoop eventLoop);
}