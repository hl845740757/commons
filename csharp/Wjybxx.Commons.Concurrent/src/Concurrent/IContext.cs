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

using System.Diagnostics.CodeAnalysis;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 异步任务的上下文
/// 在异步和并发编程中，共享上下文是很必要的，且显式的共享优于隐式的共享。
/// 共享上下文可实现的功能：
/// 1.传递取消信号
/// 2.传递超时信息
/// 3.共享数据(K-V结果)
/// 
/// <h3>上下文扩展</h3>
/// 由于这里的上下文和任务之间是组合关系，因此用户既可以通过实现更具体的上下文类型扩展，也可以仅通过扩展黑板实现。
/// 对于简单的情况：可通过实现更具体的Context类型解决。
/// 对于复杂的情况：建议通过黑板实现。
/// 
/// </summary>
public interface IContext
{
    /// <summary>
    /// 空上下文
    /// </summary>
    public static readonly IContext NONE = Context<object>.OfCancelToken(ICancelToken.NONE);

    /// <summary>
    /// 任务绑定的取消令牌（取消上下文）
    /// 1.每个任务可有独立的取消信号；
    /// 2.运行时不为null；
    /// </summary>
    ICancelToken CancelToken { get; }

#nullable disable

    /// <summary>
    /// 任务绑定的状态
    /// 1.任务之间不共享
    /// 2.运行时可能为null
    ///
    /// ps：该属性是为了迎合C#的编程风格而设计的。
    /// </summary>
    object State { get; }

    /// <summary>
    /// 任务运行时依赖的黑板（主要上下文）
    /// 1.每个任务可有独立的黑板（数据）；
    /// 2.一般而言，黑板需要实现递归向上查找。
    /// 
    /// 这里未直接实现为类似Map的读写接口，是故意的。
    /// 因为提供类似Map的读写接口，会导致创建Context的开销变大，而在许多情况下是不必要的。
    /// 将黑板设定为Object类型，既可以增加灵活性，也可以减少一般情况下的开销。
    /// </summary>
    object Blackboard { get; }

    /// <summary>
    /// 共享属性（配置上下文）
    /// 1.用于支持【数据和行为分离】的Task体系。
    /// 2.共享属性应该是只读的、可共享的，因为它是配置。
    ///
    /// PS: 数据和行为分离是指：Task仅包含行为，其属性是外部传入的；属性可能是单个任务的，也可能是多个任务共享的。
    /// </summary>
    /// <returns></returns>
    object SharedProps { get; }

#nullable enable

    /// <summary>
    /// 去除与任务绑定的属性，保留可多任务共享的属性。
    /// 需要去除的属性：取消令牌，任务绑定的状态。
    /// 注意：不是创建子上下文，而是同级上下文；通常用于下游任务继承上下文。
    /// </summary>
    /// <returns></returns>
    IContext ToSharable();
}