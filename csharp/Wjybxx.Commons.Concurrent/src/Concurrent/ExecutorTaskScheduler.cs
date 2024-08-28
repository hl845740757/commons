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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于适配C#的系统Task库
/// </summary>
public class ExecutorTaskScheduler : TaskScheduler
{
    protected readonly IExecutor _executor;

    public ExecutorTaskScheduler(IExecutor executor) {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// ps：该接口由Task框架调用
    /// </summary>
    /// <param name="task"></param>
    protected override void QueueTask(Task task) {
        _executor.Execute(new TaskWrapper(this, task, 0));
    }

    /// <summary>
    /// 向Executor中插入一个Task
    ///
    /// ps: 虽然设计为开放接口，以允许用户调用 —— 但尽量还是少使用系统库的Task。
    /// </summary>
    /// <param name="task">要调度的任务</param>
    /// <param name="options">任务的调度选项</param>
    public void QueueTask(Task task, int options) {
        _executor.Execute(new TaskWrapper(this, task, options));
    }

    /// <summary>
    /// 默认不内联，保证时序的确定性。
    /// 
    /// PS：C#的该接口设计我认为是糟糕的，时序的控制居然不在用户，而是TaskScheduler自行决定。
    /// </summary>
    /// <param name="task"></param>
    /// <param name="taskWasPreviouslyQueued"></param>
    /// <returns></returns>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
        return false; // 不内联有可能产生死锁问题，如果EventLoop在启动的过程中触发Task.Wait
    }

    /// <summary>
    /// 该方法仅用于debug，可不实现
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<Task>? GetScheduledTasks() {
        return null;
    }

    private class TaskWrapper : ITask
    {
        private readonly ExecutorTaskScheduler _taskScheduler;
        private readonly Task _task;
        private readonly int _options;

        public TaskWrapper(ExecutorTaskScheduler taskScheduler, Task task, int options) {
            _taskScheduler = taskScheduler;
            _task = task;
            _options = options;
        }

        public int Options => _options;

        public void Run() {
            // 由于用户直接持有Task的引用，因此无需特殊处理取消
            _taskScheduler.TryExecuteTask(_task);
        }
    }
}
}