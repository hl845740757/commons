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

using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 可手动重置状态的Promise。
///
/// Q：为什么是手动重置？
/// A：因为我们无法精确校验<see cref="IPromise{T}"/>中的接口的安全性，即使使用reentryId也不安全。
/// 尤其是有异步回调的情况下，Promise就更难以正确回收。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ManualResetPromise<T> : Promise<T>
{
    /// <summary>
    /// 重置Promise的状态。
    /// 注意：用户只能在Promise不再被使用的情况下调用，否则可能导致难以察觉的bug。
    /// </summary>
    public new void Reset() {
        base.Reset();
    }
}

public sealed class PromisePool<T>
{
    private static readonly ConcurrentObjectPool<ManualResetPromise<T>> POOL = new ConcurrentObjectPool<ManualResetPromise<T>>(
        () => new ManualResetPromise<T>(), (f) => f.Reset(),
        TaskPoolConfig.GetPoolSize<T>(TaskPoolConfig.TaskType.ManualResetPromise));


    /// <summary>
    /// 从对象池中申请一个Promise
    /// </summary>
    /// <returns></returns>
    public static ManualResetPromise<T> Acquire() {
        return POOL.Acquire();
    }

    /// <summary>
    /// 将Promise归还到对象池
    /// PS：会自动调用Promise的Reset方法
    /// </summary>
    /// <param name="promise"></param>
    public static void Release(ManualResetPromise<T> promise) {
        POOL.Release(promise);
    }
}
}