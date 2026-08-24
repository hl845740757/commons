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
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Logger;
using Wjybxx.Disruptor;
using TaskStatus = Wjybxx.Commons.Concurrent.TaskStatus;

namespace Commons.Tests.Concurrent;

/// <summary>
/// <see cref="FutureCombiner"/>的测试用例。
///
/// 测试分为四部分：
/// 1. 单线程下的语义测试 -- 使用已完成的Promise，结果完全确定；
/// 2. 取消语义 -- 取消是本库的一等状态，不应被降级为普通失败；
/// 3. 并发压力测试 -- 确保并发情况下不会由于竞争触发错误的结果；
/// </summary>
public class FutureCombinerTest
{
    #region 工具方法

    /// <summary>
    /// 屏蔽FutureLogger的输出。
    /// 聚合失败是多数用例的预期结果，压力测试更会产生上万条日志。
    /// </summary>
    private sealed class SilentLogHandler : FutureLogger.ILogHandler
    {
        public void LogCause(Level level, Exception ex, string message) {
        }
    }

    [SetUp]
    public void SetUp() {
        FutureLogger.Handler = new SilentLogHandler();
    }

    [TearDown]
    public void TearDown() {
        FutureLogger.Handler = null;
    }

    /// <summary>已成功的future</summary>
    private static IFuture Succeeded(object? value = null) {
        IPromise<object> promise = new Promise<object>();
        promise.TrySetResult(value);
        return promise;
    }

    /// <summary>已失败的future</summary>
    private static IFuture Failed(string message = "failed") {
        IPromise<object> promise = new Promise<object>();
        promise.TrySetException(new InvalidOperationException(message));
        return promise;
    }

    /// <summary>已取消的future</summary>
    private static IFuture Cancelled() {
        IPromise<object> promise = new Promise<object>();
        promise.TrySetCancelled();
        return promise;
    }

    /// <summary>未完成的future</summary>
    private static IPromise<object> Pending() {
        return new Promise<object>();
    }

    #endregion

    #region add

    [Test]
    public void TestFutureCountIsNotDeduplicated() {
        IFuture future = Succeeded();
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(future).Add(future).Add(future);
        // 文档明确：future计数不去重
        Assert.AreEqual(3, combiner.FutureCount);
    }
    
    /// <summary>
    /// 调用选择方法后不可再添加future
    /// </summary>
    [Test]
    public void TestAddAfterFinishThrows() {
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(Succeeded());
        combiner.WhenAll();

        Assert.Throws<InvalidOperationException>(() => combiner.Add(Succeeded()));
        Assert.Throws<InvalidOperationException>(() => combiner.AddAll(Succeeded()));
        Assert.Throws<InvalidOperationException>(() => combiner.WhenAll(), "不可重复选择");
        Assert.Throws<InvalidOperationException>(() => combiner.WhenAny());
    }

    [Test]
    public void TestClearAllowsReuse() {
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(Succeeded());
        Assert.IsTrue(combiner.WhenAll().IsSucceeded);

        combiner.Clear();
        Assert.AreEqual(0, combiner.FutureCount);

        combiner.Add(Failed());
        Assert.IsTrue(combiner.WhenAll().IsFailed, "Clear后应可重新添加并选择");
    }

    /// <summary>
    /// Clear之后，旧future的回调不应影响新的聚合结果。
    /// （Clear会替换futures和listener，旧listener已与新promise解绑）
    /// </summary>
    [Test]
    public void TestClearIsolatesOldFutures() {
        IPromise<object> pending = Pending();
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(pending);
        IPromise<object> first = combiner.WhenAll();

        combiner.Clear();
        combiner.Add(Succeeded());
        IPromise<object> second = combiner.WhenAll();
        Assert.IsTrue(second.IsSucceeded);

        // 旧future完成时，只应影响旧的聚合promise
        pending.TrySetException(new InvalidOperationException("late"));
        Assert.IsTrue(first.IsFailed);
        Assert.IsTrue(second.IsSucceeded, "旧future不应污染Clear后的聚合结果");
    }

    /// <summary>
    /// Clear必须替换futures列表，而不是复用（Clear）同一个列表实例。
    ///
    /// 旧listener持有futures引用并以futures.Count作为futureCount，
    /// 若Clear只是清空共享列表，旧listener的futureCount会被篡改，
    /// 导致尚未完成的聚合promise提前完成。
    /// </summary>
    [Test]
    public void TestClearDoesNotAliasOldFutureList() {
        IPromise<object> p1 = Pending();
        IPromise<object> p2 = Pending();
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(p1).Add(p2);
        IPromise<object> first = combiner.WhenAll();

        combiner.Clear();

        // 旧聚合仍在等待2个future，此时只完成1个
        p1.TrySetResult("ok");
        Assert.IsFalse(first.IsCompleted,
            "旧聚合promise仍应等待第2个future；提前完成说明futures列表被共享篡改");

        p2.TrySetResult("ok");
        Assert.IsTrue(first.IsSucceeded);
    }

    #endregion

    #region SetPromise

    [Test]
    public void TestSetPromiseReceivesResult() {
        IPromise<object> custom = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> returned = combiner
            .SetPromise(custom)
            .AddAll(Succeeded(), Succeeded())
            .WhenAll();

        Assert.AreSame(custom, returned, "应返回用户指定的promise");
        Assert.IsTrue(custom.IsSucceeded);
    }

    [Test]
    public void TestSetPromiseNullFallsBackToNewPromise() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> returned = combiner
            .SetPromise(null)
            .Add(Succeeded())
            .WhenAll();
        Assert.IsNotNull(returned);
        Assert.IsTrue(returned.IsSucceeded);
    }

    /// <summary>
    /// 若用户传入的promise已完成，聚合器不应抛异常（内部使用TrySetXXX）
    /// </summary>
    [Test]
    public void TestSetPromiseAlreadyCompleted() {
        IPromise<object> custom = Pending();
        custom.TrySetResult("preset");

        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> returned = combiner
            .SetPromise(custom)
            .Add(Failed())
            .WhenAll();

        Assert.AreSame(custom, returned);
        Assert.IsTrue(custom.IsSucceeded, "已完成的promise不应被覆盖");
        Assert.AreEqual("preset", custom.ResultNow());
    }

    #endregion

    #region WhenAny

    [Test]
    public void TestWhenAnyEmptyNeverCompletes() {
        // 文档明确：future数量为0时，返回的promise无法进入完成状态
        IPromise<object> promise = new FutureCombiner().WhenAny();
        Assert.IsFalse(promise.IsCompleted);
        Assert.AreEqual(TaskStatus.Pending, promise.Status);
    }

    [Test]
    public void TestWhenAnySucceededWinsOverPending() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded("first"), Pending())
            .WhenAny();

        Assert.IsTrue(promise.IsSucceeded);
        Assert.AreEqual("first", promise.ResultNow());
    }

    [Test]
    public void TestWhenAnyPropagatesNullResult() {
        // 内部使用NIL编码区分"成功但结果为null"与"未成功"
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded(null), Pending())
            .WhenAny();

        Assert.IsTrue(promise.IsSucceeded, "结果为null的成功不应被误判为失败");
        Assert.IsNull(promise.ResultNow());
    }

    [Test]
    public void TestWhenAnyFailedWhenOnlyFailureDone() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("boom"), Pending())
            .WhenAny();

        Assert.IsTrue(promise.IsFailed);
        Assert.IsInstanceOf<InvalidOperationException>(promise.ExceptionNow(false));
    }

    /// <summary>
    /// WhenAny应透传取消状态（而非降级为失败）
    /// </summary>
    [Test]
    public void TestWhenAnyPropagatesCancellation() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Cancelled(), Pending())
            .WhenAny();

        Assert.IsTrue(promise.IsCancelled);
        Assert.AreEqual(TaskStatus.Cancelled, promise.Status);
    }

    /// <summary>
    /// 后完成的future不应覆盖已完成的聚合结果
    /// </summary>
    [Test]
    public void TestWhenAnyIgnoresLaterCompletions() {
        IPromise<object> late = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded("winner"), late)
            .WhenAny();
        Assert.AreEqual("winner", promise.ResultNow());

        late.TrySetException(new InvalidOperationException("late"));
        Assert.IsTrue(promise.IsSucceeded);
        Assert.AreEqual("winner", promise.ResultNow(), "结果不应被后完成的future改写");
    }

    #endregion

    #region WhenAll

    [Test]
    public void TestWhenAllEmptySucceeds() {
        IPromise<object> promise = new FutureCombiner().WhenAll();
        Assert.IsTrue(promise.IsSucceeded);
        Assert.IsNull(promise.ResultNow(), "聚合成功不返回具体结果");
    }

    [Test]
    public void TestWhenAllAllSucceeded() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded("a"), Succeeded("b"), Succeeded("c"))
            .WhenAll();

        Assert.IsTrue(promise.IsSucceeded);
        Assert.IsNull(promise.ResultNow());
    }

    [Test]
    public void TestWhenAllWaitsForAll() {
        IPromise<object> pending = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded(), Failed(), pending)
            .WhenAll();

        Assert.IsFalse(promise.IsCompleted, "WhenAll无快速失败逻辑，必须等待全部完成");

        pending.TrySetResult("done");
        Assert.IsTrue(promise.IsFailed);
    }

    /// <summary>
    /// WhenAll存在失败时应聚合【所有】异常
    /// </summary>
    [Test]
    public void TestWhenAllAggregatesAllExceptions() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("e1"), Succeeded(), Failed("e2"), Failed("e3"))
            .WhenAll();

        Assert.IsTrue(promise.IsFailed);
        AggregateException ex = (AggregateException)promise.ExceptionNow(false);
        Assert.AreEqual(3, ex.InnerExceptions.Count, "应聚合全部3个异常");
    }

    #endregion

    #region Select / SelectAll

    [Test]
    public void TestSelectZeroAlwaysSucceeds() {
        // 文档明确：require等于0则必定成功
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("e1"), Failed("e2"))
            .WhenNSuccess(0);
        Assert.IsTrue(promise.IsSucceeded);
    }

    [Test]
    public void TestSelectNegativeThrows() {
        FutureCombiner combiner = new FutureCombiner();
        combiner.Add(Succeeded());
        Assert.Throws<ArgumentException>(() => combiner.WhenNSuccess(-1));
    }

    [Test]
    public void TestSelectRequiredGreaterThanCountFails() {
        // 文档明确：require大于监听的future数量，必定失败
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded(), Succeeded())
            .WhenNSuccess(3);

        Assert.IsTrue(promise.IsFailed);
        Assert.IsFalse(promise.IsCancelled, "任务数不足属于失败，不是取消");
    }

    [Test]
    public void TestSelectSucceedsWhenRequiredReached() {
        IPromise<object> pending = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Succeeded(), Succeeded(), pending)
            .WhenNSuccess(2);

        Assert.IsTrue(promise.IsSucceeded, "达到期望成功数即应立即完成，无需等待剩余任务");
        Assert.IsFalse(pending.IsCompleted, "聚合器不应影响上游future");
    }

    /// <summary>
    /// failFast=true：剩余任务不足以达成目标时立即失败
    /// </summary>
    [Test]
    public void TestSelectFailFastCompletesEarly() {
        IPromise<object> pending = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("e1"), Failed("e2"), pending)
            .WhenNSuccess(3, true);

        // 3个任务要求全部成功，已有2个失败 -> 无论pending结果如何都不可能成功
        Assert.IsTrue(promise.IsFailed, "failFast应在无法达成目标时立即失败");
        Assert.IsFalse(pending.IsCompleted);
    }

    /// <summary>
    /// failFast=false：即使已确定失败，也要等待所有任务完成
    /// </summary>
    [Test]
    public void TestSelectNonFailFastWaitsForAll() {
        IPromise<object> pending = Pending();
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("e1"), Failed("e2"), pending)
            .WhenNSuccess(3, false);

        Assert.IsFalse(promise.IsCompleted, "非快速失败模式应等待所有任务完成");

        pending.TrySetResult("ok");
        Assert.IsTrue(promise.IsFailed);
    }

    [Test]
    public void TestSelectAllEmptySucceeds() {
        // SelectAll等价于Select(FutureCount)，0个future时require为0
        IPromise<object> promise = new FutureCombiner().WhenAllSuccess();
        Assert.IsTrue(promise.IsSucceeded);
    }

    [Test]
    public void TestSelectAllRequiresEverySuccess() {
        FutureCombiner combiner = new FutureCombiner();
        Assert.IsTrue(combiner.AddAll(Succeeded(), Succeeded()).WhenAllSuccess().IsSucceeded);

        FutureCombiner combiner2 = new FutureCombiner();
        Assert.IsTrue(combiner2.AddAll(Succeeded(), Failed()).WhenAllSuccess().IsFailed);
    }

    /// <summary>
    /// SelectAll的require在调用时快照FutureCount
    /// </summary>
    [Test]
    public void TestSelectAllSnapshotsFutureCount() {
        FutureCombiner combiner = new FutureCombiner();
        combiner.AddAll(Succeeded(), Succeeded(), Succeeded());
        Assert.AreEqual(3, combiner.FutureCount);
        Assert.IsTrue(combiner.WhenAllSuccess().IsSucceeded);
    }

    [Test]
    public void TestSelectAggregatesFailureExceptions() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("e1"), Failed("e2"), Succeeded())
            .WhenNSuccess(3);

        AggregateException ex = (AggregateException)promise.ExceptionNow(false);
        Assert.AreEqual(2, ex.InnerExceptions.Count);
    }

    #endregion

    #region 并发压力测试

    /// <summary>
    /// 确保并发情况下不会由于竞争触发错误的结果。
    /// 半数任务失败，要求成功数恰好等于实际成功数。
    /// </summary>
    [Test]
    public void TestConcurrentSelectExactCount() {
        IEventLoopGroup consumer = new EventLoopGroupBuilder()
        {
            NumChildren = 4,
            EventLoopFactory = new EventLoopFactory(new DefaultThreadFactory("Child"), 2048)
        }.Build();
        try {
            Random random = new Random(12345); // 固定种子，便于复现
            const int taskCount = 2000;
            FutureCombiner combiner = new FutureCombiner(taskCount);
            int succeedCount = 0;
            for (int i = 0; i < taskCount; i++) {
                long delay = random.NextInt64(0, 10);
                IFuture future;
                if (random.Next(2) == 0) {
                    succeedCount++;
                    future = consumer.ScheduleAction(TaskSuccess, TimeSpan.FromMilliseconds(delay)).AsFuture();
                } else {
                    future = consumer.ScheduleAction(TaskFailure, TimeSpan.FromMilliseconds(delay)).AsFuture();
                }
                combiner.Add(future);
            }

            // 成功数恰好达标 -> 必须成功；任何竞争导致的计数错误都会使其失败
            Assert.IsNull(combiner.WhenNSuccess(succeedCount, false).Join());
        }
        finally {
            consumer.Shutdown();
            consumer.TerminationFuture.Join();
        }
    }

    /// <summary>
    /// 要求的成功数比实际多1 -> 必定失败。
    /// 用于验证failFast模式下不会因竞争误判为成功。
    /// </summary>
    [Test]
    public void TestConcurrentSelectOneMoreThanPossibleFails() {
        IEventLoopGroup consumer = new EventLoopGroupBuilder()
        {
            NumChildren = 4,
            EventLoopFactory = new EventLoopFactory(new DefaultThreadFactory("Child"), 2048)
        }.Build();
        try {
            Random random = new Random(54321);
            const int taskCount = 2000;
            FutureCombiner combiner = new FutureCombiner(taskCount);
            int succeedCount = 0;
            for (int i = 0; i < taskCount; i++) {
                long delay = random.NextInt64(0, 10);
                IFuture future;
                if (random.Next(2) == 0) {
                    succeedCount++;
                    future = consumer.ScheduleAction(TaskSuccess, TimeSpan.FromMilliseconds(delay)).AsFuture();
                } else {
                    future = consumer.ScheduleAction(TaskFailure, TimeSpan.FromMilliseconds(delay)).AsFuture();
                }
                combiner.Add(future);
            }

            IPromise<object> promise = combiner.WhenNSuccess(succeedCount + 1, true);
            promise.Await();
            Assert.IsTrue(promise.IsFailed);
        }
        finally {
            consumer.Shutdown();
            consumer.TerminationFuture.Join();
        }
    }

    /// <summary>
    /// 多线程并发完成时，WhenAll必须等到全部完成、且只完成一次
    /// </summary>
    [Test]
    [Repeat(5)]
    public void TestConcurrentWhenAllCompletesOnce() {
        const int count = 64;
        IPromise<object>[] promises = new IPromise<object>[count];
        FutureCombiner combiner = new FutureCombiner(count);
        for (int i = 0; i < count; i++) {
            promises[i] = Pending();
            combiner.Add(promises[i]);
        }
        IPromise<object> aggregate = combiner.WhenAll();

        int completionCount = 0;
        aggregate.OnCompleted((_, _) => Interlocked.Increment(ref completionCount), null);

        CountdownEvent ready = new CountdownEvent(count);
        ManualResetEventSlim go = new ManualResetEventSlim(false);
        Thread[] threads = new Thread[count];
        for (int i = 0; i < count; i++) {
            IPromise<object> promise = promises[i];
            threads[i] = new Thread(() => {
                ready.Signal();
                go.Wait();
                promise.TrySetResult("ok");
            });
            threads[i].Start();
        }
        ready.Wait();
        go.Set();
        foreach (Thread thread in threads) thread.Join();

        Assert.IsTrue(aggregate.IsSucceeded);
        Assert.AreEqual(1, completionCount, "聚合promise只应完成一次");
    }

    private static readonly Action TaskSuccess = () => { };
    private static readonly Action TaskFailure = () => throw new LightweightException();

    /// <summary>
    /// 不记录堆栈、不需要日志的轻量异常，避免压测时的开销与噪音
    /// </summary>
    private sealed class LightweightException : Exception, Wjybxx.Commons.Ex.NoLogRequiredException
    {
        public override string? StackTrace => null;
    }

    #endregion

    #region 取消语义

    /// <summary>
    /// WhenAny应优先返回成功结果。
    /// 回归用：判断条件曾被误改为 cause == null，导致已完成的future中
    /// 只要有一个失败就拿不到成功结果。
    /// </summary>
    [Test]
    public void TestWhenAnyPrefersSuccessOverFailure() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Failed("boom"), Succeeded("good"))
            .WhenAny();

        Assert.IsTrue(promise.IsSucceeded, "WhenAny应优先返回成功结果");
        Assert.AreEqual("good", promise.ResultNow());
    }

    /// <summary>
    /// 全部取消时，聚合结果应表现为取消而非失败。
    /// 取消在本库中是一等状态，调用方需要能区分"批量任务被正常取消"与"任务真的出错了"。
    /// </summary>
    [Test]
    public void TestWhenAllPropagatesCancellationWhenAllCancelled() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Cancelled(), Cancelled())
            .WhenAll();

        Assert.IsTrue(promise.IsCancelled, "全部取消时应表现为取消");
        Assert.AreEqual(TaskStatus.Cancelled, promise.Status);
        Assert.IsInstanceOf<OperationCanceledException>(promise.ExceptionNow(false));
    }

    [Test]
    public void TestSelectAllPropagatesCancellationWhenAllCancelled() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Cancelled(), Cancelled())
            .WhenAllSuccess();

        Assert.IsTrue(promise.IsCancelled);
    }

    /// <summary>
    /// 成功不影响取消的判定：未失败的future不产生异常，
    /// 因此"取消数 == 异常数"依然成立。
    /// </summary>
    [Test]
    public void TestWhenAllCancellationWithSuccessStillCancelled() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Cancelled(), Succeeded("ok"))
            .WhenAll();

        Assert.IsTrue(promise.IsCancelled, "取消 + 成功（无真实异常）仍应表现为取消");
    }

    /// <summary>
    /// 真实异常的优先级必须高于取消，不能被取消掩盖 -- 否则调用方会
    /// 误以为"这批任务被取消了"，而实际上有任务真的出错了。
    /// </summary>
    [Test]
    public void TestWhenAllRealFailureOutranksCancellation() {
        FutureCombiner combiner = new FutureCombiner();
        IPromise<object> promise = combiner
            .AddAll(Cancelled(), Failed("real"), Cancelled())
            .WhenAll();

        Assert.IsTrue(promise.IsFailed, "混有真实异常时不可降级为取消");
        Assert.IsFalse(promise.IsCancelled);

        AggregateException ex = (AggregateException)promise.ExceptionNow(false);
        Assert.AreEqual(3, ex.InnerExceptions.Count, "取消与失败都应被聚合");
    }

    #endregion
}
