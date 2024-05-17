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
#pragma warning disable CS1591

namespace Wjybxx.Commons.Ex;

/// <summary>
/// 数字格式异常
/// </summary>
public class NumberFormatException : ArgumentException
{
    public NumberFormatException() {
    }

    protected NumberFormatException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public NumberFormatException(string? message) : base(message) {
    }

    public NumberFormatException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public NumberFormatException(string? message, string? paramName) : base(message, paramName) {
    }

    public NumberFormatException(string? message, string? paramName, Exception? innerException) : base(message, paramName, innerException) {
    }
}