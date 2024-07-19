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
using System.Runtime.CompilerServices;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct ValueFutureAwaitable
{
    private readonly ValueFuture _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    public ValueFutureAwaitable(ValueFuture future, IExecutor executor, int options) {
        _future = future;
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public ValueFutureAwaiter GetAwaiter() => new ValueFutureAwaiter(_future, _executor, _options);
}

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct ValueFutureAwaitable<T>
{
    private readonly ValueFuture<T> _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    public ValueFutureAwaitable(ValueFuture<T> future, IExecutor executor, int options) {
        _future = future;
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public ValueFutureAwaiter<T> GetAwaiter() => new ValueFutureAwaiter<T>(_future, _executor, _options);
}