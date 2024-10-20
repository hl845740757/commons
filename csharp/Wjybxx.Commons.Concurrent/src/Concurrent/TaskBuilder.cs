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

using System;

namespace Wjybxx.Commons.Concurrent
{
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

    /** 分时任务 */
    public const int TYPE_TIMESHARING = 4;
    /// <summary>
    /// 表示委托类型为<see cref="ITask"/>，通常表示二次封装
    /// </summary>
    public const int TYPE_TASK = 5;

    #region factory

    public static TaskBuilder<int> NewAction(Action action, ICancelToken? cancelToken = null) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION, action, cancelToken);
    }

    public static TaskBuilder<int> NewAction(Action<IContext> action, IContext context) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION_CTX, action, context);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<T> func, ICancelToken? cancelToken = null) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC, func, cancelToken);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<IContext, T> func, IContext context) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC_CTX, func, context);
    }

    public static TaskBuilder<T> NewTimeSharing<T>(TimeSharingTask<T> func, IContext? context = null) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_TIMESHARING, func, context);
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
            if (genericTypeDefinition == typeof(Action<>) && type.GenericTypeArguments[0] == typeof(IContext)) {
                return TYPE_ACTION_CTX;
            }
            if (genericTypeDefinition == typeof(Func<>)) {
                return TYPE_FUNC;
            }
            if (genericTypeDefinition == typeof(Func<,>) && type.GenericTypeArguments[0] == typeof(IContext)) {
                return TYPE_FUNC_CTX;
            }
            if (genericTypeDefinition == typeof(TimeSharingTask<>)) {
                return TYPE_TIMESHARING;
            }
        }
        throw new ArgumentException("unsupported task type: " + type);
    }

    /** 任务是否接收context类型参数 */
    public static bool IsTaskAcceptContext(int type) {
        switch (type) {
            case TYPE_ACTION_CTX:
            case TYPE_FUNC_CTX:
            case TYPE_TIMESHARING: {
                return true;
            }
            default: {
                return false;
            }
        }
    }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">结果类型，无结果时可使用int，无开销</typeparam>
public struct TaskBuilder<T>
{
    private readonly int type;
    private readonly object task;
    private object? ctx;
    private int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">任务的类型</param>
    /// <param name="task">委托</param>
    /// <param name="ctx">任务的上下文</param>
    internal TaskBuilder(int type, object task, object? ctx = null) {
        this.type = type;
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        this.ctx = ctx;
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
    /// 任务是否接收context类型参数
    /// </summary>
    public bool IsTaskAcceptContext => TaskBuilder.IsTaskAcceptContext(type);

    /// <summary>
    /// 任务的上下文
    /// </summary>
    public IContext? Context {
        get => IsTaskAcceptContext ? (IContext?)ctx : null;
        set {
            if (!IsTaskAcceptContext) {
                throw new IllegalStateException();
            }
            this.ctx = value ?? IContext.NONE;
        }
    }

    /// <summary>
    /// 任务绑定的取消令牌
    /// </summary>
    /// <exception cref="IllegalStateException"></exception>
    public ICancelToken? CancelToken {
        get => IsTaskAcceptContext ? null : (ICancelToken)ctx;
        set {
            if (IsTaskAcceptContext) {
                throw new IllegalStateException();
            }
            this.ctx = value ?? ICancelToken.NONE;
        }
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
        get => TaskOptions.GetSchedulePhase(options);
        set => options = TaskOptions.SetSchedulePhase(options, value);
    }

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => TaskOptions.GetPriority(options);
        set => options = TaskOptions.SetPriority(options, value);
    }

    /// <summary>
    /// 最终options
    /// </summary>
    public int Options {
        get => options;
        set => options = value;
    }
}
}