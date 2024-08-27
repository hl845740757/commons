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
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 
/// </summary>
public class EventLoopGroupTest
{

    [Test]
    public void GroupTest() {
        IEventLoopGroup eventLoopGroup = new EventLoopGroupBuilder()
        {
            NumChildren = 4,
            EventLoopFactory = new EventLoopFactory(new DefaultThreadFactory("Child"))
        }.Build();

        foreach (IEventLoop eventLoop in eventLoopGroup) {
            eventLoop.Start();
            eventLoop.ScheduleAction(() => { }, TimeSpan.FromSeconds(1));
        }
        
        Thread.Sleep(1000);
        eventLoopGroup.Shutdown();

        eventLoopGroup.TerminationFuture.Join();
    }
}