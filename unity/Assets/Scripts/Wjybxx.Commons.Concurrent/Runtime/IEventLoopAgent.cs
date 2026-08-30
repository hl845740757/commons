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
/// 事件循环的内部代理
/// 1.如果缺少该组件，事件循环的模块将不会被Update。
/// 2.Agent对内，MainModule对外，都是为了避免继承扩展带来的局限性.
/// 3.由Agent决定监听器的管理和对事件的派发
///
/// Q：为什么监听器的注册也要委托给Agent处理？
/// A：允许Agent对派发的所有用户事件进行处理。
/// </summary>
public interface IEventLoopAgent<T> : IAgentEventHandler<T> where T : IAgentEvent
{
    /// <summary>
    /// 注入事件循环的引用
    /// </summary>
    /// <param name="eventLoop">事件循环</param>
    /// <param name="consumerId">事件循环的消费者id</param>
    void Inject(IEventLoop eventLoop, long consumerId) {
    }

    /// <summary>
    /// 用户模块请求注册事件监听器
    /// </summary>
    /// <param name="type">事件类型</param>
    /// <param name="handler">事件处理器</param>
    void Subscribe(int type, IAgentEventHandler<T> handler);

    /// <summary>
    /// 如果当前线程阻塞在中断也无法唤醒的地方，用户需要唤醒线程
    /// 该方法是多线程调用的，要小心并发问题
    /// </summary>
    void Wakeup() {
    }

    /// <summary>
    /// 执行协程调度
    ///
    /// 该接口用于业务事件循环实现自定义协程调度，并发库默认不调度该方法。
    /// </summary>
    /// <param name="phase">阶段</param>
    void ScheduleCoroutine(int phase) {

    }

    #region 事件循环

    //
    /// <summary>
    /// 当事件循环启动的时候将调用该方法，可以用于解决模块之间的特殊依赖
    /// 注意：该方法抛出任何异常，都将导致事件循环线程终止！启动期间提交任务时要小心死锁！
    /// </summary>
    void BeforeEventLoopStart() {
    }

    /** 在事件循环启动成功后调用 */
    void AfterEventLoopStart() {
    }

    /// <summary>
    /// 当事件循环等待较长时间或处理完一批事件之后都将调用该方法，以检查是否需要执行主循环。
    /// 事件循环会反复调用该方法，直到该方法返回false，以允许业务层补帧（实现为固定帧率循环）。
    /// 示例代码如下：
    /// <code>
    /// while(mainModule.checkMainLoop(threadTime)) {
    ///     update(modules)
    /// }
    /// </code>
    /// 1.该方法的调用时机和频率是不确定的，因此用户应该自行控制内部逻辑频率。
    /// 2.该方法建议实现为无副作用的，更新时间请在<c>BeforeMainLoop</c>执行
    /// </summary>
    /// <param name="threadTime">线程时间(单位与具体时间循环有关)，不建议依赖该值</param>
    /// <returns></returns>
    bool CheckMainLoop(long threadTime);

    /** 在每次开始主循环之前调用 */
    void BeforeMainLoop(long threadTime) {
    }

    /** 在每次主循环结束后调用 */
    void AfterMainLoop(long threadTime) {
    }

    /** 自定义Update -- 在主循环外调用，用于实现不同频率的其它Update */
    void CustomUpdate(long threadTime) {
    }

    /** 在停止所有Module前调用 */
    void BeforeEventLoopShutdown() {
    }

    /** 在EventLoop停止所有Module之后调用 */
    void AfterEventLoopShutdown() {
    }

    #endregion
}
}