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

import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.fx.ComponentStatus;

import java.lang.reflect.Method;

/**
 * 该工具类主要用于暴露特殊接口给其它程序集
 *
 * @author wjybxx
 * date - 2025/5/17
 */
public class EventLoopModuleUtils {

    /** 设置Module的状态 */
    public static void SetStatus(EventLoopModule module, ComponentStatus status) {
        module.setStatus(status);
    }

    /** 设置Module绑定的事件循环，会同时调用模块的OnReady方法。 */
    public static void SetEventLoop(IEventLoop eventLoop, EventLoopModule module) {
        module.setEventLoop(eventLoop);
    }

    /** 调用模块的Start方法 */
    public static void InvokeStart(EventLoopModule module) {
        module.invokeStart();
    }

    /** 调用模块的Stop方法 */
    public static void InvokeStop(EventLoopModule module) {
        module.invokeStop();
    }

    /** 调用模块的OnDestroy方法 */
    public static void InvokeDestroy(EventLoopModule module) {
        module.invokeDestroy();
    }

    /** 是否重写了{@link IEventLoopModule#earlyUpdate()}方法 */
    public static boolean isOverrideEarlyUpdate(IEventLoopModule module) {
        try {
            Method method = module.getClass().getMethod("earlyUpdate", ArrayUtils.EMPTY_CLASS_ARRAY);
            return !method.getDeclaringClass().isInterface();
        } catch (NoSuchMethodException ignore) {
            return false;
        }
    }

    /** 是否重写了{@link IEventLoopModule#update()}方法 */
    public static boolean isOverrideUpdate(IEventLoopModule module) {
        try {
            Method method = module.getClass().getMethod("update", ArrayUtils.EMPTY_CLASS_ARRAY);
            return !method.getDeclaringClass().isInterface();
        } catch (NoSuchMethodException ignore) {
            return false;
        }
    }

    /** 是否重写了{@link  IEventLoopModule#lateUpdate}方法 */
    public static boolean isOverrideLateUpdate(IEventLoopModule module) {
        try {
            Method method = module.getClass().getMethod("lateUpdate", ArrayUtils.EMPTY_CLASS_ARRAY);
            return !method.getDeclaringClass().isInterface();
        } catch (NoSuchMethodException ignore) {
            return false;
        }
    }

}