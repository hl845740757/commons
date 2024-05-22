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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Pool;

/// <summary>
/// 对象回收句柄
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct RecycleHandle<T> : IDisposable where T : class
{
    /// <summary>
    /// 池化的对象
    /// </summary>
    public readonly T value;
    /// <summary>
    /// 附加上下文
    /// </summary>
    internal readonly object ctx;
    /// <summary>
    /// 归属的池
    /// </summary>
    internal readonly ConcurrentObjectPool<T> pool;

    internal RecycleHandle(T value, object ctx, ConcurrentObjectPool<T> pool) {
        this.value = value;
        this.ctx = ctx;
        this.pool = pool;
    }

    public void Dispose() {
        pool.Release(this); // TODO 重复归还检测
    }
}