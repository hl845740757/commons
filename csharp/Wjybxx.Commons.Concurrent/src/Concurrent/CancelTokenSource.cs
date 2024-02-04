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
using System.Threading;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 取消令牌
/// </summary>
public class CancelTokenSource : ICancelTokenSource
{
    private static readonly IScheduledExecutorService delayer;

    private volatile int code;
    private volatile Completion stack;

    public CancelTokenSource() {
    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            this.code = ICancelToken.CheckCode(code);
        }
    }


    public CancelTokenSource newChild() {
        CancelTokenSource child = new CancelTokenSource();
        thenTransferTo(child);
        return child;
    }

    public ICancelToken AsReadonly() {
        return new ReadonlyCancelToken(this);
    }

    #region tokenSource

    public int cancel(int cancelCode = ICancelToken.REASON_DEFAULT) {
        ICancelToken.CheckCode(cancelCode);
        int preCode = Interlocked.CompareExchange(ref code, cancelCode, 0);
        if (preCode != 0) {
            return preCode;
        }
        PostComplete(this);
        return 0;
    }

    public void cancelAfter(int cancelCode, long millisecondsDelay) {
        throw new NotImplementedException();
    }

    public void cancelAfter(int cancelCode, TimeSpan timeSpan) {
        throw new NotImplementedException();
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan, IScheduledExecutorService executor) {
    }

    #endregion

    public int CancelCode => code;

    public bool IsCancelling() => code != 0;

    public int Reason() => ICancelToken.Reason(code);

    public int Degree() => ICancelToken.Degree(code);

    public bool IsInterruptible() => ICancelToken.isInterruptible(code);

    public bool IsWithoutRemove() {
        return ICancelToken.IsWithoutRemove(code);
    }

    public void CheckCancel() {
        int code = this.code;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #region core

    /** 用于表示任务已申领权限 */
    private static readonly IExecutor CLAIMED = APromise.CLAIMED;
    private const int SYNC = APromise.SYNC;
    private const int ASYNC = APromise.ASYNC;
    private const int NESTED = APromise.NESTED;

    private bool PushCompletion(Completion newHead) {
        if (IsCancelling()) {
            newHead.TryFire(SYNC);
            return false;
        }
        Completion expectedHead = stack;
        Completion realHead;
        while (expectedHead != TOMBSTONE) {
            newHead.next = expectedHead;
            realHead = Interlocked.CompareExchange(ref stack, newHead, expectedHead);
            if (realHead == expectedHead) { // success
                return true;
            }
            expectedHead = realHead; // retry
        }
        newHead.next = null;
        newHead.TryFire(SYNC);
        return false;
    }

    private static void PostComplete(CancelTokenSource source) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = ClearListeners(source, next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                curr.next = null; // help gc

                source = curr.TryFire(NESTED);
                if (source != null) {
                    goto outer;
                }
            }
            break;
        }
    }

    private static Completion? ClearListeners(CancelTokenSource source, Completion? onto) {
        Completion head;
        do {
            head = source.stack;
            if (head == TOMBSTONE) {
                return onto;
            }
        } while (Interlocked.CompareExchange(ref source.stack, TOMBSTONE, head) != head);

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            if (tmpHead.action == TOMBSTONE) {
                continue; // 跳过被删除节点
            }

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    private static bool Submit(Completion completion, IExecutor e, int options) {
        // 尝试内联
        if (TaskOption.isEnabled(options, TaskOption.STAGE_TRY_INLINE)
            && e is ISingleThreadExecutor eventLoop
            && eventLoop.InEventLoop()) {
            return true;
        }
        // 判断是否需要传递选项
        if (options != 0
            && !TaskOption.isEnabled(options, TaskOption.STAGE_NON_TRANSITIVE)) {
            e.Execute(completion);
        } else {
            completion.Options = 0;
            e.Execute(completion);
        }
        return false;
    }

    #endregion

    private abstract class Completion : ITask
    {
        /** 非volatile，由栈顶的cas更新保证可见性 */
        internal Completion? next;

        IExecutor? executor;
        int options;
        CancelTokenSource source;
        /**
         * 用户回调
         * 1.通知和清理时置为{@link #TOMBSTONE}
         * 2.子类在执行action之前需要调用{@link #popAction()}竞争。
         */
        internal object action;

        protected Completion() {
            executor = null;
            source = null!;
            action = null!;
        }

        protected Completion(IExecutor? executor, int options, CancelTokenSource source, object action) {
            this.executor = executor;
            this.options = options;
            this.source = source;
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public int Options {
            get => options;
            set => options = value;
        }

        public void Run() {
            TryFire(ASYNC);
        }

        protected internal abstract CancelTokenSource? TryFire(int mode);

        protected bool Claim() {
            IExecutor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return Submit(this, e, options);
            }
            return true;
        }
    }

    /// <summary>
    /// 该实例表示stack已被清理
    /// </summary>
    private static readonly Completion TOMBSTONE = new MockCompletion();

    private class MockCompletion : Completion
    {
        public MockCompletion() : base() {
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            throw new NotImplementedException();
        }
    }
}