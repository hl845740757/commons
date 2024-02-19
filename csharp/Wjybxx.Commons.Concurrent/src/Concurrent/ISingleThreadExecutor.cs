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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 
/// </summary>
public interface ISingleThreadExecutor : IExecutor
{
    /// <summary>
    /// 查询当前是否在EventLoop所属的线程
    ///
    /// 主要作用:
    /// 1.判断是否可访问线程封闭的数据。
    /// 2.防止死锁。
    ///
    /// 警告：如果用户基于该测试实现分支逻辑，则可能导致时序错误，eg：
    /// <code>
    /// 	if(eventLoop.inEventLoop()) {
    ///     	doSomething();
    ///     } else {
    ///         eventLoop.execute(() -> doSomething());
    ///     }
    /// </code>
    /// 假设现在有3个线程：A、B、C，它们进行了约定，线程A投递任务后，告诉线程B，线程B投递后告诉线程C，线程C再投递，以期望任务按照A、B、C的顺序处理。
    /// 在某个巧合下，线程C可能就是执行者线程，结果C的任务可能在A和B的任务之前被处理，从而破坏了外部约定的时序。
    /// 
    /// 该方法一定要慎用，它有时候是无害的，有时候则是有害的，因此必须想明白是否需要提供全局时序保证！
    /// </summary>
    /// <returns>如果在EventLoop所属的线程则返回true；否则返回false</returns>
    bool InEventLoop();

    /// <summary>
    /// 测试给定线程是否是EventLoop线程
    /// 注意：EventLoop接口约定是单线程的，不会并发执行提交的任务，但不约定整个生命周期都在同一个线程上，以允许在空闲的时候销毁线程。
    /// </summary>
    /// <param name="thread">要测试的线程</param>
    bool InEventLoop(Thread thread);
}