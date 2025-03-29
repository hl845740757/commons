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

using System;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

public class FutureCombinerTest
{
    private static IEventLoopGroup consumer;
    private static readonly Action task_success = () => { };
    private static readonly Action task_failure = () => { };
    
    [SetUp]
    public void SetUp() {
        consumer = new EventLoopGroupBuilder()
        {
            NumChildren = 4,
            EventLoopFactory = new EventLoopFactory(new DefaultThreadFactory("Child"), 2048)
        }.Build();
    }
    
    [Test]
    public void timedWait() {
        Random random = Random.Shared;
        FutureCombiner combiner = ExecutorUtil.NewCombiner();
         int taskCount = 20000;
        int succeedCount = 0;
        for (int i = 0; i < taskCount; i++) {
            long delay = random.NextInt64(0, 50);
            IFuture future;
            if (random.Next(2) == 0) {
                succeedCount++;
                future = consumer.ScheduleAction(task_success, TimeSpan.FromMilliseconds(delay)).AsFuture();;
            } else {
                future = consumer.ScheduleAction(task_failure, TimeSpan.FromMilliseconds(delay)).AsFuture();
            }
            combiner.Add(future);
        }
        Assert.IsNull(combiner.SelectN(succeedCount, false).Join());

        consumer.Shutdown();
        consumer.TerminationFuture.Join();
    }

}