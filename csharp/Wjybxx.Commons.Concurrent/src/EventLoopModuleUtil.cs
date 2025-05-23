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
using System.Reflection;
using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该工具类主要用于暴露特殊接口给其它程序集
/// </summary>
public static class EventLoopModuleUtil
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

    /** 是否重写了<see cref="IEventLoopModule.EarlyUpdate"/>方法 */
    public static bool IsOverrideEarlyUpdate(IEventLoopModule module) {
        return IsOverride(module.GetType(), "EarlyUpdate", Array.Empty<Type>());
    }

    /** 是否重写了<see cref="Wjybxx.Commons.Concurrent.IEventLoopModule.Update()"/>方法 */
    public static bool IsOverrideUpdate(IEventLoopModule module) {
        return IsOverride(module.GetType(), "Update", Array.Empty<Type>());
    }

    /** 是否重写了<see cref="Wjybxx.Commons.Concurrent.IEventLoopModule.LateUpdate()"/>方法 */
    public static bool IsOverrideLateUpdate(IEventLoopModule module) {
        return IsOverride(module.GetType(), "LateUpdate", Array.Empty<Type>());
    }

    /** 是否重写了某个方法 */
    private static bool IsOverride(Type currentType, string methodName, params Type[] paramTypes) {
        MethodInfo? methodInfo = currentType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, paramTypes, modifiers: null);
        if (methodInfo == null) {
            return true;
        }
        // 抽象类覆盖了所有的接口方法，因此是测试抽象类
        Type declaringType = methodInfo.DeclaringType!;
        return declaringType != typeof(EventLoopModule);
    }

    #endregion
}
}