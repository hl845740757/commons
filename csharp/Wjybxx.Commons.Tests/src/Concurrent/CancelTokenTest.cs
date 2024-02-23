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

using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class CancelTokenTest
{
    /** 用于测试异步执行 */
    private static readonly IEventLoop globalEventLoop = EventLoopBuilder.NewBuilder(new DefaultThreadFactory("Scheduler", true)).Build();

    static CancelTokenTest() {
        globalEventLoop.Start().Join();
    }

    private static ICancelTokenSource newTokenSource(int code = 0) {
        // TODO 不同实现之间切换
        return new CancelTokenSource(code);
    }

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

            Assert.IsFalse(signal.IsDone);
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

            Assert.IsFalse(signal.IsDone);
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

            Assert.IsFalse(signal.IsDone);
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

            Assert.IsFalse(signal.IsDone);
            cts.Cancel(1);
            signal.AwaitUninterruptibly();
            Assert.NotNull(signal.ResultNow());
        }
    }

    [Test]
    public void testDelayInterrupt() {
        ICancelTokenSource cts = new CancelTokenSource();
        cts.CancelAfter(1, 100);

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

    #endregion
}