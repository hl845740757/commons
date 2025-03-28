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

using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Fx;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

public class EventLoopModuleTest
{
    private static IEventLoop eventLoop;
    private static ComponentId<DataModule> dataCid =
        (ComponentId<DataModule>)EventLoopUtils.GLOBAL.ValueOf(typeof(DataModule));
    private static ComponentId<BehaviorModule> behaviorCid =
        (ComponentId<BehaviorModule>)EventLoopUtils.GLOBAL.ValueOf(typeof(BehaviorModule));

    [SetUp]
    public void SetUp() {
        var builder = new DisruptorEventLoopBuilder<AgentEvent>()
        {
            ThreadFactory = new DefaultThreadFactory("Scheduler", true),
            Agent = new Agent(),
            EventSequencer = new RingBufferEventSequencer<AgentEvent>.Builder(AgentEvent.FACTORY)
                .Build()
        };
        //
        builder.AddModule(new DataModule());
        builder.AddModule(new BehaviorModule());

        eventLoop = builder.Build();
        eventLoop.Start().Join();
    }

    [Test]
    public void TestUpdate() {
        Thread.Sleep(1000);
        try {
            // 数据组件为0
            DataModule dataModule = eventLoop.GetComponent(dataCid);
            Assert.IsTrue(dataModule.onReadyInvoked);
            Assert.AreEqual(dataModule.updateCount, 0);
            Assert.AreEqual(dataModule.lastUpdateCount, 0);
            // 行为组件非0
            BehaviorModule behaviorModule = eventLoop.GetComponent(behaviorCid);
            Assert.IsTrue(behaviorModule.onReadyInvoked);
            Assert.AreNotEqual(behaviorModule.updateCount, 0);
            Assert.AreNotEqual(behaviorModule.lastUpdateCount, 0);
            Assert.AreNotEqual(behaviorModule.eventCount, 0);
        }
        finally {
            eventLoop.ShutdownNow();
        }
    }

    private class Agent : IEventLoopAgent<AgentEvent>
    {
        private long lastUpdateTime = ObjectUtil.SystemTickMillis();
        private Dictionary<int, IAgentEventHandler<AgentEvent>> _handlers = new(10);

        public bool CheckMainLoop(long threadTime) {
            return ObjectUtil.SystemTickMillis() - lastUpdateTime >= 10;
        }

        public void BeforeMainLoop(long threadTime) {
            lastUpdateTime = ObjectUtil.SystemTickMillis();
        }

        public void Subscribe(int type, IAgentEventHandler<AgentEvent> handler) {
            _handlers[type] = handler;
        }

        public void OnEvent(long sequence, ref AgentEvent rawEvent) {
            if (_handlers.TryGetValue(rawEvent.Type, out var handler)) {
                handler.OnEvent(sequence, ref rawEvent);
            }
        }
    }

    [ComponentDefine(Kind = ComponentKind.Data)]
    private class DataModule : EventLoopModule
    {
        internal bool onReadyInvoked;
        internal long updateCount;
        internal long lastUpdateCount;

        public override void OnReady() {
            onReadyInvoked = true;
        }

        public override void Update() {
            updateCount++;
        }

        public override void LateUpdate() {
            lastUpdateCount++;
        }
    }

    [ComponentDefine(Kind = ComponentKind.Script)]
    private class BehaviorModule : EventLoopModule, IAgentEventHandler<AgentEvent>
    {
        internal bool onReadyInvoked;
        internal long updateCount;
        internal long lastUpdateCount;
        internal int eventCount;
        DisruptorEventLoop<AgentEvent> eventLoop;

        public override void OnReady() {
            onReadyInvoked = true;
            eventLoop = (Entity as DisruptorEventLoop<AgentEvent>)!;
        }

        public override void Start() {
            eventLoop.Subscribe(1, this);
        }

        public override void Update() {
            updateCount++;
            if (((updateCount) & 7) == 0) {
                GenerateEvent();
            }
        }

        public override void LateUpdate() {
            lastUpdateCount++;
        }

        private void GenerateEvent() {
            long sequence = eventLoop.NextSequence(1);
            if (sequence < 0) return;
            ref AgentEvent evt = ref eventLoop.GetEventRef(sequence);
            evt.Type = 1;
            eventLoop.Publish(sequence);
        }

        public void OnEvent(long sequence, ref AgentEvent rawEvent) {
            eventCount++;
        }
    }
}