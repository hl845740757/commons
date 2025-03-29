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

import java.util.function.ToIntFunction;

/**
 * @author wjybxx
 * date - 2025/3/29
 */
public class TaskPoolConfig {

    /** 计算任务池的大小 */
    private static volatile ToIntFunction<TaskPoolType> poolSizeCalculator;

    public static ToIntFunction<TaskPoolType> getPoolSizeCalculator() {
        return poolSizeCalculator;
    }

    public static void setPoolSizeCalculator(ToIntFunction<TaskPoolType> poolSizeCalculator) {
        TaskPoolConfig.poolSizeCalculator = poolSizeCalculator;
    }

    /**
     * 计算给定类型对象池的缓存池大小
     *
     * @param poolType 对象池类型
     * @return 对象池大小
     */
    public static int getPoolSize(TaskPoolType poolType) {
        ToIntFunction<TaskPoolType> func = poolSizeCalculator;
        if (func != null) {
            return Math.max(0, func.applyAsInt(poolType));
        }
        if (poolType == TaskPoolType.PROMISE_TASK
                || poolType == TaskPoolType.SCHEDULED_PROMISE_TASK
                || poolType == TaskPoolType.CTS_COMPLETION) {
            return 500;
        }
        return 100;
    }
}