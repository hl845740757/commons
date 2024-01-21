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

package cn.wjybxx.disruptor;

/**
 * 事件处理器
 * ps: 你可以实现自己的事件处理器和事件处理接口，这里的接口仅做参考。
 *
 * @author wjybxx
 * date - 2024/1/18
 */
@FunctionalInterface
public interface EventHandler<T> {

    /**
     * 接收到一个事件
     * 1. 如果消费者是多线程消费者，sequence可能不是有序的。
     * 2. 在单线程下，如果需要感知批量处理的开始和结束可实现{@link BatchEventHandler}接口。
     *
     * @param event    事件
     * @param sequence 事件对应的序号
     */
    void onEvent(T event, long sequence) throws Exception;

}