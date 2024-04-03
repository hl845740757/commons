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

namespace Commons.Tests.Concurrent;

public class FutureTest
{
    private static readonly IEventLoop globalEventLoop = EventLoopBuilder.NewBuilder(new DefaultThreadFactory("consumer", true)).Build();
    private static readonly IExecutor Executor = new ImmediateExecutor();

    [Test]
    public void TestFutureAwaitable() {
        int v = CountAsync().Get();
        Console.WriteLine("TestFutureAwaitable: " + v);
    }

    private static async IFuture<int> CountAsync() {
        IFutureTask<int> task = PromiseTask.OfFunction(() => 1, null, 0, new Promise<int>(Executor));
        Executor.Execute(task);

        IPromise<int> future = task.Future;
        Assert.IsFalse(globalEventLoop.InEventLoop(), "0. before globalEventLoop.InEventLoop() == true");

        await future.GetAwaitable(globalEventLoop);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "1. globalEventLoop.InEventLoop() == false");

        await future.GetAwaitable(globalEventLoop, TaskOption.STAGE_TRY_INLINE);
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

        await future.GetAwaitable(globalEventLoop, TaskOption.STAGE_TRY_INLINE);
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
}