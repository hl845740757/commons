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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于适配为<see cref="SynchronizationContext"/>
/// </summary>
public class ExecutorSynchronizationContext : SynchronizationContext
{
    private readonly IExecutor _executor;

    public ExecutorSynchronizationContext(IExecutor executor) {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public override void Post(SendOrPostCallback d, object? state) {
        // 不能随意内联，否则可能导致时序错误
        _executor.Execute(new PostCallbackWrapper(d, state));
    }

    protected class PostCallbackWrapper : ITask
    {
        private readonly SendOrPostCallback _callback;
        private readonly object? _state;

        public PostCallbackWrapper(SendOrPostCallback callback, object? state) {
            this._callback = callback;
            this._state = state;
        }

        public void Run() {
            _callback(_state);
        }

        public int Options => 0;
    }
}
}