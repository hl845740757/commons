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
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
public interface IValuePromise
{
    /// <summary>
    /// Promise是否已回收
    /// （限任务的调度者使用，否则可能有线程安全问题）
    /// </summary>
    bool IsRecycled(int rid);

    /// <summary>
    /// Promise是否已回收或已完成
    /// （限任务的调度者使用，否则可能有线程安全问题）
    /// </summary>
    bool IsRecycledOrCompleted(int rid);

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
    /// 返回原始的异常数据
    /// 
    /// 返回值类型：<see cref="OperationCanceledException"/>或<see cref="ExceptionDispatchInfo"/>，
    /// 用于解决C#异常信息传递开销问题。
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="ignoreReentrant"></param>
    /// <returns></returns>
    object GetExceptionOrDispatchInfo(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 如果任务成功完成，则触发回收；如果任务失败（含取消）则抛出异常
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="ignoreReentrant"></param>
    void GetVoidResult(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 获取装箱的结果
    /// </summary>
    /// <param name="reentryId"></param>
    /// <param name="ignoreReentrant"></param>
    /// <returns></returns>
    object GetResult(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 添加一个完成回调
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(int reentryId, Action<object?> continuation, object? state,
                     CancellationToken cancelToken = default, int options = 0);

    /// <summary>
    /// 添加一个完成回调
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(int reentryId, IExecutor executor, Action<object?> continuation, object? state,
                          CancellationToken cancelToken = default, int options = 0);

    /// <summary>
    /// 转换为普通的Future
    /// 注：保留关联的Executor以保留死锁检测能力。
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// </summary>
    IFuture AsFuture(int reentryId);

    /// <summary>
    /// 转换为装箱后普通的Future
    /// 注：保留关联的Executor以保留死锁检测能力。
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <typeparam name="U">下游输入类型</typeparam>
    /// <returns></returns>
    IFuture<U> AsFuture<U>(int reentryId);

    /// <summary>
    /// 用户不需要结果，Promise进入完成状态时即可回收
    /// </summary>
    void Forget(int reentryId);

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
    /// 将future置为计算中状态，如果future之前不处于pending状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">如果future之前不处于pending状态</exception>
    void SetComputing(int reentryId);

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(int reentryId, object? result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">如果Future已完成</exception>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    void SetResult(int reentryId, object? result);

    /// <summary>
    /// 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <returns></returns>
    bool TrySetException(int reentryId, Exception cause);

    /// <summary>
    /// 将future置为失败状态，如果future已进入完成状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="reentryId">重入id</param>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <exception cref="InvalidOperationException">如果Future已完成</exception>
    void SetException(int reentryId, Exception cause);

    /// <summary>
    /// 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
    ///
    /// 注：该接口主要用于避免途中处理异常
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="dispatchInfo">异常信息</param>
    /// <returns></returns>
    bool TrySetException(int reentryId, ExceptionDispatchInfo dispatchInfo);

    /// <summary>
    /// 将future置为失败状态，如果future已进入完成状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="dispatchInfo">异常信息</param>
    void SetException(int reentryId, ExceptionDispatchInfo dispatchInfo);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="reentryId">重入id，校验是否被重用</param>
    /// <param name="cts">相关的取消令牌</param>
    /// <returns></returns>
    bool TrySetCancelled(int reentryId, CancellationToken cts = default);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="reentryId">重入id</param>
    /// <param name="cts">相关的取消令牌</param>
    /// <exception cref="InvalidOperationException">如果Future已完成</exception>
    void SetCancelled(int reentryId, CancellationToken cts = default);

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
    new T GetResult(int reentryId, bool ignoreReentrant = false);

    /// <summary>
    /// 转换为普通的Future
    /// 需要支持死锁检测
    /// </summary>
    new IFuture<T> AsFuture(int reentryId);

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(int reentryId, T result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="InvalidOperationException"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">如果Future已完成</exception>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    void SetResult(int reentryId, T result);

    #region 接口适配

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IValuePromise.TrySetResult(int reentryId, object? result) {
        return TrySetResult(reentryId, result == null ? default : (T)result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IValuePromise.SetResult(int reentryId, object? result) {
        SetResult(reentryId, result == null ? default : (T)result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IValuePromise.GetResult(int reentryId, bool ignoreReentrant) {
        return GetResult(reentryId, ignoreReentrant);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IValuePromise.AsFuture(int reentryId) {
        return AsFuture(reentryId);
    }

    #endregion
}
}