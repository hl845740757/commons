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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : Promise<T>, IFutureTask<T>
{
    private Delegate _action;
    private int _options;

    public PromiseTask(IExecutor executor, Delegate action, int options = 0)
        : base(executor) {
        _action = action;
        _options = options;
    }

    public void Run() {
    }

    /// <summary>
    /// 任务的调度选项
    /// </summary>
    public int Options => _options;

    /// <summary>
    /// 任务关联的Future
    /// </summary>
    public IFuture<T> Future => this;
}