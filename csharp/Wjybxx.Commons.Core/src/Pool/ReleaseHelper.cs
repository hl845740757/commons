#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 该结构用于将try-finally语句转换为using语句。
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ReleaseHelper<T> : IDisposable where T : class
{
    private readonly IObjectPool<T>? _pool;
    private readonly T _inst;

    public ReleaseHelper(IObjectPool<T> pool) {
        _inst = pool.Acquire();
        _pool = pool;
    }

    public ReleaseHelper(IObjectPool<T>? pool, T inst) {
        _pool = pool;
        _inst = inst ?? throw new ArgumentNullException(nameof(inst));
    }

    public IObjectPool<T>? Pool => _pool;
    public T Inst => _inst;

    public void Dispose() {
        _pool?.Release(_inst);
    }
}
}