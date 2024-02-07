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
using System.Threading;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 提供定时任务支持的Executor
/// </summary>
public interface IScheduledExecutorService : IExecutorService
{
    /// <summary>
    ///
    /// 注意：使用ref仅为了避免防御性拷贝，不会修改对象的状态 —— in关键字可能产生拷贝。
    /// </summary>
    /// <param name="builder">任务构建器</param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IFuture<TResult> Schedule<TResult>(ref ScheduledTaskBuilder<TResult> builder);

    #region action

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture Schedule(Action action, TimeSpan delay, in TaskContext context = default);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture Schedule(Action<TaskContext> action, TimeSpan delay, in TaskContext context);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture<TResult> Schedule<TResult>(Func<TResult> action, TimeSpan delay, in TaskContext context = default);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture<TResult> Schedule<TResult>(Func<TaskContext, TResult> action, TimeSpan delay, in TaskContext context);

    /// <summary>
    /// 按固定延迟执行任务，FixedDelay只保证两次任务的执行间隔一定大于等于给定延迟
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, in TaskContext context = default);


    /// <summary>
    /// 按固定延迟执行任务，FixedDelay只保证两次任务的执行间隔一定大于等于给定延迟
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture ScheduleWithFixedDelay(Action<TaskContext> action, TimeSpan delay, TimeSpan period, in TaskContext context);

    /// <summary>
    /// 按给定频率执行任务，FixedRate
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, in TaskContext context = default);

    /// <summary>
    /// 按给定频率执行任务，FixedRate
    /// 
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    IFuture ScheduleAtFixedRate(Action<TaskContext> action, TimeSpan delay, TimeSpan period, in TaskContext context);

    #endregion
}