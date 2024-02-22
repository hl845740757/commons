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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 默认的上下文实现
///
/// 默认实现假设了父上下文和当前上下文的黑板类型一致，这个假设并不总是成立，但一般情况下是如此；
/// 如果该实现不满足需求，用户可实现自己的上下文类型。
/// </summary>
public class Context<T> : IContext where T : class
{
#nullable disable

    /// <summary>
    /// 状态参数 -- 状态参数用于支持私有变量，不同任务的State通常不同。
    /// </summary>
    public object State { get; }

    /// <summary>
    /// 取消令牌 -- 如果未指定，则默认赋值为<see cref="ICancelToken.NONE"/>
    /// </summary>
    public ICancelToken CancelToken { get; }

    /// <summary>
    /// 任务黑板 -- 黑板用于支持读写共享变量，不同任务可能指向同一个对象。 
    /// </summary>
    public T Blackboard { get; }

    /// <summary>
    /// 任务黑板 -- 黑板用于支持读写共享变量，不同任务可能指向同一个对象。 
    /// </summary>
    object IContext.Blackboard => Blackboard;

    /// <summary>
    /// 任务之间的共享属性
    /// </summary>
    public object SharedProps { get; }

#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parent">父上下文</param>
    /// <param name="state"></param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="blackboard">黑板</param>
    /// <param name="sharedProps">共享属性</param>
    public Context(Context<T>? parent,
                   object? state, ICancelToken? cancelToken,
                   T? blackboard, object? sharedProps) {
        Parent = parent;
        State = state;
        CancelToken = cancelToken ?? ICancelToken.NONE;
        Blackboard = blackboard;
        SharedProps = sharedProps;
    }

    /// <summary>
    /// 父上下文
    /// </summary>
    public virtual Context<T>? Parent { get; }

    /// <summary>
    /// 根上下文
    ///
    /// 默认的实现不缓存(root)上下文，如果用户需要频繁访问root上下文，可实现自己的上下文类型，
    /// 将root缓存在每一级的context上，以减少查找开销。
    /// </summary>
    public Context<T> Root {
        get {
            Context<T> root = this;
            while (root.Parent != null) {
                root = root.Parent;
            }
            return root;
        }
    }

    /// <summary>
    /// 创建上下文
    /// </summary>
    /// <param name="parent">父上下文</param>
    /// <param name="state"></param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="blackboard">黑板</param>
    /// <param name="sharedProps">共享属性</param>
    /// <returns></returns>
    protected virtual Context<T> NewContext(Context<T>? parent,
                                            object? state, ICancelToken? cancelToken,
                                            T? blackboard, object? sharedProps) {
        return new Context<T>(parent, state, cancelToken, blackboard, sharedProps);
    }

    #region factory

    public static Context<T> OfBlackboard(T blackboard) {
        return new Context<T>(null, null, null, blackboard, null);
    }

    public static Context<T> OfBlackboard(T blackboard, object sharedProps) {
        return new Context<T>(null, null, null, blackboard, sharedProps);
    }

    public static Context<T> OfState(object state) {
        return new Context<T>(null, state, null, null, null);
    }

    public static Context<T> OfState(object state, ICancelToken cancelToken) {
        return new Context<T>(null, state, cancelToken, null, null);
    }

    public static Context<T> OfState(object state, ICancelToken cancelToken, T blackboard, object sharedProps) {
        return new Context<T>(null, state, cancelToken, blackboard, sharedProps);
    }

    public static Context<T> OfCancelToken(ICancelToken cancelToken) {
        if (cancelToken == null) throw new ArgumentNullException(nameof(cancelToken));
        return new Context<T>(null, null, cancelToken, null, null);
    }

    #endregion

    #region child

    public Context<T> ChildWithState(object state) {
        return NewContext(this, state, null, Blackboard, SharedProps);
    }

    public Context<T> ChildWithState(object state, ICancelToken cancelToken) {
        return NewContext(this, state, cancelToken, Blackboard, SharedProps);
    }

    public Context<T> ChildWithBlackboard(T blackboard) {
        return NewContext(this, null, null, blackboard, SharedProps);
    }

    public Context<T> ChildWithBlackboard(T blackboard, object sharedProps) {
        return NewContext(this, null, null, blackboard, sharedProps);
    }

    public Context<T> ChildWith(object state, ICancelToken cancelToken, T blackboard, object sharedProps) {
        return NewContext(this, state, cancelToken, blackboard, sharedProps);
    }

    #endregion

    #region with

    // with不会共享state和取消令牌

    public Context<T> WithState(object state) {
        return NewContext(Parent, state, null, Blackboard, SharedProps);
    }

    public Context<T> WithState(object state, ICancelToken cancelToken) {
        return NewContext(Parent, state, cancelToken, Blackboard, SharedProps);
    }

    public Context<T> WithBlackboard(T blackboard) {
        return NewContext(Parent, null, null, blackboard, SharedProps);
    }

    public Context<T> WithBlackboard(T blackboard, object sharedProps) {
        return NewContext(Parent, null, null, blackboard, sharedProps);
    }

    public Context<T> With(object state, ICancelToken cancelToken, T blackboard, object sharedProps) {
        return NewContext(Parent, state, cancelToken, blackboard, sharedProps);
    }

    #endregion

    public Context<T> ToSharable() {
        if (State == null && CancelToken == ICancelToken.NONE) {
            return this;
        }
        return NewContext(Parent, null, ICancelToken.NONE, Blackboard, SharedProps);
    }

    IContext IContext.ToSharable() => ToSharable();
}