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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 如果一个操作可能导致死锁状态将抛出该异常.
/// 通常是因为监听者和执行者在同一个线程，监听者尝试阻塞等待结果。
/// </summary>
public class BlockingOperationException : Exception
{
    public BlockingOperationException() {
    }

    protected BlockingOperationException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public BlockingOperationException(string? message) : base(message) {
    }

    public BlockingOperationException(string? message, Exception? innerException) : base(message, innerException) {
    }
}
}