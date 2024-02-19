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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 定时任务构建器
/// </summary>
/// <typeparam name="T"></typeparam>
public struct ScheduledTaskBuilder<T>
{
    /** 不能为readonly否则调用方法会产生拷贝 */
    private TaskBuilder<T> _core;

    private ScheduledTaskBuilder(TaskBuilder<T> core) {
        _core = core;
    }

    public int Type => _core.Type;

    public object Task => _core.Task;

    public IContext? Context => _core.Context;

    public int Options => _core.Options;

    public void Enable(int taskOption) {
        _core.Enable(taskOption);
    }

    public void Disable(int taskOption) {
        _core.Disable(taskOption);
    }

    public int SchedulePhase => _core.SchedulePhase;
}