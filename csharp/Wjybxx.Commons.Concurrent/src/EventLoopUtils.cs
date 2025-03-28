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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
public static class EventLoopUtils
{
    /** 事件循环的全局组件id池 */
    public static readonly ComponentIdPool GLOBAL = ComponentIdPool.NewPool();

    /** 将组件散开为基于组件index的数组 -- 暂时禁止组件重复 */
    public static EventLoopModule[] ToIndexedArray(ICollection<EventLoopModule> moduleList) {
        if (moduleList.Count == 0) {
            return Array.Empty<EventLoopModule>();
        }
        int maxIndex = moduleList
            .Select(e => e.Cid.Index)
            .Max();

        EventLoopModule[] result = new EventLoopModule[maxIndex + 1];
        foreach (EventLoopModule module in moduleList) {
            EventLoopModule exist = result[module.Cid.Index];
            if (exist != null) {
                throw new IllegalStateException("module is duplicate, cid: " + module.Cid);
            }
            result[module.Cid.Index] = module;
        }
        return result;
    }

    /** 是否重写了<see cref="IEventLoopModule.Update"/>方法 */
    public static bool IsOverrideUpdate(IEventLoopModule module) {
        return !IsSkippable(module.GetType(), "Update", Array.Empty<Type>());
    }

    /** 是否重写了<see cref="IEventLoopModule.LateUpdate"/>方法 */
    public static bool IsOverrideLateUpdate(IEventLoopModule module) {
        return !IsSkippable(module.GetType(), "LateUpdate", Array.Empty<Type>());
    }

    /** 方法是否可跳过 */
    private static bool IsSkippable(Type handlerType, string methodName, params Type[] paramTypes) {
        MethodInfo methodInfo = handlerType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, paramTypes, modifiers: null);
        if (methodInfo == null) {
            return true;
        }
        // 抽象类覆盖了所有的接口方法，因此是测试抽象类
        Type declaringType = methodInfo.DeclaringType!;
        if (declaringType.IsGenericType) {
            return declaringType.GetGenericTypeDefinition() == typeof(EventLoopModule);
        }
        return declaringType == typeof(EventLoopModule);
    }
}
}