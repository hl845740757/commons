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
/// 该接口表示基于异步状态机的任务
///
/// 该接口借鉴了UniTask的设计，<see cref="Future"/>返回的是<see cref="ValueFuture"/>，
/// 以允许底层实现为更轻量级的
/// </summary>
public interface IStateMachineTask : ITask
{
    /// <summary>
    /// 异步任务关联的Promise
    /// </summary>
    ValueFuture Future { get; }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// 
    /// ps：
    /// 1. 定义为属性以允许实现类进行一些优化，比如：缓存实例。
    /// 2. 通常应该是Run方法的委托。
    /// </summary>
    Action MoveToNext { get; }

    /// <summary>
    /// 设置任务成功完成
    /// </summary>
    void SetResult();

    /// <summary>
    /// 设置任务失败完成
    /// </summary>
    /// <param name="exception"></param>
    void SetException(Exception exception);
}

/// <summary>
/// 该接口表示基于异步状态机的任务
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStateMachineTask<T> : ITask
{
    /// <summary>
    /// 异步任务关联的Promise
    /// </summary>
    ValueFuture<T> Future { get; }

    /// <summary>
    /// 用于驱动StateMachine的Action委托
    /// 
    /// ps：
    /// 1. 定义为属性以允许实现类进行一些优化，比如：缓存实例。
    /// 2. 通常应该是Run方法的委托。
    /// </summary>
    Action MoveToNext { get; }

    /// <summary>
    /// 设置任务成功完成
    /// </summary>
    void SetResult(T? result);

    /// <summary>
    /// 设置任务失败完成
    /// </summary>
    /// <param name="exception"></param>
    void SetException(Exception exception);
}