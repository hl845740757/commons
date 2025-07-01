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
/// 用于绑定回调线程和禁止异常抛出
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct ValueFutureAwaitable2
{
    private readonly ValueFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;
    private readonly bool _requireResult;

    /// <param name="future">future</param>
    /// <param name="requireResult">是否需要获取最终结果</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public ValueFutureAwaitable2(ValueFuture future, bool requireResult, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
        _requireResult = requireResult;
    }

    public ValueFuture Future => _future;
    public IExecutor? Executor => _executor;
    public int Options => _options;
    public bool RequireResult => _requireResult;

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValueFutureAwaitable2 AddOptions(int options) {
        return new ValueFutureAwaitable2(_future, _requireResult, _executor, _options | options);
    }

    /// <summary>
    /// 替换调度选项(保留异常禁用信息)
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValueFutureAwaitable2 WithOptions(int options) {
        int suppressed = _options & (int)SuppressedTypes.All;
        return new ValueFutureAwaitable2(_future, _requireResult, _executor, suppressed | options);
    }

    /// <summary>
    /// 设置是否获取最终的结果
    /// </summary>
    /// <param name="requireResult"></param>
    /// <returns></returns>
    public ValueFutureAwaitable2 WithRequireResult(bool requireResult = true) {
        return new ValueFutureAwaitable2(_future, requireResult, _executor, _options);
    }

    public ValueFutureAwaiter2 GetAwaiter() => new(_future, _requireResult, _executor, _options);
}

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct ValueFutureAwaitable2<T>
{
    private readonly ValueFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    public ValueFutureAwaitable2(ValueFuture<T> future, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
    }

    public ValueFuture<T> Future => _future;
    public IExecutor? Executor => _executor;
    public int Options => _options;

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValueFutureAwaitable2<T> AddOptions(int options) {
        return new ValueFutureAwaitable2<T>(_future, _executor, _options | options);
    }

    /// <summary>
    /// 替换调度选项(保留异常禁用信息)
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValueFutureAwaitable2<T> WithOptions(int options) {
        int suppressed = _options & (int)SuppressedTypes.All;
        return new ValueFutureAwaitable2<T>(_future, _executor, suppressed | options);
    }

    public ValueFutureAwaiter2<T> GetAwaiter() => new(_future, _executor, _options);
}
}