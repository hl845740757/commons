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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 取消令牌
/// </summary>
public sealed class CancelTokenSource : ICancelTokenSource
{
    /// <summary>
    /// 默认的延迟调度器
    /// </summary>
    private static readonly IScheduledExecutorService _delayer = GlobalEventLoop.Inst;

    private volatile int code;
    private volatile Completion? stack;

    public CancelTokenSource() {
    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            this.code = CancelCodes.CheckCode(code);
        }
    }

    public bool CanBeCancelled => true;

    ICancelTokenSource ICancelTokenSource.NewInstance(bool copyCode) => NewInstance(copyCode);

    public CancelTokenSource NewInstance(bool copyCode = false) {
        return new CancelTokenSource(copyCode ? code : 0);
    }

    #region tokenSource

    public bool Cancel(int cancelCode = CancelCodes.REASON_DEFAULT) {
        CancelCodes.CheckCode(cancelCode);
        int preCode = InternalCancel(cancelCode);
        if (preCode != 0) {
            return false;
        }
        PostComplete(this);
        return true;
    }

    public void CancelAfter(int cancelCode, long millisecondsDelay) {
        CancelAfter(cancelCode, TimeSpan.FromMilliseconds(millisecondsDelay), _delayer);
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan) {
        CancelAfter(cancelCode, timeSpan, _delayer);
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan, IScheduledExecutorService delayer) {
        if (delayer == null) throw new ArgumentNullException(nameof(delayer));
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewTask(new Canceller(this, cancelCode));
        builder.SetOnlyOnce(timeSpan.Ticks, new TimeSpan(1));
        delayer.Schedule(in builder);
    }

    private class Canceller : ITask
    {
        private readonly CancelTokenSource source;
        private readonly int cancelCode;

        public Canceller(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        public int Options => 0;

        public void Run() {
            source.Cancel(cancelCode);
        }
    }

    #endregion

    #region code

    public int CancelCode => code;

    public bool IsCancelRequested => code != 0;

    public int Reason => CancelCodes.GetReason(code);

    public void CheckCancel() {
        int code = this.code;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #endregion

    #region 监听器

    #region uni-accept

    public Registration ThenAccept(Action<ICancelToken> action, int options = 0) {
        return PushUniAccept(null, action, options);
    }

    public Registration ThenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAccept(executor, action, options);
    }

    private Registration PushUniAccept(IExecutor? executor, Action<ICancelToken> action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_ACCEPT, action, null);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_ACCEPT, action, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #region uni-accept-ctx

    public Registration ThenAccept(Action<ICancelToken, object> action, object? ctx, int options = 0) {
        return PushUniAcceptCtx(null, action, ctx, options);
    }

    public Registration ThenAcceptAsync(IExecutor executor, Action<ICancelToken, object> action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAcceptCtx(executor, action, ctx, options);
    }

    private Registration PushUniAcceptCtx(IExecutor? executor, Action<ICancelToken, object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_ACCEPT_CTX, action, state);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_ACCEPT_CTX, action, state);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #region uni-run

    public Registration ThenRun(Action action, int options = 0) {
        return PushUniRun(null, action, options);
    }

    public Registration ThenRunAsync(IExecutor executor, Action action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRun(executor, action, options);
    }

    private Registration PushUniRun(IExecutor? executor, Action action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_RUN, action, null);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_RUN, action, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #region uni-run-ctx

    public Registration ThenRun(Action<object> action, object? ctx, int options = 0) {
        return PushUniRunCtx(null, action, ctx, options);
    }

    public Registration ThenRunAsync(IExecutor executor, Action<object> action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRunCtx(executor, action, ctx, options);
    }

    private Registration PushUniRunCtx(IExecutor? executor, Action<object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_RUN_CTX, action, state);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_RUN_CTX, action, state);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #region uni-notify

    public Registration ThenNotify(ICancelTokenListener action, object? ctx, int options = 0) {
        return PushUniNotify(null, action, ctx, options);
    }

    public Registration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniNotify(executor, action, ctx, options);
    }

    private Registration PushUniNotify(IExecutor? executor, ICancelTokenListener listener, object? ctx, int options) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_NOTIFY, listener, ctx);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_NOTIFY, listener, ctx);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #region uni-transfer

    public Registration ThenTransferTo(ICancelTokenSource child, int options = 0) {
        return PushUniTransfer(null, child, options);
    }

    public Registration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniTransfer(executor, child, options);
    }

    private Registration PushUniTransfer(IExecutor? executor, ICancelTokenSource child, int options) {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_TRANSFER, child, null);
            return Registration.Closed;
        }
        Completion completion = GetComplete(executor, options, this, TYPE_TRANSFER, child, null);
        // 需要在Push前拿到_rid
        Registration registration = new Registration(completion, completion._rid);
        return PushCompletion(completion) ? registration : Registration.Closed;
    }

    #endregion

    #endregion

    #region core

    /** 用于表示任务已申领权限 */
    private static readonly IExecutor CLAIMED = AbstractPromise.CLAIMED;
    private const int SYNC = AbstractPromise.SYNC;
    private const int ASYNC = AbstractPromise.ASYNC;
    private const int NESTED = AbstractPromise.NESTED;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int InternalCancel(int cancelCode) {
        Debug.Assert(cancelCode != 0);
        return Interlocked.CompareExchange(ref code, cancelCode, 0);
    }

    private bool PushCompletion(Completion newHead) {
        if (IsCancelRequested) {
            newHead.TryFire(SYNC);
            return false;
        }
        Completion expectedHead = stack;
        Completion realHead;
        while (expectedHead != TOMBSTONE) {
            newHead.next = expectedHead;
            realHead = Interlocked.CompareExchange(ref this.stack, newHead, expectedHead);
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
        Completion head = source.stack;
        while (true) {
            if (head == TOMBSTONE) {
                return onto;
            }
            Completion realHead = Interlocked.CompareExchange(ref source.stack, TOMBSTONE, head);
            if (realHead == head) {
                break;
            }
            head = realHead;
        }
        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    private static bool TryInline(Completion completion, IExecutor e, int options) {
        // 尝试内联
        if (ExecutorUtil.IsInlinable(e, options)) {
            return true;
        }
        e.Execute(completion);
        return false;
    }

    #endregion

    #region completion

    private const int TYPE_ACCEPT = 0;
    private const int TYPE_ACCEPT_CTX = 1;
    private const int TYPE_RUN = 2;
    private const int TYPE_RUN_CTX = 3;
    private const int TYPE_NOTIFY = 4;
    private const int TYPE_TRANSFER = 5;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private const int MASK_TASK_TYPE = 0x0F;


    private static readonly Completion TOMBSTONE = new Completion();
    private static readonly ConcurrentObjectPool<Completion> POOL = new(
        () => new Completion(), c => c.Reset(), TaskPoolConfig.GetPoolSize<int>(TaskPoolType.CtsCompletion));

    /**  申请一个<see cref="Completion"/>实例 */
    private static Completion GetComplete(IExecutor? executor, int options, CancelTokenSource source,
                                          int type, object action, object? ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_PRIORITY_AND_SCHEDULE_PHASE);
        options |= type;

        Completion completion = POOL.Acquire();
        int rid = completion._rid + 1;
        completion._rid = rid;
        completion._fireId = rid;

        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    /** 为简化逻辑，我们总是在触发回调的时候才回收对象 */
    private sealed class Completion : ITask, IPooledDisposable
    {
        /** 非volatile，由栈顶的cas更新保证可见性 */
        internal Completion? next;
        /** 重入id -- 只增不减 */
        internal volatile int _rid;
        /// <summary>
        /// cts添加回调时的<see cref="_rid"/>快照
        /// 1.该值永不清理，用于识别能否进行通知
        /// 2.<see cref="TryFire"/>方法需要通过该值竞争更新<see cref="_rid"/>
        /// 3.<see cref="Dispose"/>方法通过用户持有的rid竞争更新<see cref="_rid"/>
        /// </summary>
        internal int _fireId;

#nullable disable
        /// <summary>
        /// 关联的取消令牌
        /// </summary>
        internal CancelTokenSource source;
        /// <summary>
        /// 绑定的线程
        /// </summary>
        internal IExecutor executor;
        /// <summary>
        /// 任务的调度选项，包含任务的类型
        /// </summary>
        internal int options;
#nullable enable
        /// <summary>
        /// 用户回调
        /// </summary>
        internal object? action;
        /// <summary>
        /// 回调关联的参数
        /// </summary>
        internal object? ctx;

        internal void Reset() {
            _rid++; // 池化时+1，volatile安全
            source = null!;
            executor = null;
            options = 0;
            action = null!;
            ctx = null;
        }

        public int Options => options;

        public void Run() {
            TryFire(ASYNC);
        }

        private bool Claim() {
            IExecutor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return TryInline(this, e, options);
            }
            return true;
        }

        /** 尝试竞争重入id */
        private bool TryIncrementRid(int reentryId) {
            return reentryId == _rid
                   && Interlocked.CompareExchange(ref _rid, reentryId + 1, reentryId) == reentryId;
        }

        internal CancelTokenSource? TryFire(int mode) {
            {
                // 同步Fire时，必须先竞争Action - 如果竞争失败，需要等待Close调用结束
                if (mode <= 0 && !TryIncrementRid(_fireId)) {
                    while (_rid != _fireId + 2) { // // 等待close完毕，不能使用小于测试(可能越界)
                        Thread.SpinWait(1);
                    }
                    goto outer;
                }
                if (ExecutorUtil.IsCancelRequested(ctx, options)) {
                    goto outer;
                }
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                Debug.Assert(action != null);
                FireNow(source, options, action, ctx);
            }
            outer:
            POOL.Release(this);
            return null;
        }

        internal static void FireNow(CancelTokenSource source,
                                     int options, object rawAction, object? ctx) {
            int taskType = (options & MASK_TASK_TYPE);
            try {
                switch (taskType) {
                    case TYPE_ACCEPT: {
                        Action<ICancelToken> action = (Action<ICancelToken>)rawAction;
                        action(source);
                        break;
                    }
                    case TYPE_ACCEPT_CTX: {
                        Action<ICancelToken, object?> action = (Action<ICancelToken, object?>)rawAction;
                        action(source, ctx);
                        break;
                    }
                    case TYPE_RUN: {
                        Action action = (Action)rawAction;
                        action();
                        break;
                    }
                    case TYPE_RUN_CTX: {
                        Action<object?> action = (Action<object?>)rawAction;
                        action(ctx);
                        break;
                    }
                    case TYPE_NOTIFY: {
                        ICancelTokenListener action = (ICancelTokenListener)rawAction;
                        action.OnCancelRequested(source, ctx);
                        break;
                    }
                    case TYPE_TRANSFER: {
                        // 这里本来有一个递归优化，为简化逻辑删除了
                        ICancelTokenSource action = (ICancelTokenSource)rawAction;
                        action.Cancel(source.code);
                        break;
                    }
                    default: {
                        throw new IllegalStateException();
                    }
                }
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "Action caught an exception");
            }
        }

        /// <summary>
        /// 关闭监听器
        /// </summary>
        /// <param name="reentryId">重入id</param>
        public void Dispose(int reentryId) {
            if (TryIncrementRid(reentryId)) {
                // 这里只释放action资源
                this.action = null!;
                this.ctx = null;
                // 更新为+2表示关闭完毕
                _rid = reentryId + 2;
            }
        }
    }

    #endregion
}
}