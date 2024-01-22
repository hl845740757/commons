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
     * 注意：应当慎用负数类型，否则可能导致{@link EventLoop}异常
     */
    void setType(int type);

    /** 获取事件的第一个参数 */
    Object getObj0();

    /** 设置事件的第一个参数 */
    void setObj0(Object obj);

    /**
     * 将事件的第一个参数转为{@link Runnable}类型
     * 这是{@link #getObj0()}的快捷方法
     */
    Runnable castObj0ToRunnable();

    /** 清理事件的引用数据 -- 避免内存泄漏 */
    void clean();

    /** 清理事件的所有数据 -- 基础值也重置 */
    void cleanAll();

}