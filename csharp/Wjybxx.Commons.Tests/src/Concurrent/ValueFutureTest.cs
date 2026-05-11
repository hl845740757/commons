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
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using TaskStatus = Wjybxx.Commons.Concurrent.TaskStatus;

namespace Commons.Tests.Concurrent;

public class ValueFutureTest
{
    [Test]
    public void TestFromResult() {
        ValueFuture<int> vf = ValueFuture<int>.FromResult(42);
        Assert.IsTrue(vf.IsCompleted);
        Assert.AreEqual(TaskStatus.Success, vf.Status);
        Assert.AreEqual(42, vf.AsFuture().Get());
    }

    [Test]
    public void TestFromException() {
        InvalidOperationException ex = new("oops");
        ValueFuture<int> vf = ValueFuture<int>.FromException(ex);
        Assert.IsTrue(vf.IsCompleted);
        Assert.AreEqual(TaskStatus.Failed, vf.Status);
    }

    [Test]
    public void TestFromCancelled() {
        ValueFuture<int> vf = ValueFuture<int>.FromCancelled();
        Assert.IsTrue(vf.IsCompleted);
        Assert.AreEqual(TaskStatus.Cancelled, vf.Status);
    }

    [Test]
    public void TestNonGenericFromResult() {
        ValueFuture vf = ValueFuture.FromResult("hello");
        Assert.IsTrue(vf.IsCompleted);
        Assert.AreEqual(TaskStatus.Success, vf.Status);
    }

    [Test]
    public void TestWrapsPromise() {
        Promise<int> p = new();
        ValueFuture<int> vf = new(p);
        Assert.IsFalse(vf.IsCompleted);
        Assert.AreEqual(TaskStatus.Pending, vf.Status);

        p.TrySetResult(7);
        Assert.IsTrue(vf.IsCompleted);
        Assert.AreEqual(7, vf.AsFuture().Get());
    }

    [Test]
    public async Task TestAwaitCompletedResult() {
        ValueFuture<int> vf = ValueFuture<int>.FromResult(99);
        int r = await vf;
        Assert.AreEqual(99, r);
    }

    [Test]
    public async Task TestAwaitPromiseBacked() {
        Promise<int> p = new();
        ValueFuture<int> vf = new(p);
        // 异步设置
        _ = Task.Run(() => {
            Task.Delay(10).Wait();
            p.TrySetResult(123);
        });
        int r = await vf;
        Assert.AreEqual(123, r);
    }

    [Test]
    public void TestPreserveAllowsMultipleAwait() {
        Promise<int> p = Promise<int>.FromResult(5);
        ValueFuture<int> original = new(p);
        ValueFuture<int> preserved = original.Preserve();
        Assert.IsTrue(preserved.IsCompleted);
        Assert.AreEqual(5, preserved.AsFuture().Get());
    }

    [Test]
    public void TestWithTaskId() {
        ValueFuture<int> vf = ValueFuture<int>.FromResult(1).WithTaskId(42);
        Assert.AreEqual(42, vf.TaskId);
    }

    [Test]
    public void TestBoxAndUnbox() {
        ValueFuture<int> vf = ValueFuture<int>.FromResult(7);
        ValueFuture boxed = vf.Box();
        Assert.AreEqual(TaskStatus.Success, boxed.Status);
        ValueFuture<int> unboxed = boxed.Unbox<int>();
        Assert.AreEqual(7, unboxed.AsFuture().Get());
    }
}
