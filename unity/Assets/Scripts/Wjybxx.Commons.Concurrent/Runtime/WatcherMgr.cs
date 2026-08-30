#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Watcher管理器
/// 
/// 监听器用于拦截插入到任务队列中的事件，在接收到一个事件时，将判断是否存在Watcher，
/// 1.如果不存在Watcher，事件将被插入任务队列。
/// 2.如果存在Watcher，事件将调用<c>Watcher.Test</c>方法测试事件。
/// 2.1 如果不是Watcher等待的事件，事件将被插入任务队列。
/// 2.2 如果是Watcher等待的事件，将删除Watcher，然后调用<c>Watcher.OnEvent</c>方法处理事件 -- 即：Watcher是一次性的。
/// 3.管理器中的watcher可能是多个，测试时将逐个测试
/// 
/// 一些指导：
/// 1.监听器应该设定超时时间，不可无限阻塞，否则可能有死锁风险，或者总是超时失败 -- 如果任务队列是有界的。
/// 2.应当先watch再执行阻塞等操作，否则可能丢失信号。
/// 3.在不需要使用的时候及时取消watch -- 建议在try-finally块中执行。
/// 4.实现必须是线程安全的，因为事件的发布者通常是另一个线程 -- 通常可以通过<see cref="IFuture"/>实现跨线程数据传输。
/// 5.监听和取消监听都是低频操作，因此可以简单实现为<c>lock</c>写，<c>volatile</c>读。
/// 6.为不同的入口分配不同的WatcherMgr有助于分散测试，提高性能。
/// </summary>
[ThreadSafe]
public interface WatcherMgr<E>
{
    /// <summary>
    /// 监听队列中的事件，直到某一个事件发生。
    /// （该方法通常由当前线程调用）
    /// </summary>
    /// <param name="watcher"></param>
    void Watch(Watcher<E> watcher);

    /// <summary>
    /// 取消监听
    /// 该方法既可能是注册监听器的代码执行，也可能是提交事件的线程（watcher的一次性原理）
    /// 如果是监听者自身调用，则可以根据返回值检测到冲突，从而采取对应的行为，这时事件的生产者可能将调用<see cref="Watcher{E}.OnEvent"/>
    /// </summary>
    /// <param name="watcher">用于判断是否是当前watcher</param>
    /// <returns>如果参数为null，则返回false；如果watcher存在，则删除并返回true，否则返回false</returns>
    bool CancelWatch(Watcher<E> watcher);

    /// <summary>
    /// 测试是否是Watcher等待的事件
    /// </summary>
    /// <param name="evt"></param>
    /// <returns> 如果事件被watcher消费则返回true，否则返回false</returns>
    bool OnEvent(in E evt);
}

/** 实现时要小心线程安全问题 */
[ThreadSafe]
public interface Watcher<E>
{
    /// <summary>
    /// 该方法禁止抛出异常，否则可能导致严重错误（事件丢失），可能导致死锁
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    bool Test(in E evt);

    /// <summary>
    /// onEvent的最好是仅仅将数据传输到监听者线程并唤醒线程，不要执行复杂的逻辑
    /// 比如通过future传输数据，监听者在future上阻塞。
    /// </summary>
    /// <param name="evt"></param>
    void OnEvent(in E evt);
}
}