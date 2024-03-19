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
using System.Threading.Tasks;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.UniTask;

/// <summary>
/// 该类解决两个问题：
/// 1. 解决泛型类的常量不共享问题。
/// 2. 提取公共代码（监听器管理）。
///
/// 注意：用户不应该使用该类 - 由于C#禁止超类的访问权限小于子类，该类只能定义为Public...
/// </summary>
public abstract class AbstractUniPromise
{
    /// <summary>
    /// 当前对象上的所有监听器，使用栈方式存储
    /// 如果{@code stack}为{@link #TOMBSTONE}，表明当前Future已完成，且正在进行通知，或已通知完毕。
    /// </summary>
    protected volatile Completion? stack;

    /// <summary>
    /// 是否处于宽松完成状态-结果已可获取，或即将可用。
    /// 处于发布结果中也可返回true。
    /// </summary>
    protected abstract bool IsRelaxedCompleted { get; }

    /// <summary>
    /// 是否已严格完成-结果已可获取.
    /// 如果存在中间状态，则需要返回false。
    /// </summary>
    protected abstract bool IsStrictlyCompleted { get; }

    #region state

    /** 表示任务已进入执行阶段 */
    internal static readonly object EX_COMPUTING = new object();
    /** 表示任务已成功完成，但正在发布执行结果 */
    private static readonly object EX_PUBLISHING = new object();
    /** 表示任务已成功完成，且结果已可见 */
    internal static readonly object EX_SUCCESS = new object();

    #endregion

    #region notify

    // Modes for Completion.tryFire. Signedness matters.
    /**
     * 同步调用模式，表示压栈过程中发现{@code Future}已进入完成状态，从而调用{@link Completion#tryFire(int)}。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(Promise)}。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    internal const int SYNC = 0;
    /**
     * 异步调用模式，表示提交到{@link Executor}之后调用{@link Completion#tryFire(int)}
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，则直接触发目标{@code Future}的完成事件，即调用{@link #postComplete(Promise)}。
     * 2. 在该模式，表示已获得执行权限，可立即执行。
     */
    internal const int ASYNC = 1;
    /**
     * 嵌套调用模式，表示由{@link #postComplete(Promise)}中触发调用。
     * 1. 如果在该模式下使下一个{@code Future}进入完成状态，不触发目标{@code Future}的完成事件，而是返回目标{@code Future}，由当前{@code Future}代为推送。
     * 2. 在该模式，在执行前，可能需要抢占执行权限。
     */
    internal const int NESTED = -1;

    /** 用于表示任务已申领权限 */
    internal static readonly IExecutor CLAIMED = new MockExecutor();

    private class MockExecutor : IExecutor
    {
        public SynchronizationContext AsSyncContext() {
            throw new NotImplementedException();
        }

        public TaskScheduler AsScheduler() {
            throw new NotImplementedException();
        }

        public void Execute(ITask task) {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 添加一个回调
    /// </summary>
    /// <param name="newHead"></param>
    /// <returns>压栈成功则返回true，否则返回false</returns>
    protected bool PushCompletion(Completion newHead) {
        if (IsStrictlyCompleted) {
            newHead.TryFire(SYNC);
            return false;
        }
        // 单线程 - 不存在并发情况
        newHead.next = this.stack;
        this.stack = newHead;
        return true;
    }

    /// <summary>
    /// 推送Future完成事件
    /// </summary>
    /// <param name="future"></param>
    protected static void PostComplete(AbstractUniPromise future) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = ClearListeners(future, next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                curr.next = null; // help gc

                future = curr.TryFire(NESTED);
                if (future != null) {
                    goto outer;
                }
            }
            break;
        }
    }

    /// <summary>
    /// 清空当前{@code Future}上的监听器，并将当前{@code Future}上的监听器逆序方式插入到{@code onto}前面。
    /// 
    /// Q: 这步操作是要干什么？
    /// A: Future的监听器构成了一棵树，在不进行优化的情况下，遍历监听器是一个【前序遍历】过程，这会产生很深的方法栈，从而影响性能。
    /// 该操作将子节点的监听器提升为当前节点的兄弟节点(插在前方)，从而将树形遍历优化为【线性遍历】，从而降低了栈深度，提高了性能。
    ///
    /// PS：这将导致无法通过Future删除回调 -- 只能通过取消令牌取消执行。
    /// </summary>
    private static Completion? ClearListeners(AbstractUniPromise promise, Completion? onto) {
        // 我们需要进行三件事
        // 1. 原子方式将当前Listeners赋值为TOMBSTONE，因为pushCompletion添加的监听器的可见性是由CAS提供的。
        // 2. 将当前栈内元素逆序，因为即使在接口层进行了说明（不提供监听器执行时序保证），但仍然有人依赖于监听器的执行时序(期望先添加的先执行)
        // 3. 将逆序后的元素插入到'onto'前面，即插入到原本要被通知的下一个监听器的前面
        Completion head = promise.stack;
        if (head == TOMBSTONE) {
            return onto;
        }
        promise.stack = TOMBSTONE;

        Completion ontoHead = onto;
        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead;
        }
        return ontoHead;
    }

    #endregion

    #region completion

    /// <summary>
    /// Completion表示一个回调任务
    /// </summary>
    protected abstract class Completion : ITask
    {
        /** 非volatile，通过{@link Promise#stack}的原子更新来保证可见性 */
        internal Completion? next;

        /// <summary>
        /// 任务在Executor执行
        /// </summary>
        public void Run() {
            TryFire(ASYNC);
        }

        /// <summary>
        /// 任务的调度选项 - 由子类提供。
        /// </summary>
        public abstract int Options { get; set; }

        /// <summary>
        /// 当依赖的某个{@code Future}进入完成状态时，该方法会被调用。
        /// 如果tryFire使得另一个{@code Future}进入完成状态，分两种情况：
        /// 1. mode指示不要调用{@link #postComplete(Promise)}方法时，则返回新进入完成状态的{@code Future}。
        /// 2. mode指示可以调用{@link #postComplete(Promise)}方法时，则直接推送其进入完成状态的事件。
        /// </summary>
        /// <param name="mode"></param>
        protected internal abstract AbstractUniPromise? TryFire(int mode);
    }

    /// <summary>
    /// 该实例表示stack已被清理
    /// </summary>
    private static readonly Completion TOMBSTONE = new MockCompletion();

    private class MockCompletion : Completion
    {
        public override int Options {
            get => 0;
            set => throw new AssertionError();
        }

        protected internal override AbstractUniPromise? TryFire(int mode) {
            throw new NotImplementedException();
        }
    }

    #endregion
}