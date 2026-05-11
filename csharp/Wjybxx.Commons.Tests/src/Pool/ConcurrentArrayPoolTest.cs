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
using Wjybxx.Commons.Pool;

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

    // ============= 补充：API 语义 / 复杂场景测试 =============

    /// <summary>
    /// 无参 Acquire 返回 DefCapacity 大小的数组
    /// </summary>
    [Test]
    public void TestAcquireParameterless() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 256,
            MaxCapacity = 4096,
            Clear = false,
        }.Build();

        byte[] arr = pool.Acquire();
        Assert.AreEqual(256, arr.Length);
    }

    /// <summary>
    /// 申请的数组长度向上对齐到 bucket 容量，而非 minimumLength
    /// </summary>
    [Test]
    public void TestAcquiredLengthMatchesBucketCapacity() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 1600,
            ArrayGrowFactor = 2,
            Clear = false,
        }.Build();
        // 容量阶梯：100, 200, 400, 800, 1600

        byte[] a = pool.Acquire(50); // 50 ≥ 100/4=25 → 命中第一桶
        Assert.AreEqual(100, a.Length);

        byte[] b = pool.Acquire(150); // 命中 200
        Assert.AreEqual(200, b.Length);

        byte[] c = pool.Acquire(800); // 精确命中
        Assert.AreEqual(800, c.Length);
    }

    /// <summary>
    /// 申请大于 MaxCapacity 的数组：直接 new，且 Release 时不会被池化
    /// </summary>
    [Test]
    public void TestAcquireOversizeReturnsFreshAndNotPooled() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 1000,
            Clear = false,
        }.Build();

        byte[] huge = pool.Acquire(5000);
        Assert.AreEqual(5000, huge.Length); // 按精确 minimumLength 创建

        pool.Release(huge);
        // 再次以同样长度申请，长度仍是 5000（直接 new），不应等于上一个引用
        byte[] huge2 = pool.Acquire(5000);
        Assert.AreEqual(5000, huge2.Length);
        Assert.AreNotSame(huge, huge2);
    }

    /// <summary>
    /// Release 长度不匹配任何 bucket 的数组：被丢弃，不可复用
    /// </summary>
    [Test]
    public void TestReleaseMismatchedLengthDropped() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 1000,
            ArrayGrowFactor = 2,
            Clear = false,
        }.Build();
        // 容量阶梯：100, 200, 400, 800, 1000

        // 长度 150 不在容量表中
        byte[] odd = new byte[150];
        pool.Release(odd);

        // 申请 150 后应该是新数组（命中桶 200，因为池里没有）
        byte[] fresh = pool.Acquire(150);
        Assert.AreEqual(200, fresh.Length); // 桶 200
        Assert.AreNotSame(odd, fresh);
    }

    /// <summary>
    /// Release/Acquire 复用同一引用
    /// </summary>
    [Test]
    public void TestReleaseAndReuseCycle() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 128,
            MaxCapacity = 1024,
            Clear = false,
        }.Build();

        byte[] a = pool.Acquire(128);
        pool.Release(a);
        byte[] b = pool.Acquire(128);
        Assert.AreSame(a, b);
    }

    /// <summary>
    /// Clear 清空所有桶
    /// </summary>
    [Test]
    public void TestClearEmptiesAllBuckets() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 800,
            ArrayGrowFactor = 2,
            Clear = false,
        }.Build();

        byte[] a = pool.Acquire(100);
        byte[] b = pool.Acquire(200);
        byte[] c = pool.Acquire(400);
        pool.Release(a);
        pool.Release(b);
        pool.Release(c);

        pool.Clear();

        // Clear 之后应得到新数组
        Assert.AreNotSame(a, pool.Acquire(100));
        Assert.AreNotSame(b, pool.Acquire(200));
        Assert.AreNotSame(c, pool.Acquire(400));
    }

    /// <summary>
    /// clear=true 在 Acquire 时清理：池默认不清理时该参数才有效
    /// </summary>
    [Test]
    public void TestAcquireClearOption() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 16,
            MaxCapacity = 64,
            Clear = false, // 默认不清理
        }.Build();

        byte[] dirty = pool.Acquire(16);
        for (int i = 0; i < dirty.Length; i++) dirty[i] = 0xFF;
        pool.Release(dirty); // 默认不清理 → 内容仍是 0xFF

        // 不要求清理：拿到的是上次脏数据
        byte[] reused = pool.Acquire(16);
        Assert.AreSame(dirty, reused);
        Assert.AreEqual(0xFF, reused[0]);

        // 重新归还，下次要求清理
        pool.Release(reused);
        byte[] clean = pool.Acquire(16, clear: true);
        Assert.AreSame(dirty, clean);
        for (int i = 0; i < clean.Length; i++) {
            Assert.AreEqual(0, clean[i]);
        }
    }

    /// <summary>
    /// Release(arr, clear=true) 在归还时清理（即使池默认不清理）
    /// </summary>
    [Test]
    public void TestReleaseClearOption() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 16,
            MaxCapacity = 64,
            Clear = false,
        }.Build();

        byte[] dirty = pool.Acquire(16);
        for (int i = 0; i < dirty.Length; i++) dirty[i] = 0x77;
        pool.Release(dirty, clear: true); // 显式要求清理

        byte[] reused = pool.Acquire(16);
        Assert.AreSame(dirty, reused);
        for (int i = 0; i < reused.Length; i++) {
            Assert.AreEqual(0, reused[i]);
        }
    }

    /// <summary>
    /// LookAhead：目标桶为空时回退到更大桶
    /// </summary>
    [Test]
    public void TestLookAheadFallback() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 800,
            ArrayGrowFactor = 2,
            Clear = false,
            LookAhead = 2,
        }.Build();
        // 容量阶梯：100, 200, 400, 800

        // 只在 400 桶里塞数组
        byte[] big = pool.Acquire(400);
        pool.Release(big);

        // 申请 100 时，目标桶 100 为空，但 lookAhead=2 → 检查 200/400
        byte[] got = pool.Acquire(100);
        Assert.AreSame(big, got); // 应回退到 400 桶
        Assert.AreEqual(400, got.Length);
    }

    /// <summary>
    /// LookAhead=0 时不会回退（即使更大桶有可用数组也忽略）
    /// </summary>
    [Test]
    public void TestLookAheadZeroNoFallback() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100,
            MaxCapacity = 800,
            ArrayGrowFactor = 2,
            Clear = false,
            LookAhead = 0,
        }.Build();

        byte[] big = pool.Acquire(400);
        pool.Release(big);

        byte[] got = pool.Acquire(100);
        Assert.AreEqual(100, got.Length); // 直接 new 100
        Assert.AreNotSame(big, got);
    }

    /// <summary>
    /// Builder.AddBucket：手动指定每个桶的容量与缓存数
    /// </summary>
    [Test]
    public void TestBuilderAddBucket() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            Clear = false,
            LookAhead = 0,
        }
        .AddBucket(64, 4)
        .AddBucket(256, 4)
        .AddBucket(1024, 2)
        .Build();

        // 申请并归还，验证按指定容量对齐
        byte[] a = pool.Acquire(50);
        Assert.AreEqual(64, a.Length);

        byte[] b = pool.Acquire(100);
        Assert.AreEqual(256, b.Length);

        byte[] c = pool.Acquire(900);
        Assert.AreEqual(1024, c.Length);

        pool.Release(a);
        pool.Release(b);
        pool.Release(c);

        Assert.AreSame(a, pool.Acquire(50));
        Assert.AreSame(b, pool.Acquire(100));
        Assert.AreSame(c, pool.Acquire(900));
    }

    /// <summary>
    /// Builder.AddBucket：容量必须严格递增
    /// </summary>
    [Test]
    public void TestBuilderAddBucketInvalidOrder() {
        Assert.Throws<ArgumentException>(() => {
            new ConcurrentArrayPool<byte>.Builder()
                .AddBucket(256, 4)
                .AddBucket(128, 4) // 容量倒退
                .Build();
        });
    }

    /// <summary>
    /// Builder 的非法参数：DefCapacity 必须 >0，MaxCapacity 必须 ≥ DefCapacity
    /// </summary>
    [Test]
    public void TestBuilderInvalidCapacities() {
        Assert.Throws<ArgumentException>(() => new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 0,
            MaxCapacity = 1024,
        }.Build());

        Assert.Throws<ArgumentException>(() => new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 2048,
            MaxCapacity = 1024, // < def
        }.Build());
    }

    /// <summary>
    /// 桶满时再 Release 被静默丢弃：Acquire 不会拿到第三个引用
    /// </summary>
    [Test]
    public void TestBucketFullSilentlyDrops() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            Clear = false,
            LookAhead = 0,
        }.AddBucket(64, 2).Build(); // 桶仅能缓存 2 个

        byte[] a = new byte[64];
        byte[] b = new byte[64];
        byte[] c = new byte[64];

        pool.Release(a);
        pool.Release(b);
        pool.Release(c); // 第三个被丢弃

        // 任意顺序拿出 a / b
        HashSet<byte[]> seen = new();
        seen.Add(pool.Acquire(64));
        seen.Add(pool.Acquire(64));
        Assert.IsTrue(seen.Contains(a));
        Assert.IsTrue(seen.Contains(b));
        Assert.IsFalse(seen.Contains(c));

        // 池已空，再次申请得到新数组
        byte[] fresh = pool.Acquire(64);
        Assert.AreNotSame(a, fresh);
        Assert.AreNotSame(b, fresh);
    }

    /// <summary>
    /// 申请远小于最小桶 1/4 的数组也会走 new（minimumLength=0 等极小值）
    /// </summary>
    [Test]
    public void TestAcquireBelowMinimumBucketReturnsFresh() {
        ConcurrentArrayPool<byte> pool = new ConcurrentArrayPool<byte>.Builder()
        {
            DefCapacity = 100, // 最小桶 100，下界 100/4=25
            MaxCapacity = 1000,
            Clear = false,
        }.Build();

        byte[] tiny = pool.Acquire(0);
        Assert.AreEqual(0, tiny.Length); // new T[0]

        byte[] small = pool.Acquire(10);
        Assert.AreEqual(10, small.Length); // 仍是按 minimumLength 直接 new
    }
}