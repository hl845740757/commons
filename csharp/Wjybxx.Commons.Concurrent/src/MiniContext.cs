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

using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 只包含取消令牌的context
/// </summary>
public sealed class MiniContext : IContext
{
    public static readonly MiniContext SHARABLE = new MiniContext(null);

#nullable disable
    /// <summary>
    /// 状态参数 -- 状态参数用于支持私有变量，不同任务的State通常不同。
    /// </summary>
    public object State { get; }

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancelToken { get; }
#nullable restore

    private MiniContext(object? state, CancellationToken cancelToken = default) {
        State = state;
        CancelToken = cancelToken;
    }

    public static MiniContext OfState(object? state) {
        return state == null ? SHARABLE : new MiniContext(state);
    }

    public static MiniContext OfState(object? state, CancellationToken cancelToken) {
        return new MiniContext(state, cancelToken);
    }
}
}