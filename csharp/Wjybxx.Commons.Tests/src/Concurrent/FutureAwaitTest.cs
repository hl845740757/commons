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

public class FutureAwaitTest
{
    private static readonly IEventLoop globalEventLoop = new DisruptorEventLoopBuilder<AgentEvent>()
    {
        ThreadFactory = new DefaultThreadFactory("Scheduler", true),
        EventSequencer = new RingBufferEventSequencer<AgentEvent>.Builder(AgentEvent.FACTORY)
            .Build()
    }.Build();
    private static readonly IExecutor executor = ImmediateExecutor.Inst;

    [Test]
    public void TestFutureAwaitable() {
        int v = CountAsync().Get();
        Console.WriteLine("TestFutureAwaitable: " + v);
    }

    private static async IFuture<int> CountAsync() {
        IFuture<int> future = ExecutorUtil.SubmitFunc(executor, () => 1).AsFuture();
        Assert.IsFalse(globalEventLoop.InEventLoop(), "0. before globalEventLoop.InEventLoop() == true");

        await future.GetAwaitable(globalEventLoop);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "1. globalEventLoop.InEventLoop() == false");

        await future.GetAwaitable(globalEventLoop, TaskOptions.STAGE_TRY_INLINE);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "2. globalEventLoop.InEventLoop() == false");

        return await future;
    }

    [Test]
    public void TestTaskAwaitable() {
        Console.WriteLine("TestTaskAwaitable: " + CountAsync2().Result);
    }

    private static async Task<int> CountAsync2() {
        Task<int> future = Task.Run(() => 1, CancellationToken.None);
        future.Wait();
        Assert.IsFalse(globalEventLoop.InEventLoop(), "0. before globalEventLoop.InEventLoop() == true");

        await future.GetAwaitable(globalEventLoop);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "1. globalEventLoop.InEventLoop() == false");

        await future.GetAwaitable(globalEventLoop, TaskOptions.STAGE_TRY_INLINE);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "2. globalEventLoop.InEventLoop() == false");

        return await future;
    }

    [Test]
    public void TestExecutorAwait() {
        AwaitExecutor();
    }

    private async void AwaitExecutor() {
        Assert.IsFalse(globalEventLoop.InEventLoop(), "before globalEventLoop.InEventLoop()");
        await globalEventLoop;
        Assert.IsTrue(globalEventLoop.InEventLoop(), "after globalEventLoop.InEventLoop()");
    }
    
    [Test]
    public async Task TestSuppressCancellation() {
        {
            ValueFuture future = globalEventLoop.ScheduleAction(() => throw new OperationCanceledException(),
                TimeSpan.FromSeconds(1));
            TaskResult result = await future.GetAwaitable(globalEventLoop, SuppressedTypes.Cancellation)
                .WithOptions(TaskOptions.STAGE_TRY_INLINE);
            Assert.IsTrue(result.IsCancelled);
        }
        {
            ValueFuture<string> future2 = globalEventLoop.ScheduleFunc<string>(() => throw new OperationCanceledException(),
                TimeSpan.FromSeconds(1));
            TaskResult<string> result2 = await future2.GetAwaitable(globalEventLoop, SuppressedTypes.Cancellation)
                .WithOptions(TaskOptions.STAGE_TRY_INLINE);
            Assert.IsTrue(result2.IsCancelled);
        }
    }
    
    [Test]
    public async Task TestSuppressError() {
        {
            ValueFuture future = globalEventLoop.ScheduleAction(() => throw new Exception(),
                TimeSpan.FromSeconds(1));
            TaskResult result = await future.GetAwaitable(globalEventLoop, SuppressedTypes.Error)
                .WithOptions(TaskOptions.STAGE_TRY_INLINE);
            Assert.IsTrue(result.IsFailed);
        }
        {
            ValueFuture<string> future2 = globalEventLoop.ScheduleFunc<string>(() => throw new Exception(),
                TimeSpan.FromSeconds(1));
            TaskResult<string> result2 = await future2.GetAwaitable(globalEventLoop, SuppressedTypes.Error)
                .WithOptions(TaskOptions.STAGE_TRY_INLINE);
            Assert.IsTrue(result2.IsFailed);
        }
    }
}