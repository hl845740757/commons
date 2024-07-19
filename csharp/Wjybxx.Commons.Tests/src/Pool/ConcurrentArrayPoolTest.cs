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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.IO;

namespace Commons.Tests.Pool;

public class ConcurrentArrayPoolTest
{
    [Repeat(5)]
    [Test]
    public void TestSpsc() {
        int minLen = 100;
        int maxLen = 1500;

        ConcurrentArrayPool<byte> arrayPool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = minLen,
            MaxCapacity = maxLen,
            BucketGrowFactor = 0.75,
            Clear = false,
        }.Build();

        TestImpl(arrayPool);
    }

    [Repeat(5)]
    [Test]
    public void TestMpmc() {
        int minLen = 100;
        int maxLen = 1500;

        ConcurrentArrayPool<byte> arrayPool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = minLen,
            MaxCapacity = maxLen,
            Clear = false,
        }.Build();

        int treadCount = 8;
        List<Thread> threads = new List<Thread>(treadCount);
        for (int i = 0; i < treadCount; i++) {
            threads.Add(new Thread(() => TestImpl(arrayPool)));
        }
        foreach (Thread thread in threads) {
            thread.Start();
        }
        foreach (Thread thread in threads) {
            thread.Join();
        }
    }

    private static void TestImpl(ConcurrentArrayPool<byte> arrayPool) {
        Random random = Random.Shared;
        for (int j = 0; j < 100000; j++) {
            int minimumLength = random.Next(0, 2048);
            byte[] bytes = arrayPool.Acquire(minimumLength);
            Assert.True(bytes.Length >= minimumLength);
            arrayPool.Release(bytes);
        }
    }
}