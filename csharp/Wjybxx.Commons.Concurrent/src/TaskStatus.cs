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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Future关联的任务的状态
/// </summary>
public enum TaskStatus : byte
{
    /** 任务尚在队列中等待 */
    Pending = 0,

    /** 任务已开始执行 */
    Computing = 1,

    /** 任务执行成功 - 完成状态 */
    Success = 2,

    /** 任务执行失败 - 完成状态 */
    Failed = 3,

    /** 任务被取消 - 完成状态 */
    Cancelled = 4
}
}