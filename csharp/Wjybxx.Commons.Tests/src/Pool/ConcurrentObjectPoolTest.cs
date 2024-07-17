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

using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.IO;

namespace Commons.Tests.Pool;

public class ConcurrentObjectPoolTest
{
    [Repeat(5)]
    [Test]
    public void TestConcurrentPool() {
        int treadCount = 8;
        ConcurrentObjectPool.SharedStringBuilderPool.Clear(); // 消除其它测试影响
        // ConcurrentObjectPool.SharedStringBuilderPool.Fill(treadCount);
        // int availableCount = ConcurrentObjectPool.SharedStringBuilderPool.AvailableCount();
        // Assert.True(availableCount == treadCount);

        List<Thread> threads = new List<Thread>(treadCount);
        for (int i = 0; i < treadCount; i++) {
            threads.Add(new Thread(TestImpl));
        }
        foreach (Thread thread in threads) {
            thread.Start();
        }
        // 等待退出
        foreach (Thread thread in threads) {
            thread.Join();
        }
        // producerIndex 799999
        // consumerIndex 799991
        // 压入了80次，从池中取出799992次，因为有8个是new出来的
        int availableCount = ConcurrentObjectPool.SharedStringBuilderPool.AvailableCount();
        Assert.True(availableCount == treadCount);
    }

    private static void TestImpl() {
        ConcurrentObjectPool<StringBuilder> objectPool = ConcurrentObjectPool.SharedStringBuilderPool;
        for (int j = 0; j < 100000; j++) {
            var obj = objectPool.Acquire();
            objectPool.Release(obj);
        }
    }
}