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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 取消令牌监听器。
///
/// ps：该接口用于特殊需求时减少闭包。
/// </summary>
public interface ICancelTokenListener
{
    /// <summary>
    /// 该方法在取消令牌收到取消信号时执行
    /// </summary>
    /// <param name="cancelToken">收到取消信号的令牌</param>
    void OnCancelRequested(ICancelToken cancelToken);
}