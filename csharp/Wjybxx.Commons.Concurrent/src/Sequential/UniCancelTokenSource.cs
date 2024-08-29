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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 单线程的取消令牌
///
/// <h3>实现说明</h3>
/// 1. 去除了{@link #code}等的volatile操作，变更为普通字段。
/// 2. 默认时间单位为毫秒。
/// 3.<see cref="TryInline"/>对executor的检测调整
/// </summary>
public class UniCancelTokenSource : ICancelTokenSource
{
    /** 取消码 -- 0表示未收到信号 */
    private int code;
    /** 监听器的首部 */
    private Completion? stack;

    /** 用于延迟执行取消 */
    private IUniScheduledExecutor? executor;

    public UniCancelTokenSource() {
    }

    public UniCancelTokenSource(int code)
        : this(null, code) {
    }

    public UniCancelTokenSource(IUniScheduledExecutor? executor, int code = 0) {
        this.executor = executor;
        if (code != 0) {
            this.code = CancelCodes.CheckCode(code);
        }
    }

    public IUniScheduledExecutor? Executor {
        get => executor;
        set => executor = value;
    }

    public bool CanBeCancelled => true;

    ICancelTokenSource ICancelTokenSource.NewInstance(bool copyCode) => NewInstance(copyCode);

    public UniCancelTokenSource NewInstance(bool copyCode = false) {
        return new UniCancelTokenSource(executor, copyCode ? code : 0);
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
        CancelAfter(cancelCode, TimeSpan.FromMilliseconds(millisecondsDelay), executor!);
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan) {
        CancelAfter(cancelCode, timeSpan, executor!);
    }

    public void CancelAfter(int cancelCode, TimeSpan timeSpan, IScheduledExecutorService delayer) {
        if (delayer == null) throw new ArgumentNullException(nameof(delayer));
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewAction(Canceller, new Context(this, cancelCode));
        builder.SetOnlyOnce(timeSpan.Ticks, new TimeSpan(1));
        delayer.Schedule(in builder);
    }

    private static void Canceller(IContext rawContext) {
        Context context = (Context)rawContext;
        context.source.Cancel(context.cancelCode);
    }

    private class Context : IContext
    {
        internal readonly UniCancelTokenSource source;
        internal readonly int cancelCode;

        public Context(UniCancelTokenSource source, int cancelCode) {
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

    public bool IsCancelling => code != 0;

    public int Reason => CancelCodes.GetReason(code);

    public int Degree => CancelCodes.GetDegree(code);

    public bool IsInterruptible => CancelCodes.IsInterruptible(code);

    public bool IsWithoutRemove => CancelCodes.IsWithoutRemove(code);

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
        if (IsCancelling && executor == null) {
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
        if (IsCancelling && executor == null) {
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
        if (IsCancelling && executor == null) {
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
        if (IsCancelling && executor == null) {
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
        if (IsCancelling && executor == null) {
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
        if (IsCancelling && executor == null) {
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
    private static readonly IExecutor CLAIMED = AbstractPromise.CLAIMED;
    private const int SYNC = AbstractPromise.SYNC;
    private const int ASYNC = AbstractPromise.ASYNC;
    private const int NESTED = AbstractPromise.NESTED;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int InternalCancel(int cancelCode) {
        Debug.Assert(cancelCode != 0);
        int preCode = this.code;
        if (preCode == 0) {
            this.code = cancelCode;
            return 0;
        }
        return preCode;
    }

    /** 删除node -- 修正指针 */
    private void RemoveNode(Completion node) {
        Completion curr = stack;
        if (curr == null || curr == TOMBSTONE) {
            return;
        }
        Completion prev = null;
        while (curr != null && curr != node) {
            prev = curr;
            curr = curr.next;
        }
        if (curr == null) {
            return;
        }
        if (prev == null) {
            stack = node.next;
        } else {
            prev.next = curr.next;
        }
    }

    private bool PushCompletion(Completion newHead) {
        if (IsCancelling) {
            newHead.TryFire(SYNC);
            return false;
        }
        // 单线程 - 不存在并发情况
        newHead.next = this.stack;
        this.stack = newHead;
        return true;
    }

    private static void PostComplete(UniCancelTokenSource source) {
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

    private static Completion? ClearListeners(UniCancelTokenSource source, Completion? onto) {
        Completion head = source.stack;
        if (head == TOMBSTONE) {
            return onto;
        }
        source.stack = TOMBSTONE;

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
        if (TaskOptions.IsEnabled(options, TaskOptions.STAGE_TRY_INLINE)
            && e is ISingleThreadExecutor eventLoop
            && eventLoop.InEventLoop()) {
            return true;
        }
        e.Execute(completion);
        return false;
    }

    #endregion

    private abstract class Completion : ITask, IRegistration
    {
        internal UniCancelTokenSource source;
        internal Completion? next;

        protected IExecutor? executor;
        protected int options;
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

        protected Completion(IExecutor? executor, int options, UniCancelTokenSource source, object action) {
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

        protected internal abstract UniCancelTokenSource? TryFire(int mode);

        protected bool Claim() {
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

        /// <summary>
        /// 删除action -- 取消或通知时需要竞争删除action
        /// </summary>
        /// <returns>删除成功则返回action，否则返回null</returns>
        protected object? PopAction() {
            object? action = this.action;
            if (action == TOMBSTONE) { // 已被取消
                return null;
            }
            this.action = TOMBSTONE;
            return action;
        }

        public void Dispose() {
            object? action = PopAction();
            if (action == null) {
                return;
            }
            source.RemoveNode(this);
            Clear();
        }

        /// <summary>
        /// 清理对象上的数据，help gc
        /// 注意：不可修改action的引用
        /// </summary>
        protected internal virtual void Clear() {
            source = null!;
            next = null;
            executor = null;
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

        protected internal override UniCancelTokenSource? TryFire(int mode) {
            throw new NotImplementedException();
        }
    }

    private class UniAccept : Completion
    {
        public UniAccept(IExecutor? executor, int options, UniCancelTokenSource source, Action<ICancelToken> action)
            : base(executor, options, source, action) {
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
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

        internal static void FireNow(UniCancelTokenSource source, Action<ICancelToken> action) {
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

        public UniAcceptCtx(IExecutor? executor, int options, UniCancelTokenSource source,
                            Action<ICancelToken, object> action, object? state)
            : base(executor, options, source, action) {
            this.state = state;
        }

        protected internal override void Clear() {
            base.Clear();
            state = null;
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action<ICancelToken, object>)PopAction();
                if (action == null) {
                    return null;
                }
                if (!AbstractUniPromise.IsCancelling(state, options)) {
                    action(source, state);
                }
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniAcceptCtx caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(UniCancelTokenSource source,
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
        public UniRun(IExecutor? executor, int options, UniCancelTokenSource source, Action action)
            : base(executor, options, source, action) {
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
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

        internal static void FireNow(UniCancelTokenSource source, Action action) {
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

        public UniRunCtx(IExecutor? executor, int options, UniCancelTokenSource source,
                         Action<object> action, object? state)
            : base(executor, options, source, action) {
            this.state = state;
        }

        protected internal override void Clear() {
            base.Clear();
            state = null;
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
            try {
                if (mode <= 0 && !Claim()) {
                    return null; // 下次执行
                }
                var action = (Action<object>)PopAction();
                if (action == null) {
                    return null;
                }
                if (!AbstractUniPromise.IsCancelling(state, options)) {
                    action(state);
                }
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "UniRunCtx caught an exception");
            }
            // help gc
            Clear();
            return null;
        }

        internal static void FireNow(UniCancelTokenSource source, Action<object> action, object? state) {
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
        public UniNotify(IExecutor? executor, int options, UniCancelTokenSource source,
                         ICancelTokenListener action)
            : base(executor, options, source, action) {
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
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

        internal static void FireNow(UniCancelTokenSource source, ICancelTokenListener action) {
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
        public UniTransferTo(IExecutor? executor, int options, UniCancelTokenSource source,
                             ICancelTokenSource action)
            : base(executor, options, source, action) {
        }

        protected internal override UniCancelTokenSource? TryFire(int mode) {
            UniCancelTokenSource output;
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

        internal static UniCancelTokenSource? FireNow(UniCancelTokenSource source, int mode,
                                                      ICancelTokenSource child) {
            if (child is not UniCancelTokenSource childSource) {
                child.Cancel(source.code);
                return null;
            }
            if (childSource.InternalCancel(source.code) == 0) {
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
}