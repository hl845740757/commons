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
/// 提供常见的拒绝策略实现
/// </summary>
public static class RejectedExecutionHandlers
{
    /// <summary>
    /// 抛出拒绝异常
    /// </summary>
    public static readonly RejectedExecutionHandler ABORT = new AbortHandler();

    /// <summary>
    /// 丢弃被拒绝的任务
    /// </summary>
    public static readonly RejectedExecutionHandler DISCARD = new DiscardHandler();

    /// <summary>
    /// 在调用者线程执行任务（同步立即执行）
    /// </summary>
    public static readonly RejectedExecutionHandler CALLER_RUNS = new CallerRun();

    private class AbortHandler : RejectedExecutionHandler
    {
        public void Rejected(ITask task, IEventLoop eventLoop) {
            throw new RejectedExecutionException();
        }
    }

    private class DiscardHandler : RejectedExecutionHandler
    {
        public void Rejected(ITask task, IEventLoop eventLoop) {
        }
    }

    private class CallerRun : RejectedExecutionHandler
    {
        public void Rejected(ITask task, IEventLoop eventLoop) {
            if (!eventLoop.IsShuttingDown) {
                task.Run();
            }
        }
    }
}
}