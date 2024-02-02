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
 * 1.该接口暴露给Executor的扩展类，不是用户使用的类。
 * 2.该接口的实例通常是不应该被序列化的
 * <p>
 * 实际上，Task不继承Runnable会更好，这样更具有识别度；也可以避免{@link IExecutor#execute(Runnable, int)}的歧义问题。
 * 但如果不继承{@link Runnable}，则我们总要对用户的任务进行封装，这可能产生较多的开销 —— 因此我们仍然继承{@link Runnable}接口。
 *
 * @author wjybxx
 * date - 2024/2/2
 */
@Beta
public interface ITask extends Runnable {

    /** 任务的调度选项 */
    int getOptions();

    /** 设置任务的调度选项 */
    void setOptions(int options);

}