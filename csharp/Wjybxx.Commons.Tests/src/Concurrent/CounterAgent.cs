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

using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class CounterAgent : IEventLoopAgent<AgentEvent>
{
    private readonly Counter _counter = new Counter();
    private long systemTickMillis = ObjectUtil.SystemTickMillis();
    private long lastUpdateTime = ObjectUtil.SystemTickMillis();

    public Counter Counter => _counter;

    public void Inject(IEventLoop eventLoop, long cid) {
    }

    public void Subscribe(int type, IAgentEventHandler<AgentEvent> handler) {
        throw new System.NotImplementedException();
    }

    public bool CheckMainLoop(long threadTime) {
        systemTickMillis = ObjectUtil.SystemTickMillis();
        return systemTickMillis - lastUpdateTime >= 10;
    }

    public void BeforeMainLoop(long threadTime) {
        lastUpdateTime = systemTickMillis;
    }

    public void OnEvent(long sequence, ref AgentEvent evt) {
        _counter.Count(evt.Type, evt.LongVal1);
    }
}