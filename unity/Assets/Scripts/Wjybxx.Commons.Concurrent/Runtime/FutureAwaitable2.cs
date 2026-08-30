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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于绑定回调线程和禁止异常抛出
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct FutureAwaitable2
{
    private readonly IFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <param name="future">future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public FutureAwaitable2(IFuture future, IExecutor? executor, int options = 0) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor;
        _options = options;
    }

    public IFuture Future => _future;
    public IExecutor? Executor => _executor;
    public int Options => _options;

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public FutureAwaitable2 AddOptions(int options) {
        return new FutureAwaitable2(_future, _executor, _options | options);
    }

    /// <summary>
    /// 替换调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public FutureAwaitable2 WithOptions(int options) {
        return new FutureAwaitable2(_future, _executor, options);
    }

    public FutureAwaiter2 GetAwaiter() => new(_future, _executor, _options);
}

/// <summary>
/// 用于绑定回调线程和禁止异常抛出
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct FutureAwaitable2<T>
{
    private readonly IFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <param name="future">future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public FutureAwaitable2(IFuture<T> future, IExecutor? executor, int options = 0) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor;
        _options = options;
    }

    public IFuture<T> Future => _future;
    public IExecutor? Executor => _executor;
    public int Options => _options;

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public FutureAwaitable2<T> AddOptions(int options) {
        return new FutureAwaitable2<T>(_future, _executor, _options | options);
    }

    /// <summary>
    /// 替换调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public FutureAwaitable2<T> WithOptions(int options) {
        return new FutureAwaitable2<T>(_future, _executor, options);
    }

    public FutureAwaiter2<T> GetAwaiter() => new(_future, _executor, _options);
}
}
