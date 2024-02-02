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