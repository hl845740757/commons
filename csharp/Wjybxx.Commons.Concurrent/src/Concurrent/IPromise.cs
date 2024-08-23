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
/// 1. 该非泛型接口用于支持统一操作，不提供特殊实现。
/// 2. void可通过byte/int/bool泛型替代 -- 推荐byte。
/// 3. 只有null是安全的，其它值都不一定安全。
/// </summary>
public interface IPromise : IFuture
{
#nullable disable
    /// <summary>
    /// 尝试将future置为正在计算状态
    /// 只有成功将future从pending状态更新为computing状态时返回true
    /// </summary>
    /// <returns></returns>
    bool TrySetComputing() {
        return TrySetComputing2() == TaskStatus.Pending;
    }

    /// <summary>
    /// 尝试将future置为正在计算状态
    /// 该接口有更好的返回值，不过一般情况下还是推荐<see cref="TrySetComputing"/>
    /// </summary>
    /// <returns>之前的状态</returns>
    TaskStatus TrySetComputing2();

    /// <summary>
    /// 将future置为计算中状态，如果future之前不处于pending状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <exception cref="IllegalStateException">如果future之前不处于pending状态</exception>
    void SetComputing();

    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    bool TrySetResult(object result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="result"></param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    /// <exception cref="InvalidCastException">如果数据类型不兼容</exception>
    void SetResult(object result);

    /// <summary>
    /// 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <returns></returns>
    bool TrySetException(Exception cause);

    /// <summary>
    /// 将future置为失败状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="cause">任务失败的原因，如果为<see cref="OperationCanceledException"/>，则等同于取消</param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    void SetException(Exception cause);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则返回false
    /// </summary>
    /// <param name="cancelCode">相关的取消码</param>
    /// <returns></returns>
    bool TrySetCancelled(int cancelCode);

    /// <summary>
    /// 将Future置为已取消状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="cancelCode">相关的取消码</param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    void SetCancelled(int cancelCode);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
public interface IPromise<T> : IFuture<T>, IPromise
{
    /// <summary>
    /// 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
    /// </summary>
    bool TrySetResult(T result);

    /// <summary>
    /// 将future置为成功完成状态，如果future已进入完成状态，则抛出<see cref="IllegalStateException"/>
    /// </summary>
    /// <param name="result"></param>
    /// <exception cref="IllegalStateException">如果Future已完成</exception>
    void SetResult(T result);

    #region 接口适配

    bool IPromise.TrySetResult(object result) {
        return TrySetResult((T)result);
    }

    void IPromise.SetResult(object result) {
        SetResult((T)result);
    }

    #endregion
}
}