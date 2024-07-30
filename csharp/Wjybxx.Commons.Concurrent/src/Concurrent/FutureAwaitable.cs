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
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct FutureAwaitable
{
    private readonly IFuture _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    public FutureAwaitable(IFuture future, IExecutor executor, int options) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public FutureAwaiter GetAwaiter() => new FutureAwaiter(_future, _executor, _options);
}

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct FutureAwaitable<T>
{
    private readonly IFuture<T> _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    public FutureAwaitable(IFuture<T> future, IExecutor executor, int options) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public FutureAwaiter<T> GetAwaiter() => new FutureAwaiter<T>(_future, _executor, _options);
}
}