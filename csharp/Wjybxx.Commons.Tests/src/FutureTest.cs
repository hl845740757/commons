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

namespace Commons.Tests;

public class FutureTest
{
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
        Console.WriteLine("count: " + CountAsync().Get());
    }

    private static async IFuture<int> CountAsync() {
        IFutureTask<int> task = new PromiseTask<int>(() => 1, 0, new Promise<int>());
        Executor.Execute(task);
        return await task.Future;
    }
}