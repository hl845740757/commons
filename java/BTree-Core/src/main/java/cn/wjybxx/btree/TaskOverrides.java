/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.btree;

import java.lang.reflect.Method;
import java.util.Arrays;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;

/**
 * 用于管理Task重写的方法信息
 *
 * @author wjybxx
 * date - 2024/1/25
 */
final class TaskOverrides {

    static final int MASK_BEFORE_ENTER = 1;
    static final int MASK_ENTER = 1 << 1;
    static final int MASK_EXIT = 1 << 2;
    static final int MASK_CANCEL_REQUESTED = 1 << 3;
    private static final int MASK_ALL = 15;

    static final int MASK_INLINABLE = 1 << 4;

    /** 由于mask使用低4位，因此Integer是缓存对象 - 装箱的影响很小 */
    private static final ConcurrentMap<Class<?>, Integer> maskCacheMap = new ConcurrentHashMap<>(64);

    @SuppressWarnings("rawtypes")
    static int maskOfTask(Class<? extends Task> clazz) {
        Integer cachedMask = maskCacheMap.get(clazz);
        if (cachedMask != null) {
            return cachedMask;
        }
        int mask = MASK_ALL; // 默认为全部重写
        try {
            if (getDeclaredMethodInherit(clazz, "beforeEnter") == null) {
                mask &= ~MASK_BEFORE_ENTER;
            }
            if (getDeclaredMethodInherit(clazz, "enter") == null) {
                mask &= ~MASK_ENTER;
            }
            if (getDeclaredMethodInherit(clazz, "exit") == null) {
                mask &= ~MASK_EXIT;
            }
            if (clazz.getDeclaredAnnotation(TaskInlinable.class) != null) {
                mask |= MASK_INLINABLE;
            }
        } catch (Exception ignore) {

        }
        maskCacheMap.put(clazz, mask);
        return mask;
    }

    private static Method getDeclaredMethodInherit(Class<?> clazz, String methodName) {
        while (clazz != Task.class) {
            Method targetMethod = Arrays.stream(clazz.getDeclaredMethods())
                    .filter(method -> methodName.equals(method.getName()))
                    .findFirst()
                    .orElse(null);
            if (targetMethod != null) {
                return targetMethod;
            }
            clazz = clazz.getSuperclass();
        }
        return null;
    }
}
