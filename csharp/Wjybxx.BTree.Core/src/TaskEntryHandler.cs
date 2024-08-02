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

namespace Wjybxx.BTree
{
/// <summary>
/// <see cref="TaskEntry{T}"/>的事件处理
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ITaskEntryHandler<T> where T : class
{
    /// <summary>
    /// 任务进入完成状态
    /// </summary>
    /// <param name="taskEntry"></param>
    void OnCompleted(TaskEntry<T> taskEntry);

    /// <summary>
    /// 用于C#端支持await语法
    /// (实现时小心时序问题)
    /// </summary>
    /// <param name="taskEntry"></param>
    /// <param name="action"></param>
    void AwaitOnCompleted(TaskEntry<T> taskEntry, Action action) {
        throw new NotImplementedException();
    }
}
}