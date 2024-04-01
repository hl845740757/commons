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

using System.Diagnostics;
using Wjybxx.Commons.Concurrent;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Sequential;

/// <summary>
/// 单线程版本定时任务的Promise
/// </summary>
/// <typeparam name="T"></typeparam>
public class UniScheduledPromise<T> : UniPromise<T>, IScheduledPromise<T>
{
#nullable disable
    private IScheduledFutureTask<T> _task;
#nullable enable

    public UniScheduledPromise(IExecutor? executor = null)
        : base(executor) {
    }

    public void SetTask(IScheduledFutureTask<T> task) {
        Debug.Assert(task.Future == this);
        this._task = task;
    }
}