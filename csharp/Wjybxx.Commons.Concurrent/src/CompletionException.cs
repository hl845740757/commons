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
using System.Runtime.Serialization;

namespace Wjybxx.Commons;

/// <summary>
/// 该异常表示在计算的过程中出现异常
/// </summary>
public class CompletionException : Exception
{
    public CompletionException() {
    }

    protected CompletionException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public CompletionException(string? message) : base(message) {
    }

    public CompletionException(string? message, Exception? innerException) : base(message, innerException) {
    }
}