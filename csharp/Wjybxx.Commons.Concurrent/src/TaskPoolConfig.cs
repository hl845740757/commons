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
/// 用于配置工具库中的对象池大小
/// </summary>
public static class TaskPoolConfig
{
    private static volatile Func<TaskPoolType, Type, int>? poolSizeCalculator;

    public static Func<TaskPoolType, Type, int>? PoolSizeCalculator {
        get => poolSizeCalculator;
        set => poolSizeCalculator = value;
    }

    /// <summary>
    /// 计算给定类型对象池的缓存池大小。
    /// 
    /// 注意：本库统一使用int代替void，因此当T为int类型时，应当分配更大的池。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <typeparam name="T">对象的泛型参数类型</typeparam>
    public static int GetPoolSize<T>(TaskPoolType poolType) {
        Func<TaskPoolType, Type, int> func = poolSizeCalculator;
        if (func != null) {
            return Math.Max(0, func.Invoke(poolType, typeof(T)));
        }
        // 通常使用int代替void，而object适用装箱场景
        bool isIntOrObject = typeof(T) == typeof(int) || typeof(T) == typeof(object);
        if (poolType == TaskPoolType.ValuePromise
            || poolType == TaskPoolType.PromiseMoveNext) {
            return isIntOrObject ? 1000 : 50;
        }
        if (poolType == TaskPoolType.CtsCompletion) {
            return 500; // 不区分泛型
        }
        return isIntOrObject ? 100 : 20;
    }
}

/// <summary>
/// 任务池类型
/// </summary>
public enum TaskPoolType
{
    /// <summary>
    /// 普通Future的状态机await回调
    /// </summary>
    PromiseMoveNext,
    /// <summary>
    /// ValueFuture状态机任务
    /// </summary>
    ValueFutureStateMachineTask,

    /// <summary>
    /// 最基础的<see cref="ValuePromise{T}"/>回调
    /// </summary>
    ValuePromise,
    /// <summary>
    /// 池化的普通任务
    /// <see cref="PromiseTask{T}"/>
    /// </summary>
    PromiseTask,
    /// <summary>
    /// 池化的定时任务
    /// <see cref="ScheduledPromiseTask{T}"/>
    /// </summary>
    ScheduledPromiseTask,

    /// <summary>
    /// <see cref="ManualResetPromise{T}"/>
    /// </summary>
    ManualResetPromise,
    /// <summary>
    /// 取消令牌的监听器
    /// </summary>
    CtsCompletion,

    /// <summary>
    /// 协程任务(分时任务的新实现)
    /// </summary>
    Coroutine,
}
}