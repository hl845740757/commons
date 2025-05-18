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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于封装回调线程
/// </summary>
public readonly struct CancelTokenAwaitable
{
    private readonly ICancelToken _cts;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancelToken">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    public CancelTokenAwaitable(ICancelToken cancelToken, IExecutor? executor, int options) {
        _cts = cancelToken ?? throw new ArgumentNullException(nameof(cancelToken));
        _executor = executor;
        _options = options;
    }

    public CancelTokenAwaiter GetAwaiter() => new CancelTokenAwaiter(_cts, _executor, _options);
}
}