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

import java.util.concurrent.Executor;
import java.util.concurrent.RejectedExecutionException;
import java.util.function.Function;

/**
 * {@link IExecutor}在{@link Executor}的基础上增加了调度选项。
 * <p>
 * 该接口需要保持较高的抽象，因此将submit之类的方法下沉到子接口。如果需要获取任务结果，
 * 可通过{@link FutureUtils#submitFunc(Executor, Function, IContext)}这类工具方法实现。
 *
 * @author wjybxx
 * date - 2024/1/9
 */
public interface IExecutor extends Executor {

    /**
     * 在将来的某个时间执行给定的命令。
     * 命令可以在新线程中执行，也可以在池线程中执行，或者在调用线程中执行，这由Executor实现决定。
     * {@link Executor#execute(Runnable)}
     *
     * @param command 要执行的任务，注意{@link ITask}类型
     * @throws NullPointerException       如果任务为null
     * @throws RejectedExecutionException 如果Executor已开始关闭
     */
    @SuppressWarnings("NullableProblems")
    @Override
    void execute(Runnable command);

    /**
     * 在将来的某个时间执行给定的命令。
     * 命令可以在新线程中执行，也可以在池线程中执行，或者在调用线程中执行，这由Executor实现决定。
     * {@link Executor#execute(Runnable)}
     * <p>
     * 任务的调度特征值
     * 1.Executor需要感知用户任务的一些属性，以实现更好的管理。
     * 2.选项可参考{@link TaskOptions}。
     * 3.该接口不应该测试任务的类型，应当以参数的options为准。
     *
     * @param command 要执行的任务
     * @param options 任务的调度特征值，见{@link TaskOptions}
     * @throws NullPointerException       如果任务为null
     * @throws RejectedExecutionException 如果Executor已开始关闭
     * @implNote 实现类如果不支持选项，应该保守调度。
     */
    default void execute(Runnable command, int options) {
        execute(FutureUtils.toTask(command, options));
    }
}