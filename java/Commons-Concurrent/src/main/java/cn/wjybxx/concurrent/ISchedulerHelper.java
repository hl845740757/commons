/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.concurrent;

import java.util.concurrent.TimeUnit;

/**
 * 该接口用户不可调用，否则可能产生错误
 *
 * @author wjybxx
 * date - 2024/8/9
 */
public interface ISchedulerHelper {

    /**
     * 当前线程的时间
     * 1. 可以使用缓存的时间，也可以实时查询，只要不破坏任务的执行约定即可。
     * 2. 如果使用缓存时间，接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。也就是说，线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
     * 3. 多线程事件循环，需要支持其它线程查询。
     */
    long tickTime();

    /**
     * 事件循环是否进入了关闭状态
     * 1.Task在检测事件循环进入关闭状态后，将自动放弃提交任务
     */
    boolean isShutDown();

    /** 是否处于事件循环线程 */
    boolean inEventLoop();

    /**
     * 规格化：将指定时间转换为tick同单位的时间
     *
     * @param worldTime 要转换的时间
     * @param timeUnit  时间单位
     * @return 和tickTime同单位的事件
     */
    long normalize(long worldTime, TimeUnit timeUnit);

    /**
     * 反规格化：将tick同单位的时间，转换为目标单位的时间
     *
     * @param localTime 要转换的时间
     * @param timeUnit  目标时间单位
     * @return 目标单位的时间
     */
    long denormalize(long localTime, TimeUnit timeUnit);

    /**
     * 任务已出基础队列，请求执行调度
     * <p>
     * 1.一定从当前线程调用
     * 2.如果不能调度任务，则取消任务
     * 3.收到取消信号，或检测到EventLoop已开始关闭的情况下不会调用
     * <p>
     * 定时任务会检测关闭状态，以避免不必要的初始化
     */
    void doSchedule(ScheduledPromiseTask<?> futureTask);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题（取决于取消信号）
     * 2.由调度器决定是否立即进入取消状态（可控制Task进入完成状态的时机）。。
     * 3.未保持与JDK的兼容，参数不直接使用{@link ICancelToken}
     * <p>
     * 注意：当收到来自其它线程的取消信号时，要小心可见性问题，尤其是对象可能被复用的情况。
     */
    void onCancelRequested(ScheduledPromiseTask<?> futureTask, int cancelCode);

    /** 计算任务的触发时间 -- 允许修正 */
    default long triggerTime(long delay, TimeUnit timeUnit) {
        if (delay <= 0) return tickTime();
        return tickTime() + normalize(delay, timeUnit);
    }

    /** 计算任务的触发间隔 -- 允许修正，但必须大于0 */
    default long triggerPeriod(long period, TimeUnit timeUnit) {
        if (period <= 0) return 1;
        return normalize(period, timeUnit);
    }

    /** 计算任务的下次触发延迟 */
    default long getDelay(long triggerTime, TimeUnit timeUnit) {
        long delay = triggerTime - tickTime();
        if (delay <= 0) return 0;
        return denormalize(delay, timeUnit);
    }

}