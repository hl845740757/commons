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
using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Mutable;
using Wjybxx.Commons.Sequential;
using Wjybxx.Disruptor;

namespace Commons.Tests.Concurrent;

public class CancelTokenTest
{
    /** 用于测试异步执行 */
    private static readonly IEventLoop globalEventLoop = new DisruptorEventLoopBuilder<MiniAgentEvent>()
    {
        ThreadFactory = new DefaultThreadFactory("Scheduler", true),
        EventSequencer = new RingBufferEventSequencer<MiniAgentEvent>.Builder(MiniAgentEvent.FACTORY)
            .Build()
    }.Build();

    static CancelTokenTest() {
        globalEventLoop.Start().Join();
    }

    private static volatile int mode = 0;

    private static ICancelTokenSource newTokenSource(int code = 0) {
        if ((Interlocked.Increment(ref mode) & 1) == 0) {
            return new CancelTokenSource(code);
        } else {
            return new UniCancelTokenSource(code);
        }
    }

    #region 公共测试

    [Test] [Repeat(4)]
    public void testRegisterBeforeCancel() {
        ICancelTokenSource cts = newTokenSource();
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenRun(() => { signal.Value = "cancelled"; });
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    /** 测试是否立即执行 */
    [Test] [Repeat(4)]
    public void testRegisterAfterCancel() {
        ICancelTokenSource cts = newTokenSource(1);
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenRun(() => { signal.Value = "cancelled"; });
            Assert.IsNotNull(signal.Value);
        }
    }

    /** unregister似乎比deregister的使用率更高... */
    [Test] [Repeat(4)]
    public void testUnregister() {
        ICancelTokenSource cts = newTokenSource(0);
        {
            MutableObject<string> signal = new MutableObject<string>();
            IRegistration handle = cts.ThenRun(() => { signal.Value = "cancelled"; });
            handle.Dispose();

            cts.Cancel(1);
            Assert.IsNull(signal.Value);
        }
    }

    /** 测试多个监听的取消 */
    [Test] [Repeat(10)]
    public void testUnregister2() {
        ICancelTokenSource cts = newTokenSource(0);
        {
            // 通知是单线程的，因此无需使用Atomic
            MutableInt counter = new MutableInt(0);
            int count = 10;
            List<IRegistration> registrationList = new List<IRegistration>(count);
            for (int i = 0; i < count; i++) {
                registrationList.Add(cts.ThenRun(counter.Increment));
            }
            // 打乱顺序，然后随机取消一部分
            CollectionUtil.Shuffle(registrationList);

            int cancelCount = MathCommon.SharedRandom.Next(count);
            for (int i = 0; i < cancelCount; i++) {
                registrationList[i].Dispose();
            }
            cts.Cancel(1);
            Assert.AreEqual(count - cancelCount, counter.IntValue);
        }
    }

    /** 测试在已取消的令牌上监听取消，然后中断线程 */
    [Test] [Repeat(4)]
    public void testInterrupt() {
        ICancelTokenSource cts = newTokenSource(0);
        cts.Cancel(1);

        Thread thread = Thread.CurrentThread;
        cts.ThenRun(thread.Interrupt);

        bool interrupted;
        try {
            thread.Join(10 * 1000);
            interrupted = false;
        }
        catch (ThreadInterruptedException) {
            interrupted = true;
        }
        Assert.IsTrue(interrupted);
    }

    [Test] [Repeat(4)]
    public void testThenAccept() {
        ICancelTokenSource cts = newTokenSource();
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenAccept((token) => {
                Assert.AreSame(cts, token);
                signal.Value = "cancelled";
            });
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    [Test] [Repeat(4)]
    public void testThenAcceptCtx() {
        ICancelTokenSource cts = newTokenSource();
        Context<string> rootCtx = Context<string>.OfBlackboard("root");
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenAccept((token, ctx) => {
                Assert.AreSame(rootCtx, ctx);
                Assert.AreSame(cts, token);
                signal.Value = "cancelled";
            }, rootCtx);
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    [Test] [Repeat(4)]
    public void testThenRun() {
        ICancelTokenSource cts = newTokenSource();
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenRun(() => { signal.Value = ("cancelled"); });
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    [Test] [Repeat(4)]
    public void testThenRunCtx() {
        ICancelTokenSource cts = newTokenSource();
        Context<string> rootCtx = Context<string>.OfBlackboard("root");
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenRun((ctx) => {
                Assert.AreSame(rootCtx, ctx);
                signal.Value = ("cancelled");
            }, rootCtx);
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    [Test] [Repeat(4)]
    public void testNotify() {
        ICancelTokenSource cts = newTokenSource();
        {
            MutableObject<string> signal = new MutableObject<string>();
            cts.ThenNotify(new Listener((token) => {
                Assert.AreSame(cts, token);
                signal.Value = ("cancelled");
            }));
            Assert.IsNull(signal.Value);
            cts.Cancel(1);
            Assert.IsNotNull(signal.Value);
        }
    }

    [Test] [Repeat(4)]
    public void testTransferTo() {
        MutableObject<string> signal = new MutableObject<string>();
        ICancelTokenSource child = newTokenSource();
        {
            child.ThenRun(() => { signal.Value = "cancelled"; });
            Assert.IsNull(signal.Value);
        }
        ICancelTokenSource cts = newTokenSource();
        cts.ThenTransferTo(child);
        cts.Cancel(1);

        Assert.IsNotNull(signal.Value);
    }

    private class Listener : ICancelTokenListener
    {
        private readonly Action<ICancelToken> listener;

        public Listener(Action<ICancelToken> listener) {
            this.listener = listener;
        }


        public void OnCancelRequested(ICancelToken cancelToken) {
            listener.Invoke(cancelToken);
        }
    }

    # endregion

    #region cts

    [Test]
    public void testThenAcceptAsync() {
        ICancelTokenSource cts = newTokenSource();
        {
            Promise<string> signal = new Promise<string>();
            cts.ThenAcceptAsync(globalEventLoop, (token) => {
                Assert.IsTrue(globalEventLoop.InEventLoop());
                Assert.AreSame(cts, token);
                signal.TrySetResult("cancelled");
            });

            Assert.IsFalse(signal.IsCompleted);
            cts.Cancel(1);
            signal.AwaitUninterruptibly();
            Assert.NotNull(signal.ResultNow());
        }
    }

    [Test]
    public void testThenAcceptCtxAsync() {
        ICancelTokenSource cts = newTokenSource();
        Context<string> rootCtx = Context<string>.OfBlackboard("root");
        {
            Promise<string> signal = new Promise<string>();
            cts.ThenAcceptAsync(globalEventLoop, (token, ctx) => {
                Assert.IsTrue(globalEventLoop.InEventLoop());
                Assert.AreSame(rootCtx, ctx);
                Assert.AreSame(cts, token);
                signal.TrySetResult("cancelled");
            }, rootCtx);

            Assert.IsFalse(signal.IsCompleted);
            cts.Cancel(1);
            signal.AwaitUninterruptibly();
            Assert.NotNull(signal.ResultNow());
        }
    }

    [Test]
    public void testThenRunAsync() {
        ICancelTokenSource cts = newTokenSource();
        {
            Promise<string> signal = new Promise<string>();
            cts.ThenRunAsync(globalEventLoop, () => {
                Assert.IsTrue(globalEventLoop.InEventLoop());
                signal.TrySetResult("cancelled");
            });

            Assert.IsFalse(signal.IsCompleted);
            cts.Cancel(1);
            signal.AwaitUninterruptibly();
            Assert.NotNull(signal.ResultNow());
        }
    }

    [Test]
    public void testThenRunCtxAsync() {
        ICancelTokenSource cts = newTokenSource();
        Context<string> rootCtx = Context<string>.OfBlackboard("root");
        {
            Promise<string> signal = new Promise<string>();
            cts.ThenRunAsync(globalEventLoop, (ctx) => {
                Assert.IsTrue(globalEventLoop.InEventLoop());
                Assert.AreSame(rootCtx, ctx);
                signal.TrySetResult("cancelled");
            }, rootCtx);

            Assert.IsFalse(signal.IsCompleted);
            cts.Cancel(1);
            signal.AwaitUninterruptibly();
            Assert.NotNull(signal.ResultNow());
        }
    }

    [Test]
    public void testDelayInterrupt() {
        if (!globalEventLoop.IsRunning) {
            throw new IllegalStateException();
        }

        ICancelTokenSource cts = new CancelTokenSource();
        cts.CancelAfter(1, 100);

        Thread thread = Thread.CurrentThread;
        cts.ThenRun(() => { thread.Interrupt(); });

        bool interrupted;
        try {
            thread.Join(10 * 1000);
            interrupted = false;
        }
        catch (ThreadInterruptedException) {
            interrupted = true;
        }
        Assert.IsTrue(interrupted);
    }

    #endregion


    #region UniCts

    [Test]
    public void testCancelCode() {
        int reason = 1024;
        int degree = 7;

        CancelCodeBuilder builder = new CancelCodeBuilder()
        {
            Reason = reason,
            Degree = degree,
            IsInterruptible = true
        };

        Assert.AreEqual(reason, builder.Reason);
        Assert.AreEqual(degree, builder.Degree);
        Assert.IsTrue(builder.IsInterruptible);

        int code = builder.Build();
        ICancelTokenSource cts = newTokenSource(0);
        cts.Cancel(code);

        Assert.AreEqual(code, cts.CancelCode);
        Assert.AreEqual(reason, cts.Reason);
        Assert.AreEqual(degree, cts.Degree);
        Assert.IsTrue(cts.IsInterruptible);
    }

    #endregion
}