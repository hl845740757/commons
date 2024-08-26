#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
/// 可池化的Future
/// </summary>
public interface IPoolableFuture
{
    /// <summary>
    /// 获取返回给用户的句柄
    /// </summary>
    ValueFuture VoidFuture { get; }

    /// <summary>
    /// 获取任务的状态
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="ignoreReentrant">是否跳过重入检测</param>
    /// <returns></returns>
    TaskStatus GetStatus(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 获取失败的异常
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="ignoreReentrant"></param>
    /// <returns></returns>
    Exception GetException(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 如果任务成功完成，则触发回收；如果任务失败（含取消）则抛出异常
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="ignoreReentrant"></param>
    void GetVoidResult(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 添加一个完成回调
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(int reentryId, Action<object?> continuation, object? state,
                     IExecutor? executor, int options = 0);

    /// <summary>
    /// 用于传输结果
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="promise"></param>
    void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise);
}

/// <summary>
/// 可池化的Future
///
/// 1.所有的读写方法都需要验证重用id。
/// 2.在用户获取结果后触发回收。
/// 3.不支持阻塞获取结果。
/// 4.通常不返回给用户，多返回给用户<see cref="ValueFuture{T}"/>。
/// 5.主要用于状态机等场景。
/// 
/// ps: 框架统一使用int代替void。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPoolableFuture<T> : IPoolableFuture
{
    /// <summary>
    /// 获取返回给用户的句柄
    /// </summary>
    ValueFuture<T> Future { get; }

    /// <summary>
    /// 获取任务的结果
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="ignoreReentrant">是否忽略重入检测</param>
    /// <returns></returns>
    T GetResult(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 用于传输结果
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="promise"></param>
    void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise);
}
}