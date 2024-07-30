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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 超时上下文
///
/// ps：使用结构体减少gc，使用时小心
/// </summary>
public struct TimeoutContext
{
    /** 剩余时间 */
    internal long timeLeft;
    /** 上次触发时间，用于固定延迟下计算deltaTime */
    internal long lastTriggerTime;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeLeft">剩余时间</param>
    /// <param name="timeCreate">创建时间</param>
    public TimeoutContext(long timeLeft, long timeCreate) {
        this.timeLeft = timeLeft;
        this.lastTriggerTime = timeCreate;
    }

    /// <summary>
    /// 在执行任务前更新状态
    /// </summary>
    /// <param name="realTriggerTime">真实触发时间 -- 真正被调度的时间</param>
    /// <param name="logicTriggerTime">逻辑触发时间（期望的调度时间）</param>
    /// <param name="isFixedRate">是否是fixedRate类型任务</param>
    public void BeforeCall(long realTriggerTime, long logicTriggerTime, bool isFixedRate) {
        if (isFixedRate) {
            timeLeft -= (logicTriggerTime - lastTriggerTime);
            lastTriggerTime = logicTriggerTime;
        } else {
            timeLeft -= (realTriggerTime - lastTriggerTime);
            lastTriggerTime = realTriggerTime;
        }
    }

    /// <summary>
    /// 是否已超时
    /// </summary>
    /// <returns></returns>
    public bool IsTimeout() => timeLeft <= 0;
}
}