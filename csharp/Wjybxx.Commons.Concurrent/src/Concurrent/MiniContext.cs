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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 只包含取消令牌的context，该类实例通常不暴露给用户的Action
/// </summary>
public sealed class MiniContext : IContext
{
    public static readonly MiniContext SHARABLE = new MiniContext(null, ICancelToken.NONE);

#nullable disable
    /// <summary>
    /// 状态参数 -- 状态参数用于支持私有变量，不同任务的State通常不同。
    /// </summary>
    public object State { get; }

    /// <summary>
    /// 取消令牌 -- 如果未指定，则默认赋值为<see cref="ICancelToken.NONE"/>
    /// </summary>
    public ICancelToken CancelToken { get; }
#nullable enable

    private MiniContext(object? state, ICancelToken? cancelToken) {
        State = state;
        CancelToken = cancelToken ?? ICancelToken.NONE;
    }

    public static MiniContext OfState(object? state) {
        if (state == null) return SHARABLE;
        return new MiniContext(state, null);
    }

    public static MiniContext OfState(object? state, ICancelToken cancelToken) {
        return new MiniContext(state, cancelToken);
    }

    public static MiniContext OfCancelToken(ICancelToken? cancelToken) {
        if (cancelToken == ICancelToken.NONE) return SHARABLE;
        return new MiniContext(null, cancelToken);
    }

    public object? Blackboard => null;
    public object? SharedProps => null;

    public IContext ToSharable() {
        return SHARABLE;
    }
}