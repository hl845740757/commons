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

import cn.wjybxx.base.fx.ComponentId;
import cn.wjybxx.base.fx.IComponent;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.List;

/**
 * 事件循环
 * 它是单线程的，它保证任务不会并发执行，且任务的执行顺序和提交顺序一致。
 *
 * <h2>时序</h2>
 * 在{@link IEventLoopGroup}的基础上，我们提供这样的时序保证：<br>
 * 1.如果 task1 的执行时间小于等于 task2 的执行时间，且 task1 先提交成功，则保证 task1 在 task2 之前执行。<br>
 * 它可以表述为：不保证后提交的高优先级的任务能先执行。<br>
 * 还可以表述为：消费者按照提交成功顺序执行是合法的。<br>
 * （简单说，提高优先级是不保证的，但反向的优化——降低优先级，则是可以支持的）
 * <p>
 * 2.周期性任务的再提交 与 新任务的提交 之间不提供时序保证。<br>
 * 它可以表述为：任务只有首次运行时是满足上面的时序的。<br>
 * 如果你期望再次运行和新任务之间得到确定性时序，可以通过提交一个新任务代替自己实现。<br>
 * （简单说，允许降低周期性任务的再执行优先级）
 * <p>
 * 3. schedule系列方法的{@code initialDelay}和{@code delay}为负时，将转换为0。
 * fixedRate除外，fixedRate期望的是逻辑时间，总逻辑时间应当是可以根据次数计算的，转0会导致错误，因此禁止负数输入。
 * 另外，fixedRate由于自身的特性，因此难以和非fixedRate类型的任务达成时序关系。
 *
 * <p>
 * Q:为什么首次触发延迟小于0时可以转为0？
 * A:我们在上面提到，由于不保证后提交的任务能在先提交的任务之前执行，因此当多个任务都能运行时，按照提交顺序执行是合法的。<br>
 * 因此，我们只要保证能按照提交顺序执行就是合法的，当所有的初始延迟都负转0时，所有后续提交的任务的优先级都小于等于当前任务，
 * 因此后续提交的任务必定在当前任务之后执行，也就是按照提交顺序执行，因此是合法的。
 *
 * <h3>组件模式</h3>
 * EventLoop虽然实现了组件模式，但运行时禁止增删组件；EventLoop应当通过Builder构建。
 *
 * <h3>警告</h3>
 * 由于{@link IEventLoop}都是单线程的，你需要避免死锁等问题。<br>
 * 1. 如果两个{@link IEventLoop}存在交互，且其中一个使用有界任务队列，则有可能导致死锁，或大量任务超时。<br>
 * 2. 如果在{@link IEventLoop}上执行阻塞或死循环操作，则可能导致死锁，或大量任务超时。<br>
 * 3. 如果{@link IEventLoop}支持自定义等待策略，要小心选择或实现，可能导致定时任务不能被及时执行。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface IEventLoop extends IEventLoopGroup, SingleThreadExecutor {

    /**
     * 返回该EventLoop线程所在的线程组（管理该EventLoop的容器）。
     * 如果没有父节点，返回null。
     */
    @Nullable
    IEventLoopGroup parent();

    /**
     * 唤醒线程
     * 如果当前{@link IEventLoop}线程陷入了阻塞状态，则将线程从阻塞中唤醒；通常用于通知线程及时处理任务和响应关闭。
     * 如果线程已停止，则该方法不产生影响
     */
    void wakeup();

    /**
     * 主动启动EventLoop
     * 一般而言，我们可以不主动启动EventLoop，在提交任务时会自动启动EventLoop，但如果我们需要确保EventLoop处于正确的状态才能对外提供服务时，则可以主动启动时EventLoop。
     * 另外，通过提交任务启动EventLoop，是无法根据任务的执行结果来判断启动是否成功的。
     *
     * @return {@link #runningFuture()}
     */
    IFuture<?> start();

    /**
     * 等待线程进入运行状态的future
     * future会在EventLoop成功启动的时候进入完成状态
     * <p>
     * 1.如果EventLoop启动失败，则Future进入失败完成状态
     * 2.如果EventLoop未启动直接关闭，则Future进入失败完成状态
     * 3.EventLoop关闭时，Future保持之前的结果
     */
    IFuture<?> runningFuture();

    /**
     * 当前线程的时间
     * 1.可以使用缓存的时间，也可以实时查询，只要不破坏任务的执行约定即可。
     * 2.多线程事件循环，需要支持其它线程查询。
     */
    long tickTime();

    /** @return EventLoop的当前状态 */
    EventLoopState state();

    /** 是否处于运行状态 */
    boolean isRunning();

    // region 组件模式

    /** 获取所有组件 */
    List<? extends IEventLoopModule> getComponents();

    /** 获取所有组件 */
    void getComponents(List<IEventLoopModule> outList);

    /** 当前组件数量 */
    int getComponentCount();

    /** 获取指定id的组件 */
    <T> T getComponent(ComponentId<T> cid);

    /** 是否包含指定组件 */
    boolean containsComponent(IComponent comp);
    // endregion
}