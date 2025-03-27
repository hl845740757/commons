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
import cn.wjybxx.base.fx.ComponentIdPool;

import java.lang.reflect.Method;
import java.util.List;

/**
 * @author wjybxx
 * date - 2025/3/27
 */
public class EventLoopUtils {

    /** 事件循环的全局组件id池 */
    public static final ComponentIdPool GLOBAL = ComponentIdPool.newPool();

    /** 将组件散开为基于组件index的数组 -- 暂时禁止组件重复 */
    public static EventLoopModule[] toIndexedArray(List<EventLoopModule> moduleList) {
        if (moduleList.isEmpty()) {
            return new EventLoopModule[0];
        }
        int maxIndex = moduleList.stream()
                .mapToInt(e -> e.getCid().index)
                .max()
                .orElseThrow();

        EventLoopModule[] result = new EventLoopModule[maxIndex + 1];
        for (EventLoopModule module : moduleList) {
            EventLoopModule exist = result[module.getCid().index];
            if (exist != null) {
                throw new IllegalStateException("module is duplicate, cid: " + module.getCid());
            }
            result[module.getCid().index] = module;
        }
        return result;
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