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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct TaskAwaitable
{
    private readonly Task _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    public TaskAwaitable(Task future, IExecutor executor, int options) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public Awaiter GetAwaiter() => new Awaiter(_future, _executor, _options);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly Action<Task, object> Invoker = (_, state) => ((Action)state).Invoke();

        private readonly Task _future;
        private readonly IExecutor _executor;
        private readonly int _options;

        internal Awaiter(Task future, IExecutor executor, int options) {
            _future = future;
            _executor = executor;
            _options = options;
        }

        // 1.IsCompleted
        // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
        public bool IsCompleted => _future.IsCompleted
                                   && TaskOption.IsEnabled(_options, TaskOption.STAGE_TRY_INLINE)
                                   && Executors.InEventLoop(_executor);

        // 2. GetResult
        // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() {
            _future.Wait();
            _future.GetAwaiter().GetResult();
        }

        // 3. OnCompleted
        /// <summary>
        /// 添加一个Future完成时的回调。
        /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
        /// </summary>
        /// <param name="continuation">回调任务</param>
        public void OnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _future.ContinueWith(Invoker, continuation, _executor.AsScheduler());
        }

        public void UnsafeOnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _future.ContinueWith(Invoker, continuation, _executor.AsScheduler());
        }
    }
}

/// <summary>
/// 用于绑定回调线程
/// 注意：不可手动获取<see cref="GetAwaiter"/>。
/// </summary>
public readonly struct TaskAwaitable<T>
{
    private readonly Task<T> _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    public TaskAwaitable(Task<T> future, IExecutor executor, int options) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _options = options;
    }

    public Awaiter GetAwaiter() => new Awaiter(_future, _executor, _options);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly Action<Task<T>, object> Invoker = (_, state) => ((Action)state).Invoke();

        private readonly Task<T> _future;
        private readonly IExecutor _executor;
        private readonly int _options;

        internal Awaiter(Task<T> future, IExecutor executor, int options) {
            _future = future;
            _executor = executor;
            _options = options;
        }

        // 1.IsCompleted
        // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
        public bool IsCompleted => _future.IsCompleted
                                   && TaskOption.IsEnabled(_options, TaskOption.STAGE_TRY_INLINE)
                                   && Executors.InEventLoop(_executor);

        // 2. GetResult
        // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult() {
            _future.Wait();
            return _future.Result;
        }

        // 3. OnCompleted
        /// <summary>
        /// 添加一个Future完成时的回调。
        /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
        /// </summary>
        /// <param name="continuation">回调任务</param>
        public void OnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _future.ContinueWith(Invoker, continuation, _executor.AsScheduler());
        }

        public void UnsafeOnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _future.ContinueWith(Invoker, continuation, _executor.AsScheduler());
        }
    }
}