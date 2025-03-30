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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该接口用户不可调用，否则可能产生错误
/// </summary>
public interface ISchedulerHelper : ICancelTokenListener
{
    /// <summary>
    /// 当前线程的时间
    /// <see cref="IEventLoop.TickTime"/>
    /// </summary>
    /// <returns></returns>
    long TickTime { get; }

    /// <summary>
    /// 事件循环是否进入了关闭状态
    /// 1.Task在检测事件循环进入关闭状态后，将自动放弃提交任务
    /// </summary>
    bool IsShutdown { get; }

    /// <summary>
    /// 查询当前是否在EventLoop所属的线程
    /// </summary>
    /// <returns></returns>
    bool InEventLoop();

    /// <summary>
    /// 规格化：将指定时间转换为tick同单位的时间
    /// (c#可根据tick数归一化)
    /// </summary>
    /// <param name="worldTime">要转换的时间</param>
    /// <param name="timeUnit">时间单位</param>
    /// <returns>和tickTime同单位的事件</returns>
    long Normalize(long worldTime, TimeSpan timeUnit);

    /// <summary>
    /// 反规格化：将tick同单位的时间，转换为目标单位的时间
    /// </summary>
    /// <param name="localTime">要转换的时间</param>
    /// <param name="timeUnit">目标时间单位</param>
    /// <returns>目标单位的时间</returns>
    long Denormalize(long localTime, TimeSpan timeUnit);

    /// <summary>
    /// 请求将当前任务重新压入队列 -- 任务当前已出队列
    /// 1.一定从当前线程调用
    /// 2.如果无法继续调度任务，则取消任务
    /// </summary>
    /// <param name="futureTask"></param>
    void DoSchedule(IScheduledFutureTask futureTask);

    /** 计算任务的触发时间 -- 允许修正 */
    long TriggerTime(long delay, TimeSpan timeUnit) {
        if (delay <= 0) return TickTime;
        if (timeUnit.Ticks < 1) throw new ArgumentException("timeUnit.Ticks < 1");
        return TickTime + Normalize(delay, timeUnit);
    }

    /** 计算任务的触发间隔 -- 允许修正，但必须大于0 */
    long TriggerPeriod(long period, TimeSpan timeUnit) {
        if (period <= 0) return 1;
        if (timeUnit.Ticks < 1) throw new ArgumentException("timeUnit.Ticks < 1");
        return Normalize(period, timeUnit);
    }

    /** 计算任务的下次触发延迟 */
    long GetDelay(long triggerTime, TimeSpan timeUnit) {
        long delay = triggerTime - TickTime;
        if (delay <= 0) return 0;
        if (timeUnit.Ticks < 1) throw new ArgumentException("timeUnit.Ticks < 1");
        return Denormalize(delay, timeUnit);
    }
}
}