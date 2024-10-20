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

using System;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 表示提交的任务被<see cref="IExecutor"/>拒绝。
/// 通常是因为目标Executor已开始关闭，或队列已满。
/// </summary>
public class RejectedExecutionException : Exception
{
    public RejectedExecutionException() {
    }

    public RejectedExecutionException(string? message) : base(message) {
    }

    public RejectedExecutionException(string? message, Exception? innerException) : base(message, innerException) {
    }
}
}