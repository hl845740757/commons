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
 * FutureTask是Executor压入的可获取结果的任务类型
 * 1.该接口暴露给Executor的扩展类，不是用户使用的类。
 * 2.需要获取结果的任务，我们将调度选项保存下来；普通任务的调度选项可能仅在execute时使用。
 * 3.该接口的实例通常是不应该被序列化的
 *
 * @author wjybxx
 * date - 2023/11/16
 */
public interface IFutureTask<V> extends ITask {

    /**
     * 用于获取结果的句柄
     * 注意：返回给用户时应当转为{@link IFuture}类型。
     */
    IPromise<V> future();

    /**
     * run方法应当使{@link #future()}进入完成状态
     */
    @Override
    void run();

}