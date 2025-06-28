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
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 异步调度任务测试
/// </summary>
public class AsyncTaskTest
{
    [Test]
    public async Task Test() {
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAsyncTask(AsyncMethod);
        int r = await GlobalEventLoop.Inst.Schedule(in builder);
        Console.WriteLine("r: " + r);
    }

    private static async ValueFuture<int> AsyncMethod(AsyncTaskContext context) {
        int r = 0;
        for (; r < 10; r++) {
            long start = ObjectUtil.SystemTickMillis();
            await context.Sleep(TimeSpan.FromMilliseconds(100));
            long end = ObjectUtil.SystemTickMillis();
            Console.WriteLine("costTime: " + (end - start));
        }
        return r;
    }
}