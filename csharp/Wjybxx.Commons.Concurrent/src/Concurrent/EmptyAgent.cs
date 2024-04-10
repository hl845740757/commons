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
/// 空代理，用于避免null
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public class EmptyAgent<TEvent> : IEventLoopAgent<TEvent> where TEvent : IAgentEvent
{
    /** 默认实例 */
    public static readonly EmptyAgent<TEvent> INST = new EmptyAgent<TEvent>();

    private EmptyAgent() {
    }

    public void Inject(IEventLoop eventLoop) {
    }

    public void OnStart() {
    }

    public void OnEvent(TEvent evt) {
    }

    public void Update() {
    }

    public void OnShutdown() {
    }
}