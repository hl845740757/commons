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
    private static volatile Func<TaskType, Type, int>? poolSizeCalculator;

    public static Func<TaskType, Type, int>? PoolSizeCalculator {
        get => poolSizeCalculator;
        set => poolSizeCalculator = value;
    }

    /// <summary>
    /// 计算给定类型的<see cref="IStateMachineDriver{T}"/>的缓存池大小。
    /// 注意：本库默认使用int代替void，因此当T为int类型时，应当分配更大的池。
    /// </summary>
    public static int GetPoolSize<T>(TaskType domain) {
        Func<TaskType, Type, int> func = poolSizeCalculator;
        if (func != null) {
            return Math.Max(0, func.Invoke(domain, typeof(T)));
        }
        return typeof(T) == typeof(int) ? 100 : 50;
    }

    public enum TaskType
    {
        PromiseCompleted,
        UniPromiseCompleted,
        ValueFutureStateMachineDriver,
        PromiseTask,
        ScheduledPromiseTask,
        ValueFutureTask,
        ManualResetPromise,
        ValuePromise,
    }
}
}