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
public interface IScheduledHelper
{
    /// <summary>
    /// 当前线程的时间
    /// 1. 可以使用缓存的时间，也可以实时查询，只要不破坏任务的执行约定即可。
    /// 2. 如果使用缓存时间，接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。也就是说，线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
    /// 3. 多线程事件循环，需要支持其它线程查询。
    /// </summary>
    /// <returns></returns>
    long TickTime { get; }

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
    void Reschedule(IScheduledFutureTask futureTask);

    /// <summary>
    /// 任务不可继续触发 -- 任务当前已出队列
    /// 1.回调给用户，让用户决定是否清理和缓存。
    /// 2.与<see cref="Reschedule"/>成对
    /// </summary>
    /// <param name="futureTask"></param>
    void OnCompleted(IScheduledFutureTask futureTask);

    /// <summary>
    /// 收到用户的取消请求
    /// 1.可能从其它线程调用，需考虑线程安全问题（取决于取消信号）
    /// 2.Task关联的future在调用方法前已进入取消状态，用户处理后续逻辑。
    /// </summary>
    /// <param name="futureTask"></param>
    /// <param name="cancelCode"></param>
    void OnCancelRequested(IScheduledFutureTask futureTask, int cancelCode);

    /** 计算任务的触发时间 -- 允许修正 */
    long TriggerTime(long delay, TimeSpan timeUnit) {
        if (delay <= 0) return TickTime;
        return TickTime + Normalize(delay, timeUnit);
    }

    /** 计算任务的触发间隔 -- 允许修正，但必须大于0 */
    long TriggerPeriod(long period, TimeSpan timeUnit) {
        if (period <= 0) return 1;
        return Normalize(period, timeUnit);
    }

    /** 计算任务的下次触发延迟 */
    long GetDelay(long triggerTime, TimeSpan timeUnit) {
        long delay = triggerTime - TickTime;
        if (delay <= 0) return 0;
        return Denormalize(delay, timeUnit);
    }
}
}