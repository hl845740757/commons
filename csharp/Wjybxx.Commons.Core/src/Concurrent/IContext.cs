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


namespace Wjybxx.Commons.Concurrent
{
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
    public static readonly IContext NONE = MiniContext.SHARABLE;

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
    /// 任务绑定的取消令牌（取消上下文）
    /// 1.每个任务可有独立的取消信号；
    /// 2.运行时不为null -- 不要返回null，使用<see cref="ICancelToken.NONE"/>代替。
    /// </summary>
    ICancelToken CancelToken { get; }
}
}