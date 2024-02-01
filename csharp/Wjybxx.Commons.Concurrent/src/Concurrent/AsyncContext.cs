﻿#region LICENSE

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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 异步回调上下文
/// 
/// ps：该结构体用于避免过多的方法扩展。
/// </summary>
public readonly struct AsyncContext
{
    /// <summary>
    /// 取消令牌
    /// </summary>
    public readonly ICancelToken? CancelToken;
    /// <summary>
    /// 回调参数
    /// </summary>
    public readonly object? State;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="state">回调参数</param>
    public AsyncContext(ICancelToken? cancelToken, object? state = null) {
        CancelToken = cancelToken;
        State = state;
    }
}