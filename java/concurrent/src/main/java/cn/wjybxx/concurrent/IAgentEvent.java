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
 * {@link EventLoopAgent}接收的事件类型
 * <p>
 * 注意：实现类最好保持为简单的数据类，不要赋予逻辑。
 *
 * @author wjybxx
 * date - 2024/1/22
 */
public interface IAgentEvent {

    /** 表示事件无效 */
    int TYPE_INVALID = -1;
    /** 表示普通的{@link Runnable} */
    int TYPE_RUNNABLE = 0;

    /** 获取事件的类型 */
    int getType();

    /**
     * 设置事件的类型
     *
     * @apiNote 应当慎用负数类型，否否则可能影响事件循环的工作。
     * @implNote 由于clean的存在，用户忘记赋值的情况下仍然可能为 -1，事件循环的实现者需要注意
     */
    void setType(int type);

    /** 事件或任务的调度选项 */
    int getOptions();

    /**
     * 将options存储在Event上。
     * 1.以支持自定义事件中的调度选项 -- 冗余存储，解除耦合。
     * 2.可避免对{@link Runnable}的封装。
     */
    void setOptions(int options);

    /** 获取事件的第一个参数 */
    Object getObj0();

    /** 设置事件的第一个参数 */
    void setObj0(Object obj);

    /**
     * 清理事件的引用数据 -- 避免内存泄漏
     * ps:事件循环每处理完事件就会调用该方法以避免内存泄漏
     */
    void clean();

    /** 清理事件的所有数据 -- 基础值也重置 */
    void cleanAll();

}