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
///
/// 事件循环
/// 它是单线程的，它保证任务不会并发执行，且任务的执行顺序和提交顺序一致。
///
/// <h3>时序</h3>
/// 在<see cref="IEventLoopGroup"/>的基础上，我们提供这样的时序保证：
/// 1.如果 task1 的执行时间小于等于 task2 的执行时间，且 task1 先提交成功，则保证 task1 在 task2 之前执行。
/// 它可以表述为：不保证后提交的高优先级的任务能先执行。
/// 还可以表述为：消费者按照提交成功顺序执行是合法的。
/// （简单说，提高优先级是不保证的，但反向的优化——降低优先级，则是可以支持的）
/// 
/// 2.周期性任务的再提交 与 新任务的提交 之间不提供时序保证。
/// 它可以表述为：任务只有首次运行时是满足上面的时序的。
/// 如果你期望再次运行和新任务之间得到确定性时序，可以通过提交一个新任务代替自己实现。
/// （简单说，允许降低周期性任务的再执行优先级）
/// 
/// 3. schedule系列方法的{@code initialDelay}和{@code delay}为负时，将转换为0。
/// fixedRate除外，fixedRate期望的是逻辑时间，总逻辑时间应当是可以根据次数计算的，转0会导致错误，因此禁止负数输入。
/// 另外，fixedRate由于自身的特性，因此难以和非fixedRate类型的任务达成时序关系。
///
/// 
/// Q:为什么首次触发延迟小于0时可以转为0？
/// A:我们在上面提到，由于不保证后提交的任务能在先提交的任务之前执行，因此当多个任务都能运行时，按照提交顺序执行是合法的。
/// 因此，我们只要保证能按照提交顺序执行就是合法的，当所有的初始延迟都负转0时，所有后续提交的任务的优先级都小于等于当前任务，
/// 因此后续提交的任务必定在当前任务之后执行，也就是按照提交顺序执行，因此是合法的。
///
/// <h3>警告</h3>
/// 由于{@link EventLoop}都是单线程的，你需要避免死锁等问题。
/// 1. 如果两个{@link EventLoop}存在交互，且其中一个使用有界任务队列，则有可能导致死锁，或大量任务超时。
/// 2. 如果在{@link EventLoop}上执行阻塞或死循环操作，则可能导致死锁，或大量任务超时。
/// 3. 如果{@link EventLoop}支持自定义等待策略，要小心选择或实现，可能导致定时任务不能被及时执行。 
/// </summary>
public interface IEventLoop : IFixedEventLoopGroup, ISingleThreadExecutor
{
    /// <summary>
    /// 返回该EventLoop线程所在的线程组（管理该EventLoop的容器）。
    /// 如果没有父节点，返回null。
    /// </summary>
    IEventLoopGroup? Parent { get; }

    /// <summary>
    /// 事件循环的主模块，
    /// 主模块是事件循环的外部策略实现，用于暴露特殊的业务接口
    ///（Agent对内，MainModule对外，都是为了避免继承扩展带来的局限性）
    /// </summary>
    IEventLoopModule MainModule { get; }

    /// <summary>
    /// 唤醒线程
    /// 如果当前EventLoop线程陷入了阻塞状态，则将线程从阻塞中唤醒；
    /// 通常用于通知及时处理任务和响应关闭。
    /// </summary>
    void Wakeup();

    /// <summary>
    /// 启动事件循环
    /// 一般来说我们可以不显式启动事件循环，事件循环在接收到任务后，会自行启动；
    /// 但如果我们需要确保所有的EventLoop都处于正确的状态才对外服务时，则可以显式启动EventLoop。
    /// </summary>
    /// <returns>RunningFuture</returns>
    IFuture Start();

    /// <summary>
    /// future会在EventLoop成功启动的时候进入完成状态
    /// </summary>
    IFuture RunningFuture { get; }

    /// <summary>
    /// 事件循环的生命周期状态
    /// </summary>
    EventLoopState State { get; }

    #region 状态查询

    /// <summary>
    /// 事件循环是否处于运行中状态
    /// </summary>
    bool IsRunning => State == EventLoopState.Running;

    /// <summary>
    /// 当事件循环处于1阶段关闭状态，或更之后的状态时返回true
    /// </summary>
    bool IExecutorService.IsShuttingDown => State >= EventLoopState.ShuttingDown;

    /// <summary>
    /// 当事件循环处于2阶段关闭状态，或更之后的状态时返回true
    /// </summary>
    bool IExecutorService.IsShutdown => State >= EventLoopState.Shutdown;

    bool IExecutorService.IsTerminated => State >= EventLoopState.Terminated;

    #endregion

    /// <summary>
    /// 测试是否在事件循环线程内，如果不在事件循环线程内则抛出异常
    /// </summary>
    /// <exception cref="GuardedOperationException"></exception>
    void EnsureInEventLoop() {
        if (!InEventLoop()) {
            throw new GuardedOperationException();
        }
    }

    /// <summary>
    /// 测试是否在事件循环线程内，如果不在事件循环线程内则抛出异常
    /// </summary>
    /// <exception cref="GuardedOperationException"></exception>
    void EnsureInEventLoop(string method) {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (!InEventLoop()) {
            throw new GuardedOperationException("Calling " + method + " must in the EventLoop");
        }
    }
}

/// <summary>
/// 事件循环的生命周期标识
/// </summary>
public enum EventLoopState
{
    /// <summary>
    /// 已创建，但尚未启动
    /// </summary>
    Unstarted = 0,

    /// <summary>
    /// 启动中
    /// </summary>
    Starting = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    Running = 2,

    /// <summary>
    /// 收到关闭请求，正在关闭；在该阶段下，事件循环将仍尝试执行尚未执行的任务；
    /// </summary>
    ShuttingDown = 3,

    /// <summary>
    /// 二阶段关闭状态，终止前的清理工作；在该阶段下，事件循环将丢弃尚未执行的任务，以尽快响应关闭；
    /// </summary>
    Shutdown = 4,

    /// <summary>
    /// 终止
    /// </summary>
    Terminated = 5
}
}