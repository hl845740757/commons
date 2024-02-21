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
    private static readonly MiniContext Sharable = new MiniContext(ICancelToken.NONE);

    /// <summary>
    /// 取消令牌 -- 如果未指定，则默认赋值为<see cref="ICancelToken.NONE"/>
    /// </summary>
    public ICancelToken CancelToken { get; }

    private MiniContext(ICancelToken? cancelToken) {
        CancelToken = cancelToken ?? ICancelToken.NONE;
    }

    public static MiniContext Create(ICancelToken? cancelToken) {
        if (cancelToken == null || cancelToken == ICancelToken.NONE) {
            return Sharable;
        }
        return new MiniContext(cancelToken);
    }

    public object? State => null;
    public object? Blackboard => null;
    public object? SharedProps => null;

    public IContext ToSharable() {
        return Sharable;
    }
}