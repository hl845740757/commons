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

using System;
using System.Threading;
using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该工具类主要用于暴露特殊接口给其它程序集
/// </summary>
public static class EventLoopUtil
{
    #region module

    /// <summary>
    /// 设置Module的状态
    /// </summary>
    public static void SetStatus(EventLoopModule module, ComponentStatus status) {
        module.SetStatus(status);
    }

    /// <summary>
    /// 设置Module绑定的事件循环，
    /// 会同时调用模块的OnReady方法。
    /// </summary>
    public static void SetEventLoop(IEventLoop eventLoop, EventLoopModule module) {
        module.SetEventLoop(eventLoop);
    }

    /// <summary>
    /// 调用模块的Start方法
    /// </summary>
    /// <param name="module"></param>
    public static Exception? InvokeStart(EventLoopModule module) {
        return module.InvokeStart();
    }

    /// <summary>
    /// 调用模块的Stop方法
    /// </summary>
    public static Exception? InvokeStop(EventLoopModule module) {
        return module.InvokeStop();
    }

    /// <summary>
    /// 调用模块的OnDestroy方法
    /// </summary>
    public static Exception? InvokeDestroy(EventLoopModule module) {
        return module.InvokeDestroy();
    }

    #endregion

    #region syncContext

    /// <summary>
    /// C#的这个同步上下文简直屎一般的设计，将自己发布为静态变量，方便一时，后悔一生。
    /// 当大家习惯了Await总是隐式回调到当前线程的时候，严重影响了扩展性，总是要兼容这个垃圾设计。
    /// await应该支持显式传参，传递要回调的线程。
    /// </summary>
    private static readonly ThreadLocal<IExecutor> localSyncContext = new();

    /// <summary>
    /// 获取当前线程的同步上下文
    /// </summary>
    public static IExecutor? Current => localSyncContext.Value;

    /// <summary>
    /// 设置当前线程的同步上下文
    /// </summary>
    public static void SetExecutor(IExecutor? context) {
        localSyncContext.Value = context;
    }

    /// <summary>
    /// 获取用于回调的线程
    /// </summary>
    /// <param name="executor"></param>
    /// <returns></returns>
    public static IExecutor? GetAwaiterExecutor(IExecutor? executor) {
        return executor ?? localSyncContext.Value;
    }

    #endregion
}
}