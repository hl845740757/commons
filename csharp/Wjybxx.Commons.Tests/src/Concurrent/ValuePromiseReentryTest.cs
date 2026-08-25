#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using TaskStatus = Wjybxx.Commons.Concurrent.TaskStatus;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 测试ValuePromise的一次性消费语义（reentryId守卫）
/// 这些用例针对的是对象池复用带来的高风险逻辑：
/// 1.消费后rid失效，防止用户拿到复用后对象的数据；
/// 2.rid失效必须与"是否启用对象池"解耦，否则行为随配置变化。
/// </summary>
public class ValuePromiseReentryTest
{
    /// <summary>
    /// 消费结果后rid必须失效 -- 这是防止"读到别人的结果"的第一道防线
    /// </summary>
    [Test]
    public void TestGetResultInvalidatesRid() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        promise.SetResult(rid, 42);

        Assert.AreEqual(42, promise.GetResult(rid));
        // 消费后rid失效
        Assert.IsTrue(promise.IsRecycled(rid), "rid should be invalidated after GetResult");
        Assert.Throws<InvalidOperationException>(() => promise.GetResult(rid));
    }

    /// <summary>
    /// 取异常同样触发失效
    /// </summary>
    [Test]
    public void TestGetExceptionInvalidatesRid() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        promise.SetException(rid, new InvalidOperationException("boom"));

        Assert.IsNotNull(promise.GetException(rid));
        Assert.IsTrue(promise.IsRecycled(rid));
        Assert.Throws<InvalidOperationException>(() => promise.GetException(rid));
    }

    /// <summary>
    /// Forget 也应使rid失效（用户已声明不需要结果）
    /// </summary>
    [Test]
    public void TestForgetInvalidatesRid() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        promise.SetResult(rid, 1);
        promise.Forget(rid);

        Assert.IsTrue(promise.IsRecycled(rid), "rid should be invalidated after Forget");
    }

    /// <summary>
    /// 旧rid不能访问复用后的对象 -- 核心防护场景
    /// 这是M-1缺陷的回归测试：若rid递增依赖对象池，池禁用时该守卫会完全失效
    /// </summary>
    [Test]
    public void TestStaleRidRejectedAfterReuse() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int oldRid);
        promise.SetResult(oldRid, 100);
        promise.GetResult(oldRid); // 触发回收

        // 模拟对象被复用：手动推进到下一个使用周期
        int newRid = promise.IncReentryId();
        Assert.AreNotEqual(oldRid, newRid, "rid must advance on reuse");

        promise.SetResult(newRid, 200);
        // 持有旧rid的第三方不能读到新任务的结果
        Assert.Throws<InvalidOperationException>(() => promise.GetResult(oldRid),
            "stale rid must not read the reused promise's result");
        Assert.AreEqual(200, promise.GetResult(newRid));
    }

    /// <summary>
    /// ignoreReentrant=true 时跳过校验（框架内部诊断用途）
    /// </summary>
    [Test]
    public void TestIgnoreReentrantBypassesValidation() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        promise.SetResult(rid, 7);
        promise.GetResult(rid); // rid失效

        // ignoreReentrant下不抛异常，且不再次触发回收
        Assert.DoesNotThrow(() => promise.GetStatus(rid, true));
    }

    /// <summary>
    /// 未完成时取结果应抛异常，且不应使rid失效（否则用户无法重试）
    /// </summary>
    [Test]
    public void TestGetResultBeforeCompletionKeepsRidValid() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);

        Assert.Throws<InvalidOperationException>(() => promise.GetResult(rid));
        // rid仍有效，可以正常完成并取结果
        Assert.IsFalse(promise.IsRecycled(rid), "rid must stay valid when task has not completed");
        promise.SetResult(rid, 5);
        Assert.AreEqual(5, promise.GetResult(rid));
    }

    /// <summary>
    /// IsRecycledOrCompleted 的语义：完成或回收都返回true
    /// </summary>
    [Test]
    public void TestIsRecycledOrCompleted() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        Assert.IsFalse(promise.IsRecycledOrCompleted(rid), "pending promise is neither recycled nor completed");

        promise.SetResult(rid, 1);
        Assert.IsTrue(promise.IsRecycledOrCompleted(rid), "completed promise reports true");
    }

    /// <summary>
    /// 重复完成应被拒绝
    /// </summary>
    [Test]
    public void TestDoubleCompleteRejected() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        Assert.IsTrue(promise.TrySetResult(rid, 1));
        Assert.IsFalse(promise.TrySetResult(rid, 2), "second completion must be rejected");
        Assert.IsFalse(promise.TrySetException(rid, new Exception()), "cannot fail an already-succeeded promise");
        Assert.AreEqual(1, promise.GetResult(rid));
    }

    /// <summary>
    /// 取消状态的传播
    /// </summary>
    [Test]
    public void TestCancelledStatus() {
        ValuePromise<int> promise = ValuePromise<int>.Acquire(out int rid);
        Assert.IsTrue(promise.TrySetCancelled(rid));
        Assert.AreEqual(TaskStatus.Cancelled, promise.GetStatus(rid));
        Assert.Throws<OperationCanceledException>(() => promise.GetResult(rid));
    }

    /// <summary>
    /// 池被禁用时rid同样必须失效 -- M-1缺陷的回归测试
    ///
    /// 背景：非int/object类型默认不池化(TaskPoolConfig)，此时PrepareToRecycle不会调用Reset。
    /// 若rid递增写在Reset中，则池禁用时一次性消费语义完全失去守卫，
    /// 且行为会随池配置变化（开发期正常、上线后抛异常或读到别人的结果）。
    /// </summary>
    [Test]
    public void TestRidInvalidatedEvenWhenPoolDisabled() {
        // UnpooledResult是自定义类型，默认不会被池化
        Assert.AreEqual(0, TaskPoolConfig.GetPoolSize<UnpooledResult>(TaskPoolType.ValuePromise, true),
            "precondition: this type must not be pooled");

        ValuePromise<UnpooledResult> promise = ValuePromise<UnpooledResult>.Acquire(out int rid);
        promise.SetResult(rid, new UnpooledResult());
        promise.GetResult(rid);

        Assert.IsTrue(promise.IsRecycled(rid),
            "rid must be invalidated regardless of whether the pool is enabled");
        Assert.Throws<InvalidOperationException>(() => promise.GetResult(rid));
    }

    /// <summary>
    /// 用于验证未池化路径的类型（不可为int/object）
    /// </summary>
    private sealed class UnpooledResult
    {
    }
}
