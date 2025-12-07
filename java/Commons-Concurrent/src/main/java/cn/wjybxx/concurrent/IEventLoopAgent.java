/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.base.annotation.Beta;

/**
 * 事件循环的内部代理
 * 1.如果缺少该组件，事件循环的模块将不会被Update。
 * 2.Agent对内，MainModule对外，都是为了避免继承扩展带来的局限性.
 * 3.由Agent决定监听器的管理和对事件的派发
 * <p>
 * Q：为什么监听器的注册也要委托给Agent处理？
 * A：允许Agent对派发的所有用户事件进行处理。
 *
 * @author wjybxx
 * date - 2023/11/17
 */
public interface IEventLoopAgent<T extends IAgentEvent> extends IAgentEventHandler<T> {

    /**
     * 注入事件循环的引用
     *
     * @param eventLoop  事件循环
     * @param consumerId 事件循环的消费者id
     */
    default void inject(IEventLoop eventLoop, @Beta long consumerId) {

    }

    /**
     * 用户模块请求注册事件监听器
     *
     * @param type    事件类型
     * @param handler 事件处理器
     */
    void subscribe(int type, IAgentEventHandler<? super T> handler);

    /**
     * 如果当前线程阻塞在中断也无法唤醒的地方，用户需要唤醒线程
     * 该方法是多线程调用的，要小心并发问题
     */
    default void wakeup() {

    }

    // region 事件循环

    /**
     * 当事件循环启动的时候将调用该方法，可以用于解决模块之间的特殊依赖
     * 注意：该方法抛出任何异常，都将导致事件循环线程终止！启动期间提交任务时要小心死锁！
     */
    default void beforeEventLoopStart() {

    }

    /** 在事件循环启动成功后调用 */
    default void afterEventLoopStart() {
    }

    /**
     * 当事件循环等待较长时间或处理完一批事件之后都将调用该方法，以检查是否需要执行主循环。
     * 事件循环会反复调用该方法，直到该方法返回false，以允许业务层补帧（实现为固定帧率循环）。
     * 示例代码如下：
     * <pre>{@code
     * while(mainModule.checkMainLoop(threadTime)) {
     *     update(modules)
     * }
     * }</pre>
     * 注意：
     * 1.该方法的调用时机和频率是不确定的，因此用户应该自行控制内部逻辑频率。
     * 2.该方法建议实现为无副作用的，更新时间请在{@link #beforeMainLoop(long)}执行
     *
     * @param threadTime 线程时间(单位与具体时间循环有关)，不建议依赖该值
     */
    boolean checkMainLoop(long threadTime);

    /** 在每次开始主循环之前调用 */
    default void beforeMainLoop(long threadTime) {

    }

    /** 在每次主循环结束后调用 */
    default void afterMainLoop(long threadTime) {

    }

    /** 自定义Update -- 在主循环外调用，用于实现不同频率的其它Update */
    default void customUpdate(long threadTime) {

    }

    /** 在停止所有Module前调用 */
    default void beforeEventLoopShutdown() {
    }

    /** 在EventLoop停止所有Module之后调用 */
    default void afterEventLoopShutdown() {

    }
    // endregion

}