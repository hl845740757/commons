﻿#region LICENSE

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
/// 用于简化options的构建
/// </summary>
public struct TaskOptionBuilder
{
    private int options;

    public TaskOptionBuilder() {
        options = 0;
    }

    /// <summary>
    /// 启用选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Enable(int taskOption) {
        options |= taskOption;
    }

    /// <summary>
    /// 禁用选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Disable(int taskOption) {
        options &= ~taskOption;
    }

    /// <summary>
    /// 设置任务的调度阶段
    /// </summary>
    public int SchedulePhase {
        get => TaskOption.SchedulePhase(options);
        set {
            options &= ~ TaskOption.MASK_SCHEDULE_PHASE;
            options |= value;
        }
    }

    /// <summary>
    /// 最终options
    /// </summary>
    public int Options {
        get => options;
        set => options = value;
    }
}