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

using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class WatcherMgrTest
{
    [Test]
    public void Test() {
        SimpleWatcherMgr<AgentEvent> watcherMgr = new SimpleWatcherMgr<AgentEvent>();
        Promise<AgentEvent> promise = new Promise<AgentEvent>();
        FutureWatcher watcher = new FutureWatcher(promise);

        watcherMgr.Watch(watcher);
        watcherMgr.OnEvent(new AgentEvent()
        {
            Type = 1
        });
        Assert.IsTrue(promise.IsSucceeded);
    }

    private class FutureWatcher : Watcher<AgentEvent>
    {
        private readonly IPromise<AgentEvent> _future;

        public FutureWatcher(IPromise<AgentEvent> future) {
            _future = future;
        }

        public bool Test(in AgentEvent evt) {
            return evt.Type == 1;
        }

        public void OnEvent(in AgentEvent evt) {
            _future.TrySetResult(evt);
        }
    }
}