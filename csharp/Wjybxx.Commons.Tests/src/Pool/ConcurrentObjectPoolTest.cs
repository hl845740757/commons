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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;

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

    // ============= 补充：API 语义 / 复杂场景测试 =============

    private sealed class Box
    {
        public int CleanCount;
        public int Payload;
    }

    /// <summary>
    /// 池为空时 Acquire 走工厂；Release 后再 Acquire 复用同一实例
    /// </summary>
    [Test]
    public void TestAcquireReleaseReuse() {
        int created = 0;
        ConcurrentObjectPool<Box> pool = new(() => {
            Interlocked.Increment(ref created);
            return new Box();
        }, b => b.CleanCount++, poolSize: 4);

        Assert.AreEqual(0, pool.AvailableCount());

        Box b1 = pool.Acquire();
        Assert.IsNotNull(b1);
        Assert.AreEqual(1, created);
        Assert.AreEqual(0, b1.CleanCount); // 新建对象未清理

        pool.Release(b1);
        Assert.AreEqual(1, b1.CleanCount); // 释放时执行清理
        Assert.AreEqual(1, pool.AvailableCount());

        Box b2 = pool.Acquire();
        Assert.AreSame(b1, b2); // 复用同一实例
        Assert.AreEqual(1, created);
        Assert.AreEqual(0, pool.AvailableCount());
    }

    /// <summary>
    /// filter 拒绝时调用 destroyer，不入池
    /// </summary>
    [Test]
    public void TestFilterRejectsThenDestroyerCalled() {
        int destroyed = 0;
        ConcurrentObjectPool<Box> pool = new(
            () => new Box(),
            _ => {},
            poolSize: 8,
            filter: b => b.Payload < 10, // 大于等于10被拒
            destroyer: _ => Interlocked.Increment(ref destroyed));

        Box ok = new() { Payload = 1 };
        Box rejected = new() { Payload = 100 };

        pool.Release(ok);
        Assert.AreEqual(1, pool.AvailableCount());
        Assert.AreEqual(0, destroyed);

        pool.Release(rejected);
        Assert.AreEqual(1, pool.AvailableCount()); // 仍是 1
        Assert.AreEqual(1, destroyed);
    }

    /// <summary>
    /// 池满时 Offer 失败 → destroyer 被调用
    /// </summary>
    [Test]
    public void TestPoolFullDestroyerInvoked() {
        int destroyed = 0;
        ConcurrentObjectPool<Box> pool = new(
            () => new Box(),
            _ => {},
            poolSize: 2,
            destroyer: _ => Interlocked.Increment(ref destroyed));

        pool.Release(new Box());
        pool.Release(new Box());
        Assert.AreEqual(2, pool.AvailableCount());
        Assert.AreEqual(0, destroyed);

        pool.Release(new Box()); // 第三个无法入池
        Assert.AreEqual(2, pool.AvailableCount());
        Assert.AreEqual(1, destroyed);
    }

    /// <summary>
    /// poolSize=0：永不缓存，每次 Acquire 都走工厂；Release 直接调用 destroyer
    /// </summary>
    [Test]
    public void TestZeroPoolSizeNeverCaches() {
        int created = 0;
        int destroyed = 0;
        ConcurrentObjectPool<Box> pool = new(
            () => {
                Interlocked.Increment(ref created);
                return new Box();
            },
            _ => {},
            poolSize: 0,
            destroyer: _ => Interlocked.Increment(ref destroyed));

        Assert.AreEqual(0, pool.PoolSize);

        Box b = pool.Acquire();
        pool.Release(b);
        Box b2 = pool.Acquire();
        pool.Release(b2);

        Assert.AreEqual(2, created);
        Assert.AreEqual(2, destroyed);
        Assert.AreEqual(0, pool.AvailableCount());
        Assert.AreNotSame(b, b2);
    }

    /// <summary>
    /// Clear 清空所有缓存对象，且对每个对象调用 destroyer
    /// </summary>
    [Test]
    public void TestClearInvokesDestroyer() {
        int destroyed = 0;
        ConcurrentObjectPool<Box> pool = new(
            () => new Box(),
            _ => {},
            poolSize: 8,
            destroyer: _ => Interlocked.Increment(ref destroyed));

        for (int i = 0; i < 5; i++) {
            pool.Release(new Box());
        }
        Assert.AreEqual(5, pool.AvailableCount());

        pool.Clear();
        Assert.AreEqual(0, pool.AvailableCount());
        Assert.AreEqual(5, destroyed);

        // Clear 后仍可继续使用
        pool.Release(new Box());
        Assert.AreEqual(1, pool.AvailableCount());
    }

    /// <summary>
    /// PoolSize 反映底层 bucket 大小
    /// </summary>
    [Test]
    public void TestPoolSizeProperty() {
        ConcurrentObjectPool<Box> pool = new(() => new Box(), _ => {}, poolSize: 16);
        Assert.AreEqual(16, pool.PoolSize);
    }

    /// <summary>
    /// SharedStringBuilderPool：Acquire 后 StringBuilder 已被清空（cleaner 生效）
    /// </summary>
    [Test]
    public void TestSharedStringBuilderPoolCleansOnRelease() {
        ConcurrentObjectPool<StringBuilder> pool = ConcurrentObjectPool.SharedStringBuilderPool;
        pool.Clear();

        StringBuilder sb = pool.Acquire();
        sb.Append("dirty content");
        Assert.AreEqual(13, sb.Length);

        pool.Release(sb); // cleaner = builder.Clear()
        Assert.AreEqual(0, sb.Length);

        StringBuilder sb2 = pool.Acquire();
        // 池中只有一个对象，必然复用
        Assert.AreSame(sb, sb2);
        Assert.AreEqual(0, sb2.Length);

        pool.Release(sb2);
    }

    /// <summary>
    /// 并发场景下：同一时刻不会有两个线程持有同一对象
    /// （池"未鉴定归属"，不能阻止外部归还，但内部 bucket 应保证 Poll 后元素不会再次被 Poll 出来）
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestConcurrentNoDoubleAcquire() {
        const int threadCount = 8;
        const int iterations = 50000;

        // 预先填充池，使大部分 Acquire 都来自池
        ConcurrentObjectPool<Box> pool = new(() => new Box(), _ => {}, poolSize: threadCount * 2);
        for (int i = 0; i < threadCount * 2; i++) {
            pool.Release(new Box());
        }

        // 用一个并发字典记录"当前被持有"的对象引用
        ConcurrentDictionary<Box, byte> inUse = new();
        int violations = 0;

        Thread[] threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++) {
            threads[i] = new Thread(() => {
                for (int j = 0; j < iterations; j++) {
                    Box b = pool.Acquire();
                    if (!inUse.TryAdd(b, 0)) {
                        Interlocked.Increment(ref violations);
                    }
                    // 极短的占用窗口
                    inUse.TryRemove(b, out _);
                    pool.Release(b);
                }
            });
        }
        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();

        Assert.AreEqual(0, violations, "同一对象不能被两个线程同时持有");
        Assert.AreEqual(threadCount * 2, pool.AvailableCount());
    }

    /// <summary>
    /// AvailableCount 在 Release 后递增、Acquire 后递减；填充满后再 Release 不会超过 PoolSize
    /// </summary>
    [Test]
    public void TestAvailableCountTracking() {
        ConcurrentObjectPool<Box> pool = new(() => new Box(), _ => {}, poolSize: 3);
        Assert.AreEqual(0, pool.AvailableCount());

        pool.Release(new Box());
        Assert.AreEqual(1, pool.AvailableCount());
        pool.Release(new Box());
        pool.Release(new Box());
        Assert.AreEqual(3, pool.AvailableCount());

        // 池已满
        pool.Release(new Box());
        Assert.AreEqual(3, pool.AvailableCount());

        pool.Acquire();
        Assert.AreEqual(2, pool.AvailableCount());
    }
}