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

using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Core;

public class FutureTest
{
    private static readonly IEventLoop globalEventLoop = EventLoopBuilder.NewBuilder(new DefaultThreadFactory("consumer")).Build();
    private static readonly IExecutor Executor = new ImmediateExecutor();

    private class ImmediateExecutor : IExecutor
    {
        private readonly SynchronizationContext synchronizationContext;
        private readonly TaskScheduler scheduler;

        public ImmediateExecutor() {
            synchronizationContext = new ExecutorSynchronizationContext(this);
            scheduler = new ExecutorTaskScheduler(this);
        }

        public SynchronizationContext AsSyncContext() {
            return synchronizationContext;
        }

        public TaskScheduler AsScheduler() {
            return scheduler;
        }

        public void Execute(ITask task) {
            task.Run();
        }
    }

    [Test]
    public void AwaiterTest() {
        // Console.WriteLine("count: " + CountAsync().Get());
    }

    private static async IFuture<int> CountAsync() {
        IFutureTask<int> task = PromiseTask.OfFunction(() => 1, null, 0, new Promise<int>(Executor));
        Executor.Execute(task);

        await Executor.SwitchTo();
        
        // 确保回调在目标指定线程 --- 任务已完成的情况下，无法控制回调线程。。。
        // int value = await task.Future.GetAwaiter(globalEventLoop, TaskOption.STAGE_TRY_INLINE);
        Assert.IsTrue(globalEventLoop.InEventLoop(), "globalEventLoop.InEventLoop() == false");

        return await task.Future;
    }
}