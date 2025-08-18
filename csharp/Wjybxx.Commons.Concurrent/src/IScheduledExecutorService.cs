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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 提供定时任务支持的Executor
///
/// 1.调度器什么时候响应取消信号，是不确定的。
/// 2.定时任务可通过<see cref="TaskResultException"/>返回结果。
/// </summary>
public interface IScheduledExecutorService : IExecutorService
{
    /// <summary>
    /// 提交一个任务
    /// </summary>
    /// <param name="builder">任务构建器</param>
    /// <typeparam name="T">结果类型</typeparam>
    /// <returns></returns>
    ValueFuture<T> Schedule<T>(in ScheduledTaskBuilder<T> builder);

    #region action

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="ctx">委托参数，注意<see cref="IConstant"/>类型</param>
    /// <param name="delay">执行延迟</param>
    /// <returns></returns>
    ValueFuture ScheduleAction(Action<object> action, object ctx, TimeSpan delay);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">执行延迟</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture<T> ScheduleFunc<T>(Func<T> action, TimeSpan delay, ICancelToken? cancelToken = null);

    /// <summary>
    /// 在给定的延迟之后执行给定的委托
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="ctx">委托参数，注意<see cref="IConstant"/>类型</param>
    /// <param name="delay">执行延迟</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueFuture<T> ScheduleFunc<T>(Func<object, T> action, object ctx, TimeSpan delay);

    /// <summary>
    /// 以固定延迟执行给定的任务(少执行了就少执行了)
    /// FixedDelay只保证两次任务的执行间隔一定大于等于给定延迟
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null);

    /// <summary>
    /// 以固定频率执行给定的任务（少执行了会补-慎用）
    /// </summary>
    /// <param name="action">要调度的任务</param>
    /// <param name="delay">首次执行延迟</param>
    /// <param name="period">后续执行间隔</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns></returns>
    ValueFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null);

    #endregion
}
}