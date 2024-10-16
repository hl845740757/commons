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

namespace Wjybxx.Commons.Ex
{
/// <summary>
/// 包含错误码的异常
/// </summary>
public class ErrorCodeException : Exception, IErrorCodeException, NoLogRequiredException, ISerializable
{
    private readonly int errorCode;

    public ErrorCodeException(int errorCode) {
        this.errorCode = errorCode;
    }

    public ErrorCodeException(int errorCode, string? message) : base(message) {
        this.errorCode = errorCode;
    }

    /// <summary>
    /// 错误码
    /// </summary>
    public int ErrorCode => errorCode;

    #region seril

    protected ErrorCodeException(SerializationInfo info, StreamingContext context)
        : base(info, context) {
        this.errorCode = info.GetInt32("code");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context) {
        base.GetObjectData(info, context);
        info.AddValue("code", errorCode);
    }

    #endregion
}
}