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
 * 批量事件处理器
 * ps: 你可以实现自己的事件处理器和事件处理接口，这里的接口仅做参考。
 *
 * @author wjybxx
 * date - 2024/1/18
 */
public interface BatchEventHandler<T> extends EventHandler<T> {

    /**
     * 批处理开始
     * 注意：抛出异常可能导致不确定的行为
     *
     * @param firstSequence 批处理的第一个序号
     * @param batchSize     批处理大小
     */
    void onBatchStart(long firstSequence, int batchSize);

    /**
     * 批处理结束
     *
     * @param lastSequence 批处理的最后一个sequence
     */
    void onBatchEnd(long lastSequence);

}