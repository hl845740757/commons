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
using System.Runtime.Serialization;
using Wjybxx.Commons.Ex;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 该异常表示监听的任务数不足以到达成功条件
/// </summary>
public class TaskInsufficientException : Exception, NoLogRequiredException
{
    public TaskInsufficientException() {
    }

    protected TaskInsufficientException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public TaskInsufficientException(string? message) : base(message) {
    }

    public TaskInsufficientException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public static TaskInsufficientException Create(int futureCount, int doneCount, int succeedCount,
                                                   int successRequire) {
        string msg = $"futureCount: {futureCount}, doneCount: {doneCount}, succeedCount: {succeedCount}, successRequire: {successRequire}";
        return new TaskInsufficientException(msg);
    }
}