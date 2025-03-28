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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// <see cref="IAgentEvent"/>的处理器
/// 抽取该接口以允许{@link IEventLoopModule}注册监听器
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAgentEventHandler<T> where T : IAgentEvent
{
    /// <summary>
    /// 处理提交到EventLoop的事件
    /// 注意：不可以保留事件的引用
    /// </summary>
    /// <param name="sequence">事件序号，有效性取决于事件循环的实现</param>
    /// <param name="rawEvent">事件</param>
    void OnEvent(long sequence, ref T rawEvent);
}
}