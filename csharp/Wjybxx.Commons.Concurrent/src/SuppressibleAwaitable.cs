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
public readonly struct SuppressibleAwaitable
{
    private readonly ValueFuture _future;
    private readonly IExecutor _executor;
    private readonly int _options;
    private readonly bool _requireResult;

    /// <param name="future">future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    /// <param name="requireResult">是否需要获取最终结果</param>
    public SuppressibleAwaitable(ValueFuture future, IExecutor executor, int options, bool requireResult) {
        _future = future;
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
        _requireResult = requireResult;
    }

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public SuppressibleAwaitable AddOptions(int options) {
        return new SuppressibleAwaitable(_future, _executor, _options | options, _requireResult);
    }

    /// <summary>
    /// 替换调度选项(保留异常禁用信息)
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public SuppressibleAwaitable WithOptions(int options) {
        int suppressed = _options & (int)SuppressedTypes.All;
        return new SuppressibleAwaitable(_future, _executor, suppressed | options, _requireResult);
    }

    /// <summary>
    /// 设置是否获取最终的结果
    /// </summary>
    /// <param name="requireResult"></param>
    /// <returns></returns>
    public SuppressibleAwaitable RequireResult(bool requireResult = true) {
        return new SuppressibleAwaitable(_future, _executor, _options, requireResult);
    }

    public SuppressibleAwaiter GetAwaiter() => new(_future, _executor, _options, _requireResult);
}

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct SuppressibleAwaitable<T>
{
    private readonly ValueFuture<T> _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    public SuppressibleAwaitable(ValueFuture<T> future, IExecutor executor, int options) {
        _future = future;
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    /// <summary>
    /// 增加调度选项
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public SuppressibleAwaitable<T> AddOptions(int options) {
        return new SuppressibleAwaitable<T>(_future, _executor, _options | options);
    }

    /// <summary>
    /// 替换调度选项(保留异常禁用信息)
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public SuppressibleAwaitable<T> WithOptions(int options) {
        int suppressed = _options & (int)SuppressedTypes.All;
        return new SuppressibleAwaitable<T>(_future, _executor, suppressed | options);
    }

    public SuppressibleAwaiter<T> GetAwaiter() => new(_future, _executor, _options);
}
}