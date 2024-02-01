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
#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class ForwardFuture<T> : IFuture<T>
{
    protected readonly IFuture<T> future;

    public ForwardFuture(IFuture<T> future) {
        this.future = future ?? throw new ArgumentNullException(nameof(future));
    }
    
    #region 转发

    public IExecutor? Executor => future.Executor;

    public IFuture<T> AsReadonly() {
        return future.AsReadonly();
    }

    public TaskStatus Status => future.Status;

    public bool IsPending => future.IsPending;

    public bool IsComputing => future.IsComputing;

    public bool IsDone => future.IsDone;

    public bool IsCancelled => future.IsCancelled;

    public bool IsSucceeded => future.IsSucceeded;

    public bool IsFailed => future.IsFailed;

    public bool IsFailedOrCancelled => future.IsFailedOrCancelled;

    public bool GetNow(out T result) {
        return future.GetNow(out result);
    }

    public T ResultNow() {
        return future.ResultNow();
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        return future.ExceptionNow(throwIfCancelled);
    }

    #endregion
}