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

using System;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using TaskStatus = Wjybxx.Commons.Concurrent.TaskStatus;

namespace Commons.Tests.Concurrent;

public class PromiseTest
{
    [Test]
    public void TestInitialState() {
        Promise<int> p = new();
        Assert.IsTrue(p.IsPending);
        Assert.IsFalse(p.IsCompleted);
        Assert.AreEqual(TaskStatus.Pending, p.Status);
    }

    [Test]
    public void TestSetResult() {
        Promise<int> p = new();
        Assert.IsTrue(p.TrySetResult(42));
        Assert.IsTrue(p.IsCompleted);
        Assert.IsTrue(p.IsSucceeded);
        Assert.AreEqual(42, p.ResultNow());
        Assert.AreEqual(TaskStatus.Success, p.Status);

        // 重复设置应失败
        Assert.IsFalse(p.TrySetResult(99));
        Assert.AreEqual(42, p.ResultNow());
    }

    [Test]
    public void TestSetException() {
        Promise<int> p = new();
        InvalidOperationException ex = new("boom");
        Assert.IsTrue(p.TrySetException(ex));
        Assert.IsTrue(p.IsFailed);
        Assert.IsTrue(p.IsFailedOrCancelled);
        Assert.AreSame(ex, p.ExceptionNow());

        Assert.IsFalse(p.TrySetException(new Exception("again")));
    }

    [Test]
    public void TestSetCancelled() {
        Promise<int> p = new();
        Assert.IsTrue(p.TrySetCancelled());
        Assert.IsTrue(p.IsCancelled);
        Assert.IsTrue(p.IsFailedOrCancelled);
        Assert.AreEqual(TaskStatus.Cancelled, p.Status);
    }

    [Test]
    public void TestSetResultThrowsIfAlreadyDone() {
        Promise<int> p = Promise<int>.FromResult(1);
        Assert.Throws<InvalidOperationException>(() => p.SetResult(2));
        Assert.Throws<InvalidOperationException>(() => p.SetException(new Exception("x")));
    }

    [Test]
    public void TestFromResultFactory() {
        Promise<string> p = Promise<string>.FromResult("hello");
        Assert.IsTrue(p.IsSucceeded);
        Assert.AreEqual("hello", p.ResultNow());
    }

    [Test]
    public void TestFromExceptionFactory() {
        InvalidOperationException ex = new("err");
        Promise<int> p = Promise<int>.FromException(ex);
        Assert.IsTrue(p.IsFailed);
        Assert.AreSame(ex, p.ExceptionNow());
    }

    [Test]
    public void TestFromCancelledFactory() {
        Promise<int> p = Promise<int>.FromCancelled();
        Assert.IsTrue(p.IsCancelled);
    }

    [Test]
    public void TestTrySetComputing() {
        Promise<int> p = new();
        Assert.IsTrue(p.TrySetComputing());
        Assert.IsTrue(p.IsComputing);
        // Computing 状态不允许再次进入 Computing
        Assert.IsFalse(p.TrySetComputing());

        Assert.IsTrue(p.TrySetResult(1));
        Assert.IsTrue(p.IsSucceeded);
    }

    [Test]
    public void TestExceptionNowOnSucceededThrows() {
        Promise<int> p = Promise<int>.FromResult(1);
        Assert.Throws<InvalidOperationException>(() => p.ExceptionNow());
    }

    [Test]
    public void TestResultNowOnFailedThrows() {
        Promise<int> p = Promise<int>.FromException(new Exception("err"));
        Assert.Throws<InvalidOperationException>(() => p.ResultNow());
    }

    [Test]
    public void TestGetWaitsForCompletion() {
        Promise<int> p = new();
        Thread t = new(() => {
            Thread.Sleep(20);
            p.TrySetResult(123);
        });
        t.Start();
        int r = p.Get();
        Assert.AreEqual(123, r);
        t.Join();
    }

    [Test]
    public void TestGetWithTimeout() {
        Promise<int> p = new();
        Assert.Throws<TimeoutException>(() => p.Get(TimeSpan.FromMilliseconds(20)));
    }

    /// <summary>
    /// 多线程下的状态机压力测试：仅一个线程能成功设置结果，其余必须返回 false
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestConcurrentSetResultStress() {
        const int rounds = 200;
        const int threads = 8;
        for (int round = 0; round < rounds; round++) {
            Promise<int> p = new();
            int successCount = 0;
            int failCount = 0;
            CountdownEvent ready = new(threads);
            ManualResetEventSlim go = new(false);
            Thread[] ts = new Thread[threads];
            for (int i = 0; i < threads; i++) {
                int payload = i;
                ts[i] = new Thread(() => {
                    ready.Signal();
                    go.Wait();
                    if (p.TrySetResult(payload)) {
                        Interlocked.Increment(ref successCount);
                    } else {
                        Interlocked.Increment(ref failCount);
                    }
                });
                ts[i].Start();
            }
            ready.Wait();
            go.Set();
            foreach (Thread t in ts) t.Join();

            Assert.AreEqual(1, successCount, $"round {round}: more than one set");
            Assert.AreEqual(threads - 1, failCount);
            Assert.IsTrue(p.IsSucceeded);
        }
    }
}
