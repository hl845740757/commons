/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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
 * 基于Disruptor架构的事件循环需要对外开放的接口
 *
 * @author wjybxx
 * date - 2025/3/30
 */
public interface IDisruptorEventLoop<T extends IAgentEvent> extends IEventLoop {

    /** 获取序号关联的事件 -- 仅限生产者调用，且只应调用一次 */
    T getEvent(long sequence);

    /**
     * @param size 申请的序号数量
     * @return 如果申请成功，则返回对应的sequence，否则返回 -1
     */
    long tryNextSequence(int size);

    /**
     * 开放的特殊接口
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre> {@code
     *      long sequence = eventLoop.nextSequence();
     *      try {
     *          AgentEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      } finally {
     *          eventLoop.publish(sequence)
     *      }
     * }</pre>
     *
     * @return 如果申请成功，则返回对应的sequence，否则返回 -1
     */
    long nextSequence();

    /** 发布申请的序号 */
    void publish(long sequence);

    /**
     * 1.按照规范，在调用该方法后，必须在finally块中进行发布。
     * 2.事件类型必须大于等于0，否则可能导致异常
     * 3.返回值为-1时必须检查
     * <pre>{@code
     *   int n = 10;
     *   long hi = eventLoop.nextSequence(n);
     *   try {
     *      long lo = hi - (n - 1);
     *      for (long sequence = lo; sequence <= hi; sequence++) {
     *          AgentEvent event = eventLoop.getEvent(sequence);
     *          // Do work.
     *      }
     *   } finally {
     *      eventLoop.publish(lo, hi);
     *   }
     * }</pre>
     *
     * @param size 申请的空间大小
     * @return 如果申请成功，则返回申请空间的最大序号，否则返回-1
     */
    long nextSequence(int size);

    /**
     * 发布申请的序号
     *
     * @param lo inclusive
     * @param hi inclusive
     */
    void publish(long lo, long hi);

    /**
     * 订阅事件
     * {@link IEventLoopModule}应当在启动时注册。
     *
     * @param type    事件类型
     * @param handler 事件处理器
     */
    void subscribe(int type, IAgentEventHandler<? super T> handler);
}