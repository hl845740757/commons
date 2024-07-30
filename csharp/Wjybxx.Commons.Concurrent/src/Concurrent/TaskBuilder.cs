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

#pragma warning disable CS1591

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
    /// 表示委托类型为<see cref="Action{IContext}"/>
    /// </summary>
    public const int TYPE_ACTION_CTX = 1;

    /// <summary>
    /// 表示委托类型为<see cref="Func{TResult}"/>
    /// </summary>
    public const int TYPE_FUNC = 2;
    /// <summary>
    /// 表示委托类型为<see cref="Func{IContext,TResult}"/>
    /// </summary>
    public const int TYPE_FUNC_CTX = 3;

    /** 分时任务 - 暂未移植到C# */
    public const int TYPE_TIMESHARING = 4;
    /// <summary>
    /// 表示委托类型为<see cref="ITask"/>，通常表示二次封装
    /// </summary>
    public const int TYPE_TASK = 5;

    #region factory

    public static TaskBuilder<int> NewAction(Action action) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION, action);
    }

    public static TaskBuilder<int> NewAction(Action<IContext> action, IContext context) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION_CTX, action, context);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<T> func) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC, func);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<IContext, T> func, IContext context) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC_CTX, func, context);
    }

    public static TaskBuilder<int> NewTask(ITask task) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_TASK, task);
    }

    #endregion

    /// <summary>
    /// 计算Task的类型
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int TaskType(object task) {
        if (task is Action) {
            return TYPE_ACTION;
        }
        if (task is ITask) {
            return TYPE_TASK;
        }
        Type type = task.GetType();
        if (type.IsGenericType) {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(Action<>) && type.GetGenericArguments()[0] == typeof(IContext)) {
                return TYPE_ACTION_CTX;
            }
            if (genericTypeDefinition == typeof(Func<>)) {
                return TYPE_FUNC;
            }
            if (genericTypeDefinition == typeof(Func<,>) && type.GetGenericArguments()[0] == typeof(IContext)) {
                return TYPE_FUNC_CTX;
            }
        }
        throw new ArgumentException("unsupported task type: " + type);
    }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">结果类型，无结果时可使用int，无开销</typeparam>
public struct TaskBuilder<T> : TaskBuilder
{
    private readonly int type;
    private readonly object task;
    private IContext? context;
    private int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">任务的类型</param>
    /// <param name="task">委托</param>
    /// <param name="context">任务的上下文</param>
    internal TaskBuilder(int type, object task, IContext? context = null) {
        this.type = type;
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        this.context = context;
        this.options = 0;
    }

    /// <summary>
    /// 任务的类型
    /// </summary>
    public int Type => type;

    /// <summary>
    /// 委托
    /// </summary>
    public object Task => task;

    /// <summary>
    /// 任务的上下文
    /// 即使用户的任务不接收ctx，executor也可能需要
    /// </summary>
    public IContext? Context {
        get => context;
        set => context = value;
    }

    /// <summary>
    /// 启用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Enable(int optionMask) {
        options |= optionMask;
    }

    /// <summary>
    /// 禁用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Disable(int optionMask) {
        options &= ~optionMask;
    }

    /// <summary>
    /// 设置任务的调度阶段
    /// </summary>
    public int SchedulePhase {
        get => TaskOption.GetSchedulePhase(options);
        set => options = TaskOption.SetSchedulePhase(options, value);
    }

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => TaskOption.GetPriority(options);
        set => options = TaskOption.SetPriority(options, value);
    }

    /// <summary>
    /// 最终options
    /// </summary>
    public int Options {
        get => options;
        set => options = value;
    }
}