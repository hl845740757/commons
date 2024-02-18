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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 由于结构体不能继承，我们通过接口来定义常量。
/// </summary>
public interface TaskBuilder
{
    /// <summary>
    /// 表示委托类型为<see cref="Action"/>
    /// </summary>
    public const int TYPE_ACTION = 0;
    /// <summary>
    /// 表示委托类型为<see cref="Action{TaskContext}"/>
    /// </summary>
    public const int TYPE_ACTION_CTX = 1;

    /// <summary>
    /// 表示委托类型为<see cref="Func{TResult}"/>
    /// </summary>
    public const int TYPE_FUNC = 2;
    /// <summary>
    /// 表示委托类型为<see cref="Func{TaskContext,TResult}"/>
    /// </summary>
    public const int TYPE_FUNC_CTX = 3;
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">结果类型，无结果时可使用object，无开销</typeparam>
public struct TaskBuilder<T> : TaskBuilder
{
    private readonly int type;
    private readonly Delegate action;
    private readonly TaskContext context;
    private int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">任务的类型</param>
    /// <param name="action">委托</param>
    /// <param name="context">任务的上下文</param>
    private TaskBuilder(int type, Delegate action, TaskContext context = default) {
        this.type = type;
        this.action = action;
        this.context = context;
    }

    public static ref TaskBuilder<T> newFunc(Func<T> func) {
        ref TaskBuilder<T> builder = new TaskBuilder<T>(TaskBuilder.TYPE_FUNC, func, null);
        return ref builder;
    }

    public static ref TaskBuilder<T> newFunc(Func<T> func, IContext context) {
        ref TaskBuilder<T> builder = new TaskBuilder<T>(TaskBuilder.TYPE_FUNC_CTX, func, context);
        return ref builder;
    }

    public static ref TaskBuilder<T> newAction(Action func) {
        ref TaskBuilder<T> builder = new TaskBuilder<T>(TaskBuilder.TYPE_ACTION, func, null);
        return ref builder;
    }

    public static ref TaskBuilder<T> newAction(Action<T> func) {
        ref TaskBuilder<T> builder = new TaskBuilder<T>(TaskBuilder.TYPE_ACTION_CTX, func, null);
        return ref builder;
    }

    /// <summary>
    /// 任务的类型
    /// </summary>
    public int Type => type;

    /// <summary>
    /// 委托
    /// </summary>
    public Delegate Action => action;

    /// <summary>
    /// 委托的上下文
    /// </summary>
    public TaskContext Context => context;

    /// <summary>
    /// 任务的调度选项
    /// </summary>
    public int Options {
        get => options;
        set => options = value;
    }

    /// <summary>
    /// 启用特定任务选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Enable(int taskOption) {
        this.options = TaskOption.Enable(options, taskOption);
    }

    /// <summary>
    /// 关闭特定任务选项
    /// </summary>
    /// <param name="taskOption"></param>
    public void Disable(int taskOption) {
        this.options = TaskOption.Disable(options, taskOption);
    }

    /// <summary>
    /// 设置options中任务期望的调度阶段
    /// </summary>
    public int SchedulePhase {
        get => options & TaskOption.MASK_SCHEDULE_PHASE;
        set {
            this.options &= ~TaskOption.MASK_SCHEDULE_PHASE;
            this.options |= value;
        }
    }
}