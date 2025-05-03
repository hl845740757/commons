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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 被抑制的异常类型
/// (该枚举仅用于方便编码)
/// </summary>
[Flags]
public enum SuppressedTypes
{
    None = 0,
    /// <summary>
    /// 禁止取消异常抛出
    /// </summary>
    Cancellation = TaskOptions.SUPPRESS_CANCELLATION_THROW,
    /// <summary>
    /// 禁止失败异常抛出
    /// </summary>
    Error = TaskOptions.SUPPRESS_ERROR_THROW,
    /// <summary>
    /// 禁止全部异常抛出
    /// </summary>
    All = Cancellation | Error
}
}