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

public class AsyncStateMachinePoolTest
{
    [Test]
    public void Test() {
        int expected = 100;
        TaskPoolConfig.AddPoolConfig<AsyncStateMachinePoolTest, int>(TaskPoolType.ValueFutureStateMachineTask, expected);
        int poolSize = TaskPoolConfig.GetPoolSize<AsyncStateMachinePoolTest, int>(TaskPoolType.ValueFutureStateMachineTask);
        Assert.AreEqual(expected, poolSize);
        
        TaskPoolConfig.AddPoolConfig<int>(TaskPoolType.ValuePromise, expected);
        poolSize = TaskPoolConfig.GetPoolSize<int>(TaskPoolType.ValuePromise);
        Assert.AreEqual(expected, poolSize);
        
        TaskPoolConfig.AddPoolConfig(TaskPoolType.Coroutine, 0);
        poolSize = TaskPoolConfig.GetPoolSize<int>(TaskPoolType.Coroutine);
        Assert.AreEqual(0, poolSize);
    }
}