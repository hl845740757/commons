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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 由于结构体不能继承，我们通过接口来定义常量。
/// </summary>
public static class TaskBuilder
{
    /// <summary>
    /// 空任务(用于延时任务占位)
    /// </summary>
    public const int TYPE_EMPTY = 0;
    /// <summary>
    /// 表示委托类型为<see cref="Action"/>
    /// </summary>
    public const int TYPE_ACTION = 1;
    /// <summary>
    /// 表示委托类型为<see cref="Action{T}"/>
    /// </summary>
    public const int TYPE_ACTION_CTX = 2;

    /// <summary>
    /// 表示委托类型为<see cref="Func{TResult}"/>
    /// </summary>
    public const int TYPE_FUNC = 3;
    /// <summary>
    /// 表示委托类型为<see cref="Func{T,R}"/>
    /// </summary>
    public const int TYPE_FUNC_CTX = 4;

    /// <summary>
    /// 表示委托类型为<see cref="ITask"/>，通常表示二次封装
    /// </summary>
    public const int TYPE_TASK = 5;
    /// <summary>
    /// 异步任务<see cref="Func{AsyncTaskContext, ValueFuture}"/>
    /// </summary>
    public const int TYPE_ASYNC_TASK = 6;

    #region factory

    public static TaskBuilder<int> NewAction(Action action, CancellationToken cancelToken = default) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION, action, cancelToken);
    }

    public static TaskBuilder<int> NewAction(Action<object> action, object ctx) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_ACTION_CTX, action, ctx);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<T> func, CancellationToken cancelToken = default) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC, func, cancelToken);
    }

    public static TaskBuilder<T> NewFunc<T>(Func<object, T> func, object ctx) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_FUNC_CTX, func, ctx);
    }

    public static TaskBuilder<int> NewTask(ITask task) {
        return new TaskBuilder<int>(TaskBuilder.TYPE_TASK, task);
    }

    internal static TaskBuilder<T> NewAsyncTask<T>(Func<AsyncTaskContext, ValueFuture<T>> task) {
        return new TaskBuilder<T>(TaskBuilder.TYPE_ASYNC_TASK, task);
    }

    #endregion
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
    /// 任务的上下文
    /// </summary>
    public object? Context {
        get => ctx;
        set => ctx = value;
    }

    /// <summary>
    /// 最终options
    /// </summary>
    public int Options {
        get => options;
        set => options = value;
    }

    /// <summary>
    /// 是否启用了某选项
    /// </summary>
    /// <param name="optionMask"></param>
    /// <returns></returns>
    public bool IsEnabled(int optionMask) {
        return (options & optionMask) != 0;
    }

    /// <summary>
    /// 启用或禁用选项
    /// </summary>
    /// <param name="optionMask"></param>
    /// <param name="enable"></param>
    public void SetEnable(int optionMask, bool enable) {
        if (enable) {
            options |= optionMask;
        } else {
            options &= ~optionMask;
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
}
}