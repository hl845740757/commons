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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Concurrent
{
public interface IValuePromise
{
    #region future

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
    /// <param name="options">调度选项</param>
    void OnCompleted(int reentryId, Action<object?> continuation, object? state, int options = 0);

    /// <summary>
    /// 添加一个完成回调
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(int reentryId, IExecutor executor, Action<object?> continuation, object? state, int options = 0);

    /// <summary>
    /// 用于传输结果
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="promise"></param>
    void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise);

    #endregion

    #region promise

    /// <summary>
    /// 尝试将future置为正在计算状态
    /// 只有成功将future从pending状态更新为computing状态时返回true
    /// </summary>
    /// <returns></returns>
    bool TrySetComputing(int reentryId);

    /// <summary>
    /// 尝试将future置为正在计算状态
    /// 该接口有更好的返回值，不过一般情况下还是推荐<see cref="TrySetComputing"/>
    /// </summary>
    /// <returns>之前的状态</returns>
    TaskStatus TrySetComputing2(int reentryId);

    /// <summary>
    /// 将future置为计算中状态，如果future之前不处于pending状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <exception cref="IllegalStateException">如果future之前不处于pending状态</exception>
    void SetComputing(int reentryId);

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(int reentryId, object result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    void SetResult(int reentryId, object result);

    /// <summary>
    /// 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <returns></returns>
    bool TrySetException(int reentryId, Exception cause);

    /// <summary>
    /// 将future置为失败状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="reentryId">重入id</param>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    void SetException(int reentryId, Exception cause);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cancelCode">相关的取消码</param>
    /// <returns></returns>
    bool TrySetCancelled(int reentryId, int cancelCode);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="reentryId">重入id</param>
    /// <param name="cancelCode">相关的取消码</param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    void SetCancelled(int reentryId, int cancelCode);

    #endregion
}

/// <summary>
/// 与通用的的Promise不同，
/// ValuePromise和ValueFuture之间为组合关系，目的在于池化Promise。
///
/// 1.所有的读写方法都需要验证重用id。
/// 2.Promise不应该返回给用户，多返回给用户<see cref="ValueFuture{T}"/>。
/// 3.不支持阻塞获取结果。
/// 4.在用户获取结果后触发回收。
/// 5.主要用于状态机等场景。
///  
/// ps: 框架统一使用int代替void。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IValuePromise<T> : IValuePromise
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

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(int reentryId, T result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    void SetResult(int reentryId, T result);

    #region 接口适配

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IValuePromise.TrySetResult(int reentryId, object result) {
        return TrySetResult(reentryId, (T)result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IValuePromise.SetResult(int reentryId, object result) {
        SetResult(reentryId, (T)result);
    }

    #endregion
}
}