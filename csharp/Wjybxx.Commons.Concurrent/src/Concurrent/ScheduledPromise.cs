﻿#region LICENSE

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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

public class ScheduledPromise<T> : Promise<T>, IScheduledPromise<T>
{
    private IScheduledFutureTask<T> _task;

    public ScheduledPromise(IExecutor? executor = null, IContext? context = null)
        : base(executor, context) {
    }

    public void SetTask(IScheduledFutureTask<T> task) {
        Debug.Assert(task.Future == this);
        this._task = task;
    }
}