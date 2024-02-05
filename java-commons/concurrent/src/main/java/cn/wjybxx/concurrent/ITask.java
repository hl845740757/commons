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
 * Task是Executor中调度的任务抽象。
 * 1. 该接口暴露给Executor的扩展类，用户尽量避免直接实现该接口。
 * 2. 该接口的实例通常是不应该被序列化的。
 * <p>
 * Task不继承{@link Runnable}有更好的识别度，也可以避免{@link IExecutor#execute(Runnable, int)}的歧义问题。
 * 但不继承的情况下，我们总是要对用户的任务进行封装，这可能产生较多的开销。
 *
 * @author wjybxx
 * date - 2024/2/2
 */
@Beta
public interface ITask extends Runnable {

    /**
     * 任务的调度选项
     *
     * @implNote 在任务执行期间不应该变化
     */
    int getOptions();

}