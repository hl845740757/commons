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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 取消令牌源由任务的创建者（发起者）持有，具备取消权限。
/// </summary>
public interface ICancelTokenSource : ICancelToken
{
    /// <summary>
    /// 将Token置为取消状态
    /// </summary>
    /// <param name="cancelCode">取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}</param>
    /// <exception cref="ArgumentException">如果code小于等于0；或reason部分为0</exception>
    /// <returns>Token的当前值；如果Token已被取消，则非0；如果Token尚未被取消，则返回0。</returns>
    int Cancel(int cancelCode = CancelCodes.REASON_DEFAULT);

    /// <summary>
    /// 在一段时间后发送取消命令
    /// </summary>
    /// <param name="cancelCode">取消码</param>
    /// <param name="millisecondsDelay">延迟时间(毫秒) -- 单线程版的话，真实单位取决于约定。</param>
    void CancelAfter(int cancelCode, long millisecondsDelay);

    /// <summary>
    /// 在一段时间后发送取消命令
    /// </summary>
    /// <param name="cancelCode">取消码</param>
    /// <param name="timeSpan">延迟时间</param>
    void CancelAfter(int cancelCode, TimeSpan timeSpan);

    /// <summary>
    /// 创建一个同类型实例。
    /// 1.原型对象，避免具体类型依赖。
    /// 2.默认情况下，其它上下文应当拷贝。
    /// </summary>
    /// <param name="copyCode">是否拷贝取消码</param>
    /// <returns>新的同类型实例</returns>
    ICancelTokenSource NewInstance(bool copyCode = false);
}