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

using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 定时任务调度器，时间单位取决于具体的实现，通常是毫秒 -- 也可能是帧数。
///
/// <h3>时序保证</h3>
/// 1. 单次执行的任务之间，有严格的时序保证，当过期时间(超时时间)相同时，先提交的一定先执行。
/// 2. 周期性执行的的任务，仅首次执行具备时序保证，当进入周期运行时，与其它任务之间便不具备时序保证。
/// </summary>
public interface IUniScheduledExecutor : IUniExecutorService, IScheduledExecutorService
{
}
}