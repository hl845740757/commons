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
public sealed class CancelTokenSource : ICancelTokenSource
{
    /// <summary>
    /// 默认的延迟调度器
    /// </summary>
    private static readonly IScheduledExecutorService delayer;

    private volatile int code;
    private volatile Completion? stack;

    public CancelTokenSource() {
    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            this.code = ICancelToken.CheckCode(code);
        }
    }

    ICancelTokenSource ICancelTokenSource.NewChild() {
        return NewChild();
    }

    public CancelTokenSource NewChild() {
        CancelTokenSource child = new CancelTokenSource();
        ThenTransferTo(child);
        return child;
    }

    public ICancelToken AsReadonly() {
        return new ReadonlyCancelToken(this);
    }

    public bool CanBeCancelled => true;

    #region tokenSource

    public int Cancel(int cancelCode = ICancelToken.REASON_DEFAULT) {
        ICancelToken.CheckCode(cancelCode);
        int preCode = Interlocked.CompareExchange(ref this.code, cancelCode, 0);
        if (preCode != 0) {
            return preCode;
        }
        PostComplete(this);
        return 0;
    }

    public void CancelAfter(int cancelCode, long millisecondsDelay) {
        throw new NotImplementedException();
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan) {
        throw new NotImplementedException();
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan, IScheduledExecutorService delayer) {
        if (delayer == null) throw new ArgumentNullException(nameof(delayer));
        delayer.ScheduleAction(Callback, timeSpan, new Context(this, cancelCode));
    }

    private static void Callback(IContext rawContext) {
        Context context = (Context)rawContext;
        context.source.Cancel(context.cancelCode);
    }

    private class Context : IContext
    {
        internal readonly CancelTokenSource source;
        internal readonly int cancelCode;

        public Context(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        public ICancelToken CancelToken => source;
        public object? State => null;
        public object? Blackboard => null;
        public object? SharedProps => null;
    }

    #endregion

    #region code

    public int CancelCode => code;

    public bool IsCancelling() => code != 0;

    public int Reason() => ICancelToken.Reason(code);

    public int Degree() => ICancelToken.Degree(code);

    public bool IsInterruptible() => ICancelToken.IsInterruptible(code);

    public bool IsWithoutRemove() {
        return ICancelToken.IsWithoutRemove(code);
    }

    public void CheckCancel() {
        int code = this.code;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #endregion

    #region 监听器

    #region uni-accept

    public IRegistration ThenAccept(Action<ICancelToken> action, int options = 0) {
        return PushUniAccept(null, action, options);
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAccept(executor, action, options);
    }

    private IRegistration PushUniAccept(IExecutor? executor, Action<ICancelToken> action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniAccept.FireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniAccept(executor, options, this, action);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #region uni-accept-ctx

    public IRegistration ThenAccept(Action<ICancelToken, object> action, object? state, int options = 0) {
        return PushUniAcceptCtx(null, action, state, options);
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken, object> action, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAcceptCtx(executor, action, state, options);
    }

    private IRegistration PushUniAcceptCtx(IExecutor? executor, Action<ICancelToken, object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniAcceptCtx.FireNow(this, action, state);
            return TOMBSTONE;
        }
        Completion completion = new UniAcceptCtx(executor, options, this, action, state);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #region uni-run

    public IRegistration ThenRun(Action action, int options = 0) {
        return PushUniRun(null, action, options);
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRun(executor, action, options);
    }

    private IRegistration PushUniRun(IExecutor? executor, Action action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniRun.FireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniRun(executor, options, this, action);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #region uni-run-ctx

    public IRegistration ThenRun(Action<object> action, object? state, int options = 0) {
        return PushUniRunCtx(null, action, state, options);
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action<object> action, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRunCtx(executor, action, state, options);
    }

    private IRegistration PushUniRunCtx(IExecutor? executor, Action<object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniRunCtx.FireNow(this, action, state);
            return TOMBSTONE;
        }
        Completion completion = new UniRunCtx(executor, options, this, action, state);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #region uni-notify

    public IRegistration ThenNotify(ICancelTokenListener action, int options = 0) {
        return PushUniNotify(null, action, options);
    }

    public IRegistration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniNotify(executor, action, options);
    }

    private IRegistration PushUniNotify(IExecutor? executor, ICancelTokenListener action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniNotify.FireNow(this, action);
            return TOMBSTONE;
        }
        Completion completion = new UniNotify(executor, options, this, action);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #region uni-transfer

    public IRegistration ThenTransferTo(ICancelTokenSource child, int options = 0) {
        return PushUniTransfer(null, child, options);
    }

    public IRegistration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniTransfer(executor, child, options);
    }

    private IRegistration PushUniTransfer(IExecutor? executor, ICancelTokenSource action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelling() && executor == null) {
            UniTransferTo.FireNow(this, SYNC, action);
            return TOMBSTONE;
        }
        Completion completion = new UniTransferTo(executor, options, this, action);
        return PushCompletion(completion) ? completion : TOMBSTONE;
    }

    #endregion

    #endregion

    #region core

    /** 用于表示任务已申领权限 */
    private static readonly IExecutor CLAIMED = APromise.CLAIMED;
    private const int SYNC = APromise.SYNC;
    private const int ASYNC = APromise.ASYNC;
    private const int NESTED = APromise.NESTED;

    /// <summary>
    /// 栈顶回调被删除时尝试删除更多的节点
    /// </summary>
    /// <param name="expectedHead">当前的head</param>
    /// <returns>最新栈顶，可能是<see cref="TOMBSTONE"/></returns>
    private Completion? RemoveClosedNode(Completion expectedHead) {
        Completion? next = expectedHead.next;
        while (next != null && next.action == TOMBSTONE) {
            next = next.next;
        }
        Completion realHead = Interlocked.CompareExchange(ref this.stack, next, expectedHead);
        return realHead == expectedHead ? next : realHead;
    }

    private bool PushCompletion(Completion newHead) {
        if (IsCancelling()) {
            newHead.TryFire(SYNC);
            return false;
        }
        Completion expectedHead = stack;
        Completion realHead;
        while (expectedHead != TOMBSTONE) {
            // 处理延迟删除
            if (expectedHead != null && expectedHead.action == TOMBSTONE) {
                expectedHead = RemoveClosedNode(expectedHead);
                continue;
            }

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
        if (TaskOption.IsEnabled(options, TaskOption.STAGE_TRY_INLINE)
            && e is ISingleThreadExecutor eventLoop
            && eventLoop.InEventLoop()) {
            return true;
        }
        // 判断是否需要传递选项
        if (options != 0
            && !TaskOption.IsEnabled(options, TaskOption.STAGE_NON_TRANSITIVE)) {
            e.Execute(completion);
        } else {
            completion.Options = 0;
            e.Execute(completion);
        }
        return false;
    }

    #endregion

    private abstract class Completion : ITask, IRegistration
    {
        /** 非volatile，由栈顶的cas更新保证可见性 */
        internal Completion? next;

        protected IExecutor? executor;
        protected int options;
        protected CancelTokenSource source;
        /**
         * 用户回调
         * 1.通知和清理时置为<see cref="TOMBSTONE"/>
         * 2.子类在执行action之前需要调用<see cref="PopAction"/>竞争。
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

        /// <summary>
        /// 删除action -- 取消或通知时需要竞争删除action
        /// </summary>
        /// <returns>删除成功则返回action，否则返回null</returns>
        protected object? PopAction() {
            object? action = this.action;
            if (action == TOMBSTONE) { // 已被取消
                return null;
            }
            if (Interlocked.CompareExchange(ref this.action, TOMBSTONE, action) == action) {
                return action;
            }
            return null; // 竞争失败-被取消或通知
        }

        public void Dispose() {
            object? action = PopAction();
            if (action == null) {
                return;
            }
            if (this == source.stack) {
                source.RemoveClosedNode(this);
            }
            Clear();
        }

        /// <summary>
        /// 清理对象上的数据，help gc
        /// 注意：不可修改action的引用
        /// </summary>
        protected virtual void Clear() {
            executor = null!;
            source = null!;
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

    private class UniAccept : Completion
    {
        public UniAccept(IExecutor? executor, int options, CancelTokenSource source, Action<ICancelToken> action)
            : base(executor, options, source, action) {
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action<ICancelToken>)PopAction();
                if (action == null) {
                    return null;
                }
                action(source);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniAccept caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(CancelTokenSource source, Action<ICancelToken> action) {
            try {
                action(source);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniAccept caught an exception");
            }
        }
    }

    private class UniAcceptCtx : Completion
    {
        private object? state;

        public UniAcceptCtx(IExecutor? executor, int options, CancelTokenSource source,
                            Action<ICancelToken, object> action, object? state)
            : base(executor, options, source, action) {
            this.state = state;
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action<ICancelToken, object>)PopAction();
                if (action == null) {
                    return null;
                }
                action(source, state);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniAcceptCtx caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(CancelTokenSource source,
                                     Action<ICancelToken, object> action, object? state) {
            try {
                action(source, state);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniAcceptCtx caught an exception");
            }
        }
    }

    private class UniRun : Completion
    {
        public UniRun(IExecutor? executor, int options, CancelTokenSource source, Action action)
            : base(executor, options, source, action) {
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action)PopAction();
                if (action == null) {
                    return null;
                }
                action();
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniRun caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(CancelTokenSource source, Action action) {
            try {
                action();
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniRun caught an exception");
            }
        }
    }

    private class UniRunCtx : Completion
    {
        private object? state;

        public UniRunCtx(IExecutor? executor, int options, CancelTokenSource source,
                         Action<object> action, object? state)
            : base(executor, options, source, action) {
            this.state = state;
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action<object>)PopAction();
                if (action == null) {
                    return null;
                }
                action(state);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniRunCtx caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(CancelTokenSource source, Action<object> action, object? state) {
            try {
                action(state);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniRunCtx caught an exception");
            }
        }
    }

    private class UniNotify : Completion
    {
        public UniNotify(IExecutor? executor, int options, CancelTokenSource source,
                         ICancelTokenListener action)
            : base(executor, options, source, action) {
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (ICancelTokenListener)PopAction();
                if (action == null) {
                    return null;
                }
                action.OnCancelRequested(source);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniNotify caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(CancelTokenSource source, ICancelTokenListener action) {
            try {
                action.OnCancelRequested(source);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniNotify caught an exception");
            }
        }
    }

    private class UniTransferTo : Completion
    {
        public UniTransferTo(IExecutor? executor, int options, CancelTokenSource source,
                             ICancelTokenSource action)
            : base(executor, options, source, action) {
        }

        protected internal override CancelTokenSource? TryFire(int mode) {
            CancelTokenSource output;
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (ICancelTokenSource)PopAction();
                if (action == null) {
                    return null;
                }
                output = FireNow(source, mode, action);
            }
            catch (Exception ex) {
                output = null;
                FutureLogger.LogCause(ex, "UniNotify caught an exception");
            }
            // help gc
            Clear();
            return output;
        }

        internal static CancelTokenSource? FireNow(CancelTokenSource source, int mode,
                                                   ICancelTokenSource child) {
            if (child is not CancelTokenSource childSource) {
                child.Cancel(source.code);
                return null;
            }
            if (Interlocked.CompareExchange(ref childSource.code, source.code, 0) == 0) {
                if (mode < 0) { // 嵌套模式
                    return childSource;
                }
                PostComplete(childSource);
                return null;
            }
            return null;
        }
    }
}