#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 我们不提供两个抽象，使用int代替void
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TaskResult<T>
{
    private readonly T _result;
    private readonly Exception? _ex;

    public TaskResult(T result, Exception? ex) {
        _result = result;
        _ex = ex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TaskResult<T>(T r) {
        return new TaskResult<T>(r, null);
    }

    /// <summary>
    /// 任务是否执行成功
    /// </summary>
    public bool IsSucceeded => _ex == null;

    /// <summary>
    /// 任务是否被取消
    /// </summary>
    public bool IsCancelled => _ex is OperationCanceledException;

    /// <summary>
    /// 任务是否执行失败
    /// </summary>
    public bool IsFailed => _ex != null && _ex is not OperationCanceledException;

    /// <summary>
    /// 获取任务的结果，只有成功的情况下可调用
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T Result {
        get {
            if (_ex != null) {
                throw new InvalidOperationException();
            }
            return _result;
        }
    }

    /// <summary>
    /// 获取任务关联的异常，成功的情况下返回null
    /// </summary>
    public Exception? Exception => _ex;
}
}