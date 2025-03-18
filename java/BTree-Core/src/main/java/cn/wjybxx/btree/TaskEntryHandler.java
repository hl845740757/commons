/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree;

/**
 * 用于处理Entry的完成事件
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public interface TaskEntryHandler<T> {

    /** 任务启动时调用 -- 同于将任务发布到其它地方，不可执行其它逻辑 */
    default void onEnter(TaskEntry<T> taskEntry) {

    }

    /** 任务退出时调用 -- 用于删除发布的数据，不可执行其它逻辑 */
    default void onExit(TaskEntry<T> taskEntry) {

    }

    /** 任务进入完成状态 */
    void onCompleted(TaskEntry<T> taskEntry);

    /** 任务的激活状态发生改变 */
    default void onActiveChanged(TaskEntry<T> taskEntry) {
    }
}