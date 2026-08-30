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
/// 该接口用于处理泛型问题
/// </summary>
public interface IManualResetPromise : IPromise
{
    void Release();
}

/// <summary>
/// 可手动重置状态的Promise。
///
/// Q：为什么是手动重置？
/// A：因为我们无法精确校验<see cref="IPromise{T}"/>中的接口的安全性，即使使用reentryId也不安全。
/// 尤其是有异步回调的情况下，Promise就更难以正确回收。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ManualResetPromise<T> : Promise<T>, IManualResetPromise
{
    // 只可以池中获取
    private ManualResetPromise() {
    }

    /// <summary>
    /// 将Promise归还到池中-用户手动调用
    /// </summary>
    public void Release() {
        POOL?.Release(this);
    }

    #region factory

    private static readonly ConcurrentObjectPool<ManualResetPromise<T>>? POOL;

    static ManualResetPromise() {
        int poolSize = TaskPoolConfig.GetPoolSize<T>(TaskPoolType.ManualResetPromise);
        if (poolSize > 0) {
            POOL = new ConcurrentObjectPool<ManualResetPromise<T>>(() => new ManualResetPromise<T>(), e => e.Reset(), poolSize);
        }
    }
    
    /// <summary>
    /// 从对象池中申请一个Promise
    /// </summary>
    /// <returns></returns>
    public static ManualResetPromise<T> Acquire() {
        return POOL != null ? POOL.Acquire() : new ManualResetPromise<T>();
    }

    /// <summary>
    /// 从对象池中申请一个Promise
    /// </summary>
    /// <param name="e">任务绑定的线程</param>
    /// <returns></returns>
    public static ManualResetPromise<T> Acquire(IExecutor e) {
        ManualResetPromise<T> promise = POOL != null ? POOL.Acquire() : new ManualResetPromise<T>();
        promise.SetExecutor(e);
        return promise;
    }

    #endregion
}
}