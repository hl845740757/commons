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
 * 3.该接口的实例通常是不应该被序列化的.
 * 4.接口不再暴露Future，以允许Task在完成后清理Future。
 *
 * @author wjybxx
 * date - 2023/11/16
 */
public interface IFutureTask<V> extends ITask {

    /**
     * 是否收到了取消信号；
     * 调度器会检查任务的取消信号，以避免不必要的执行。
     */
    boolean isCancelRequested();

    /**
     * 取消执行
     * 可能是检测到取消信号，也可能是其它原因，EventLoop主动停止任务。
     * 如果此时收到了取消信号，可优先使用取消令牌中的取消码进入取消状态。
     * 如果task已进入完成状态。可忽略该请求。
     */
    void cancel(int code);

}