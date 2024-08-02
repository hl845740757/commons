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

import cn.wjybxx.base.annotation.Beta;

/**
 * 可分时运行的任务 - 需要长时间运行才能得出结果的任务。
 * 1. 分时任务代表着所有需要自定义管理状态的任务。
 * 2. 除了设置Promise的结果外，Task还可以约定特殊的Promise以向外部传递其它信息，比如：任务的进度。
 * 3. 该接口尚不稳定，避免用于非EventLoop架构。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@Beta
@FunctionalInterface
public interface TimeSharingTask<V> {

    /** 任务在添加到Executor之前进行绑定 */
    default void inject(IContext ctx, IPromise<? super V> promise) {

    }

    /**
     * 任务开始前调用
     * 1. start和update是连续执行的。
     * 2. start抛出异常会导致任务直接结束。
     *
     * @param ctx     任务的上下文
     * @param promise 关联的promise
     */
    default void start(IContext ctx, IPromise<? super V> promise) {

    }

    /**
     * 单步执行一次。
     * 1.Executor在执行该方法之前会调用{@link IPromise#trySetComputing()}，因此任务内部无需再次调用。
     * 2.任务在完成时需要将Promise置为完成状态！
     *
     * @param ctx     任务的上下文
     * @param promise 关联的promise
     */
    void update(IContext ctx, IPromise<? super V> promise) throws Exception;

    /**
     * 任务结束时调用
     * 1.只有在成功执行{@link #start(IContext, IPromise)}的情况下才会调用
     * 2.会在start所在的executor调用 -- 如果目标executor已关闭则不会执行。
     *
     * @param ctx     任务的上下文
     * @param promise 关联的promise
     */
    default void stop(IContext ctx, IPromise<? super V> promise) {

    }
}