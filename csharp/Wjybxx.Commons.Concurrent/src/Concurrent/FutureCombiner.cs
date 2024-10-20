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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Future聚合器
/// </summary>
public sealed class FutureCombiner
{
    private ChildListener childrenListener = new ChildListener();
    private IPromise<object>? aggregatePromise;
    private int futureCount;

    public FutureCombiner() {
    }

    public FutureCombiner Add(IFuture future) {
        if (future == null) throw new ArgumentNullException(nameof(future));
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Adding futures is not allowed after finished adding");
        }
        ++futureCount;
        future.OnCompleted(Invoker, childrenListener, 0);
        return this;
    }

    public FutureCombiner AddAll(IEnumerable<IFuture> futures) {
        foreach (IFuture future in futures) {
            Add(future);
        }
        return this;
    }

    /// <summary>
    /// 获取监听的future数量
    /// 注意：future计数是不去重的，一个future反复添加会反复计数
    /// </summary>
    public int FutureCount => futureCount;

    //
    /// <summary>
    /// 设置接收结果的Promise
    /// 如果在执行操作前没有指定Promise，将创建<see cref="Promise{T}"/>实例。
    /// </summary>
    /// <param name="aggregatePromise"></param>
    /// <returns></returns>
    public FutureCombiner SetAggregatePromise(IPromise<object> aggregatePromise) {
        this.aggregatePromise = aggregatePromise;
        return this;
    }

    /// <summary>
    /// 重置状态，使得可以重新添加future和选择
    /// </summary>
    public void Clear() {
        childrenListener = new ChildListener();
        aggregatePromise = null;
        futureCount = 0;
    }

    // region select

    /// <summary>
    /// 返回的promise在任意future进入完成状态时进入完成状态
    /// 返回的promise与首个完成future的结果相同（不准确）
    /// 注意：如果future数量为0，返回的promise将无法进入完成状态。
    /// </summary>
    /// <returns></returns>
    public IPromise<object> AnyOf() {
        return Finish(AggregateOptions.AnyOf());
    }

    /// <summary>
    /// 成功N个触发成功
    ///
    /// 如果触发失败，只随机记录一个Future的异常信息，而不记录所有的异常信息。
    /// <p>
    /// 1.如果require等于【0】，则必定会成功。
    /// 2.如果require大于监听的future数量，必定会失败。
    /// 3.如果require小于监听的future数量，当成功任务数达到期望时触发成功。
    /// </p>
    /// <p>
    /// 如果lazy为false，则满足成功/失败条件时立即触发完成；
    /// 如果lazy为true，则等待所有任务完成之后才触发成功或失败。
    /// </p>
    /// </summary>
    /// <param name="successRequire">期望成成功的任务数</param>
    /// <param name="failFast">是否在不满足条件时立即失败</param>
    /// <returns></returns>
    public IPromise<object> SelectN(int successRequire, bool failFast) {
        return Finish(AggregateOptions.SelectN(futureCount, successRequire, failFast));
    }

    /// <summary>
    /// 要求所有的future都成功时才进入成功状态；
    /// 任意任务失败，最终结果都表现为失败。
    /// </summary>
    /// <param name="failFast">是否在不满足条件时立即失败</param>
    /// <returns></returns>
    public IPromise<object> SelectAll(bool failFast = true) {
        return Finish(AggregateOptions.SelectAll(failFast));
    }

    // region 内部实现

    private IPromise<object> Finish(AggregateOptions options) {
        ChildListener childrenListener = this.childrenListener;
        if (childrenListener == null) {
            throw new IllegalStateException("Already finished");
        }
        this.childrenListener = null!;

        IPromise<object> aggregatePromise = this.aggregatePromise;
        if (aggregatePromise == null) {
            aggregatePromise = new Promise<object>();
        } else {
            this.aggregatePromise = null;
        }

        // 数据存储在ChildListener上有助于扩展
        childrenListener.futureCount = this.futureCount;
        childrenListener.options = options;
        childrenListener.aggregatePromise = aggregatePromise;
        childrenListener.CheckComplete();
        return aggregatePromise;
    }

    /** 避免过多的闭包 */
    private static readonly Action<IFuture, object> Invoker = (future, state) => {
        ChildListener childListener = (ChildListener)state;
        childListener.Accept(future);
    };

    private class ChildListener
    {
        private volatile int succeedCount;
        private volatile int doneCount;

        /** 非volatile，虽然存在竞争，但重复赋值是安全的，通过promise发布到其它线程 */
        private object? result;
        private Exception? cause;

        /** 非volatile，其可见性由{@link #aggregatePromise}保证 */
        internal int futureCount;
        internal AggregateOptions options;
        internal volatile IPromise<object>? aggregatePromise;

        public void Accept(IFuture future) {
            if (future.IsFailed) {
                Accept(null, future.ExceptionNow(false));
            } else {
                Accept(future.ResultNow(), null);
            }
        }

        void Accept(object? r, Exception? throwable) {
            // 我们先增加succeedCount，再增加doneCount，读取时先读取doneCount，再读取succeedCount，
            // 就可以保证succeedCount是比doneCount更新的值，才可以提前判断是否立即失败
            if (throwable == null) {
                result = EncodeValue(r);
                Interlocked.Increment(ref succeedCount);
            } else {
                cause = throwable;
            }
            Interlocked.Increment(ref doneCount);

            IPromise<object> aggregatePromise = this.aggregatePromise;
            if (aggregatePromise != null && !aggregatePromise.IsCompleted && CheckComplete()) {
                result = null;
                cause = null;
            }
        }

        internal bool CheckComplete() {
            // 字段的读取顺序不可以调整
            int doneCount = this.doneCount;
            int succeedCount = this.succeedCount;
            if (doneCount < succeedCount) { // 退出竞争，另一个线程来完成
                return false;
            }

            if (options.IsAnyOf) {
                if (futureCount == 0) { // anyOf不能完成，考虑打印log
                    return false;
                }
                if (doneCount == 0) {
                    return false;
                }
                if (result != null) { // anyOf下尽量返回成功
                    return aggregatePromise!.TrySetResult(DecodeValue(result));
                } else {
                    return aggregatePromise!.TrySetException(cause);
                }
            }

            // 懒模式需要等待所有任务完成
            if (!options.failFast && doneCount < futureCount) {
                return false;
            }
            // 包含了require小于等于0的情况
            int successRequire = options.IsSelectAll ? futureCount : options.successRequire;
            if (succeedCount >= successRequire) {
                return aggregatePromise!.TrySetResult(null);
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                if (cause == null) {
                    cause = TaskInsufficientException.Create(futureCount, doneCount, succeedCount, successRequire);
                }
                return aggregatePromise!.TrySetException(cause);
            }
            return false;
        }
    }

    private static readonly object NIL = new object();

    private static object EncodeValue(object? val) {
        return val == null ? NIL : val;
    }

    private static object? DecodeValue(object? r) {
        return r == NIL ? null : r;
    }

    // endregion
}
}