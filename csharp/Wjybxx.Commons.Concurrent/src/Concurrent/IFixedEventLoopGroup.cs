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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 固定数量 EventLoop 的事件循环线程组
/// 它提供了相同key选择相同 EventLoop 的方法。
/// </summary>
public interface IFixedEventLoopGroup : IEventLoopGroup
{
    /// <summary>
    /// 选择一个<see cref="IEventLoop"/>用于接下来的任务调度。
    ///
    /// 实现约定：相同的key返回相同的对象。
    /// </summary>
    /// <param name="key">计算索引的键</param>
    /// <returns></returns>
    IEventLoop Select(int key);

    /// <summary>
    /// EventLoop的数量
    /// </summary>
    /// <returns></returns>
    int ChildCount();
}