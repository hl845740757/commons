/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

/**
 * 可分时运行的任务 - 需要长时间运行才能得出结果的任务。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@FunctionalInterface
public interface TimeSharingTask<V> {

    /**
     * 单步执行一次。
     * 1.Executor在执行该方法之前会调用{@link IPromise#trySetComputing()}，因此任务内部无需再次调用。
     * 2.任务在完成时需要将Promise置为完成状态！
     *
     * @param promise 用于获取任务上下文和设置结果
     */
    void step(IPromise<? super V> promise) throws Exception;

}