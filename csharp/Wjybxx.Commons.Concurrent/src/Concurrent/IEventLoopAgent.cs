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

using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 事件循环的内部代理
/// </summary>
public interface IEventLoopAgent<TEvent> where TEvent : IAgentEvent
{
    /// <summary>
    /// 注入EventLoop实例。
    ///
    /// 注意：此时EventLoop可能尚未完全初始化，建议只是单纯保存引用！
    /// </summary>
    /// <param name="eventLoop">事件循环</param>
    void Inject(IEventLoop eventLoop);

    /// <summary>
    /// 事件循环线程启动的时候
    /// 注意：该方法抛出任何异常都将导致事件循环线程终止！启动期间提交任务时要小心死锁。
    /// </summary>
    void OnStart();

    /// <summary>
    /// 收到一个用户自定义事件或任务
    /// ps：由于事件可能是结构体类型，因此使用ref。
    /// </summary>
    /// <param name="sequence">事件序号，如果是Disruptor类事件循环则有值</param>
    /// <param name="evt">事件</param>
    void OnEvent(long sequence, ref TEvent evt);

    /// <summary>
    /// 当事件循环等待较长时间或处理完一批事件之后都将调用该方法
    /// 注意：该方法的调用时机和频率是不确定的，因此用户应该自行控制内部逻辑频率。
    /// </summary>
    void Update();

    /// <summary>
    ///  如果当前线程阻塞在中断也无法唤醒的地方，用户需要唤醒线程
    /// 该方法是多线程调用的，要小心并发问题
    /// </summary>
    void Wakeup() {
    }

    /// <summary>
    /// 当事件循环退出时将调用该方法
    /// 退出前进行必要的清理，释放系统资源。
    /// 注意：此时EventLoop已清理<see cref="SynchronizationContext"/>，因此Shutdown钩子中使用await要小心线程问题。
    /// </summary>
    void OnShutdown();
}
}