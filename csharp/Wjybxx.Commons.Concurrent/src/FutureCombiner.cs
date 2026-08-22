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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons.Collections; // Unity下提供EnsureCapacity

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Future聚合器
/// </summary>
public sealed class FutureCombiner
{
    private List<IFuture> _futures;
    private FutureListener _listener;
    private IPromise<object>? _promise;

    public FutureCombiner(int expectedCount = 0) {
        _futures = new List<IFuture>(expectedCount);
        _listener = new FutureListener(_futures);
    }

    public FutureCombiner Add(IFuture future) {
        if (future == null) throw new ArgumentNullException(nameof(future));
        FutureListener listener = CheckFinish();
        _futures.Add(future);
        future.OnCompleted(invoker, listener);
        return this;
    }

    public FutureCombiner AddAll(params IFuture[] futures) {
        FutureListener listener = CheckFinish();
        _futures.EnsureCapacity(_futures.Count + futures.Length);
        foreach (IFuture future in futures) {
            if (future == null) {
                throw new ArgumentException("futures contains null element");
            }
            _futures.Add(future);
            future.OnCompleted(invoker, listener);
        }
        return this;
    }

    public FutureCombiner AddAll(IEnumerable<IFuture> futures) {
        FutureListener listener = CheckFinish();
        if (futures is ICollection<IFuture> coll) {
            _futures.EnsureCapacity(_futures.Count + coll.Count);
        }
        foreach (IFuture future in futures) {
            if (future == null) {
                throw new ArgumentException("futures contains null element");
            }
            _futures.Add(future);
            future.OnCompleted(invoker, listener);
        }
        return this;
    }

    /// <summary>
    /// 获取监听的future数量
    /// 注意：future计数是不去重的，一个future反复添加会反复计数
    /// </summary>
    public int FutureCount => _futures.Count;

    //
    /// <summary>
    /// 设置接收结果的Promise
    /// 如果在执行操作前没有指定Promise，将创建<see cref="Promise{T}"/>实例。
    /// </summary>
    /// <param name="promise">接收结果的Promise</param>
    /// <returns></returns>
    public FutureCombiner SetPromise(IPromise<object>? promise) {
        this._promise = promise;
        return this;
    }

    /// <summary>
    /// 重置状态，使得可以重新添加future和选择
    /// </summary>
    public void Clear() {
        _futures = new List<IFuture>();
        _listener = new FutureListener(_futures);
        _promise = null;
    }

    // region select

    /// <summary>
    /// 返回的promise在任意future进入完成状态时进入完成状态
    /// 返回的promise与首个完成future的结果相同（不准确）
    /// 注意：如果future数量为0，返回的promise将无法进入完成状态。
    /// </summary>
    /// <returns></returns>
    public IPromise<object> WhenAny() {
        return Finish(AggregateOptions.WhenAny());
    }

    /// <summary>
    /// 返回的promise在所有future进入完成状态时进入完成状态
    /// 存在失败的Future时，最终进入失败状态，并聚合所有Future的异常
    /// (无快速失败逻辑)
    /// </summary>
    /// <returns></returns>
    public IPromise<object> WhenAll() {
        return Finish(AggregateOptions.WhenAll());
    }

    /// <summary>
    /// 成功N个触发成功
    ///
    /// 如果触发失败，则聚合所有异常信息为<see cref="AggregateException"/>。
    /// <p>
    /// 1.如果require等于【0】，则必定会成功。
    /// 2.如果require大于监听的future数量，必定会失败。
    /// 3.如果require小于监听的future数量，当成功任务数达到期望时触发成功。
    /// </p>
    /// </summary>
    /// <param name="required">期望成成功的任务数</param>
    /// <param name="failFast">是否在不满足条件时立即失败</param>
    /// <returns></returns>
    public IPromise<object> Select(int required, bool failFast = true) {
        return Finish(AggregateOptions.SelectN(required, failFast));
    }

    /// <summary>
    /// 要求所有的future都成功时才进入成功状态；
    /// 任意任务失败，最终结果都表现为失败。
    /// </summary>
    /// <param name="failFast">是否在不满足条件时立即失败</param>
    /// <returns></returns>
    public IPromise<object> SelectAll(bool failFast = true) {
        return Finish(AggregateOptions.SelectN(FutureCount, failFast));
    }

    // region 内部实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FutureListener CheckFinish() {
        FutureListener listener = this._listener;
        if (listener == null) {
            throw new InvalidOperationException("Already finished");
        }
        return listener;
    }

    private IPromise<object> Finish(AggregateOptions options) {
        FutureListener listener = CheckFinish();
        this._listener = null!;

        IPromise<object> promise = this._promise;
        if (promise == null) {
            promise = new Promise<object>();
        }

        // 数据存储在Listener上有助于扩展
        // listener.futures = _futures;
        listener.options = options;
        listener.promise = promise;
        listener.CheckComplete();
        return promise;
    }

    /** 避免过多的闭包 */
    private static readonly Action<IFuture, object> invoker = (future, state) => {
        FutureListener listener = (FutureListener)state;
        listener.Accept(future);
    };

    private class FutureListener
    {
        private volatile int succeedCount;
        private volatile int failedCount;
        private volatile int doneCount;

        /** 虽然存在竞争，但重复赋值是安全的，通过promise发布到其它线程 */
        private volatile object? result;
        private volatile Exception? cause;

        /** 非volatile，其可见性由<see cref="promise"/>保证 */
        private readonly List<IFuture> futures;
        internal AggregateOptions options;
        internal volatile IPromise<object>? promise;

        public FutureListener(List<IFuture> futures) {
            this.futures = futures;
        }

        public void Accept(IFuture future) {
            if (future.IsFailedOrCancelled) {
                Accept(null, future.ExceptionNow(false));
            } else {
                Accept(future.ResultNow(), null);
            }
        }

        private void Accept(object? r, Exception? ex) {
            // 更新时先增加succeedCount，再增加doneCount；读取时先读取doneCount，再读取succeedCount，
            // 就可以保证succeedCount是比doneCount更新的值，才可以提前判断是否立即失败
            if (ex == null) {
                result = EncodeValue(r);
                Interlocked.Increment(ref succeedCount);
            } else {
                cause = ex;
                Interlocked.Increment(ref failedCount);
            }
            Interlocked.Increment(ref doneCount);

            IPromise<object> promise = this.promise;
            if (promise != null && !promise.IsCompleted && CheckComplete()) {
                // result = null; // 清理可能导致其它线程异常
                // cause = null;
            }
        }

        internal bool CheckComplete() {
            int doneCount = this.doneCount;
            int succeedCount = this.succeedCount;
            if (doneCount < succeedCount) { // 退出竞争，另一个线程来完成
                return false;
            }

            IPromise<object> promise = this.promise!;
            int futureCount = futures.Count;
            if (options.IsWhenAny) {
                if (doneCount == 0) {
                    return false;
                }
                if (succeedCount > 0) { // anyOf下尽量返回成功
                    return promise.TrySetResult(DecodeValue(result));
                } else {
                    return promise.TrySetException(cause);
                }
            }
            if (options.IsWhenAll) {
                if (doneCount < futureCount) {
                    return false;
                }
                Exception cause = this.cause;
                if (cause != null) {
                    cause = CreateAggregateException();
                    return promise.TrySetException(cause);
                }
                return promise.TrySetResult(null);
            }

            // 非快速失败模式需要等待所有任务完成
            if (!options.failFast && doneCount < futureCount) {
                return false;
            }
            // 包含了require小于等于0的情况
            int successRequire = options.required;
            if (succeedCount >= successRequire) {
                return promise.TrySetResult(null);
            }
            // 剩余的任务不足以达到成功，则立即失败；包含了require大于futureCount的情况
            if (succeedCount + (futureCount - doneCount) < successRequire) {
                Exception cause = this.cause;
                if (cause != null) {
                    cause = CreateAggregateException();
                } else {
                    string message = $"FailFast, done: {doneCount}/{futureCount}, succ: {succeedCount}/{successRequire}";
                    cause = new AggregateException(message); // 统一返回聚合异常
                }
                return promise.TrySetException(cause);
            }
            return false;
        }

        private Exception CreateAggregateException() {
            var exceptions = new List<Exception>(failedCount);
            int cancelled = 0 ;
            foreach (IFuture upstream in futures) {
                if (upstream.IsCancelled) {
                    cancelled++;
                }
                if (upstream.IsFailedOrCancelled) {
                    exceptions.Add(upstream.ExceptionNow(false));
                }
            }

            if (cancelled == exceptions.Count) {
                return exceptions[0]; // 理论上可能仍存在成功的任务
            }
            return new AggregateException(exceptions);
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