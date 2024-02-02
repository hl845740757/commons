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
using System.Threading;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// Promise不会实现两份（泛型和非泛型），那会导致大量的重复代码，有非常高的维护成本。
/// 在不需要结果的情况下，可以选择将泛型参数定义为byte或int，尽可能减少开销即可。
/// 
/// PS：重复编码不仅仅是指Promise，与Promise相关的各个体系都需要双份...
/// </summary>
/// <typeparam name="T"></typeparam>
public class Promise<T> : Promise, IPromise<T>, IFuture<T>
{
    private const int ST_PENDING = (int)TaskStatus.PENDING;
    private const int ST_COMPUTING = (int)TaskStatus.COMPUTING;
    private const int ST_SUCCESS = (int)TaskStatus.SUCCESS;
    private const int ST_FAILED = (int)TaskStatus.FAILED;
    private const int ST_CANCELLED = (int)TaskStatus.CANCELLED;

    /// <summary>
    /// 任务的执行状态。
    ///
    /// 为了避免对结果的装箱，我们将result和exception分离为两个字段；分为两个字段其实占用了更多的内存，但减少了GC管理的对象 —— 这在GC拉胯的虚拟机中还是有收益的。。。
    /// 由于不能对结果装箱，我们需要显式的state字段来记录状态，这导致我们不能原子的更新state和result，因此必然存在一个发布中状态。
    /// 我们通过【负数状态】表示正在发布中，这样既可以表明正在发布中，还可以表示即将进入的状态。
    /// </summary>
    private volatile int _state;
    /** 任务成功执行时的结果 -- 可见性由state保证 */
    private T _result;
    /** 任务执行失败时的结果 -- 可见性由state保证 */
    private Exception? _ex;

    private readonly IExecutor? _executor;

    public Promise(IExecutor? executor = null) {
        _executor = executor;
    }

    public static Promise<T> CompletedPromise(T result) {
    }

    public static Promise<T> FailedPromise(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
    }

    #region internal

    private bool InternalSetResult(T result) {
        // c#的CAS和java参数顺序相反...
        // 先测试Pending状态 -- 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势，暂不优化
        int preStatus = Interlocked.CompareExchange(ref _state, -ST_SUCCESS, ST_PENDING);
        if (preStatus == ST_PENDING) {
            _result = result;
            _state = ST_SUCCESS;
            return true;
        }
        // 任务可能处于Computing状态，重试
        if (preStatus == ST_COMPUTING) {
            preStatus = Interlocked.CompareExchange(ref _state, -ST_SUCCESS, ST_COMPUTING);
            if (preStatus == ST_COMPUTING) {
                _result = result;
                _state = ST_SUCCESS;
                return true;
            }
        }
        return false;
    }

    private bool InternalSetException(Exception exception) {
        int targetState = exception is OperationCanceledException ? ST_CANCELLED : ST_FAILED;
        // 先测试Pending状态
        int preStatus = Interlocked.CompareExchange(ref _state, -targetState, ST_PENDING);
        if (preStatus == ST_PENDING) {
            _ex = exception;
            _state = targetState;
            return true;
        }
        // 任务可能处于Computing状态，重试
        if (preStatus == ST_COMPUTING) {
            preStatus = Interlocked.CompareExchange(ref _state, -targetState, ST_COMPUTING);
            if (preStatus == ST_COMPUTING) {
                _ex = exception;
                _state = targetState;
                return true;
            }
        }
        return false;
    }

    private static CompletionException EncodeException(Exception ex) {
        if (ex is CompletionException ex2) {
            return ex2;
        }
        return new CompletionException(null, ex);
    }

    /** 获取当前状态（宽松），如果处于发布中状态，则返回即将进入的状态 */
    private int RelaxedState() {
        int state = _state;
        if (state < 0) {
            state *= -1;
        }
        return state;
    }

    /** 获取当前状态（严格），如果处于发布中状态，则等待目标线程发布完毕 */
    private int StrictState() {
        int state = _state;
        if (state < 0) {
            // busy spin -- 该过程通常很快，因此自旋等待即可
            while ((state = _state) < 0) {
            }
        }
        return state;
    }

    #endregion

    #region 上下文

    public IExecutor Executor => _executor;

    public IFuture<T> AsReadonly() => new ForwardFuture<T>(this);

    #endregion

    #region 状态查询

    /** 是否表示完成状态 */
    private static bool IsDone0(int state) {
        return state >= ST_SUCCESS;
    }

    private static bool IsFailedOrCancelled0(int state) {
        return state >= ST_FAILED;
    }

    public TaskStatus Status => (TaskStatus)RelaxedState();
    public bool IsPending => RelaxedState() == ST_PENDING;
    public bool IsComputing => RelaxedState() == ST_COMPUTING;
    public bool IsCancelled => RelaxedState() == ST_CANCELLED;
    public bool IsSucceeded => RelaxedState() == ST_SUCCESS;
    public bool IsFailed => RelaxedState() == ST_FAILED;

    public sealed override bool IsDone => RelaxedState() >= ST_SUCCESS;
    public bool IsFailedOrCancelled => RelaxedState() >= ST_FAILED;

    #endregion

    #region 状态更新

    public bool TrySetComputing() {
        int preState = Interlocked.CompareExchange(ref _state, ST_COMPUTING, ST_PENDING);
        return preState == ST_PENDING;
    }

    public TaskStatus TrySetComputing2() {
        int preState = Interlocked.CompareExchange(ref _state, ST_COMPUTING, ST_PENDING);
        if (preState < 0) {
            preState *= -1;
        }
        return (TaskStatus)preState;
    }

    public void SetComputing() {
        if (!TrySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    public bool TrySetResult(T result) {
        if (InternalSetResult(result)) {
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetResult(T result) {
        if (!TrySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetException(Exception cause) {
        if (cause == null) throw new ArgumentNullException(nameof(cause));
        if (InternalSetException(cause)) {
            FutureLogger.LogCause(cause);
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetException(Exception cause) {
        if (!TrySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetCancelled(int code = ICancelToken.REASON_DEFAULT) {
        if (InternalSetException(StacklessCancellationException.InstOf(code))) {
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetCancelled(int code = ICancelToken.REASON_DEFAULT) {
        if (!TrySetCancelled(code)) {
            throw new IllegalStateException("Already complete");
        }
    }

    #endregion

    #region 非阻塞结果查询

    public T ResultNow() {
        int state = StrictState();
        return state switch
        {
            ST_SUCCESS => _result,
            ST_FAILED => throw new IllegalStateException("Task completed with exception"),
            ST_CANCELLED => throw new IllegalStateException("Task was cancelled"),
            _ => throw new IllegalStateException("Task has not completed")
        };
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        int state = StrictState();
        return state switch
        {
            ST_FAILED => _ex!,
            ST_CANCELLED when throwIfCancelled => throw _ex!,
            ST_CANCELLED => _ex!,
            ST_SUCCESS => throw new IllegalStateException("Task completed with a result");
            _ => throw new IllegalStateException("Task has not completed")
        };
    }

    /** 上报future的执行结果 -- 取消意外的异常都将被包装为<see cref="CompletionException"/> */
    private T ReportJoin(int state) {
        Debug.Assert(state > 0);
        if (state == ST_SUCCESS) {
            return _result;
        }
        if (state == ST_CANCELLED) {
            throw _ex!;
        }
        throw new CompletionException(null, _ex);
    }

    #endregion

    #region 阻塞结果查询

    // virtual 以支持重写
    protected virtual void CheckDeadlock() {
        if (_executor is ISingleThreadExecutor se && se.InEventLoop()) {
            throw new BlockingOperationException();
        }
    }

    public T Get() {
        int state = StrictState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        Await();
        return ReportJoin(StrictState());
    }

    public T Join() {
        int state = StrictState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        AwaitUninterruptibly();
        return ReportJoin(StrictState());
    }

    private Awaiter? TryPushAwaiter() {
        Completion head = stack;
        if (head is Awaiter awaiter) {
            return awaiter; // 阻塞操作不多，而且通常集中在调用链的首尾
        }
        awaiter = new Awaiter(this);
        return PushCompletion(awaiter) ? awaiter : null;
    }

    public IFuture<T> Await() {
        if (IsDone) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.Await();
        }
        return this;
    }

    public IFuture<T> AwaitUninterruptibly() {
        if (IsDone) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.AwaitUninterruptibly();
        }
        return this;
    }

    public bool Await(TimeSpan timeout) {
        if (IsDone) {
            return true;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            return awaiter.Await(timeout);
        }
        return true;
    }

    public bool AwaitUninterruptibly(TimeSpan timeout) {
        if (IsDone) {
            return true;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            return awaiter.AwaitUninterruptibly(timeout);
        }
        return true;
    }

    #endregion

    #region async

    #endregion
}