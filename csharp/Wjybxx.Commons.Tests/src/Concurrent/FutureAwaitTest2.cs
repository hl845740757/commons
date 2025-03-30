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
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 我们调整了<see cref="ValueFutureAwaiter"/>的实现，会查询<see cref="EventLoopUtil.Current"/>，
/// 我们现在在多线程环境测试一下。
/// </summary>
public class FutureAwaitTest2
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
    public void TestAwait() {
        for (int i = 0; i < 1000; i++) {
            consumer.Select().Execute(() => CountAsync().Forget());
        }
        Thread.Sleep(1000);
        consumer.ShutdownNow();
    }

    private static async IFuture<int> CountAsync() {
        // 现在在其中一个线程中
        int threadId = Thread.CurrentThread.ManagedThreadId;
        // 再提交一个任务到其它线程 -- 
        await consumer.Select().SubmitAction(task_success);
        // 测试await成功返回到之前的线程
        Assert.AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);
        return threadId;
    }
}