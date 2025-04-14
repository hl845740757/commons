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

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Iterator;
import java.util.concurrent.Callable;
import java.util.concurrent.ScheduledExecutorService;

/**
 * 事件循环线程组，它管理着一组{@link IEventLoop}。
 * 它的本质是容器，它主要负责管理持有的EventLoop的生命周期。
 *
 * <h1>时序约定</h1>
 * 1.{@link IEventLoopGroup}代表着一组线程，不对任务的执行时序提供任何保证，用户只能通过工具自行协调。<br>
 * 2.{@link #execute(Runnable)}{@link #submit(Callable)}系列方法的时序等同于{@code schedule(task, 0, TimeUnit.SECONDS)}
 * <p>
 * Q: 为什么在接口层不提供严格的时序约定？<br>
 * A: 如果在接口层定义了严格的时序约定，实现类就会受到限制。
 * <p>
 * 1.时序很重要，在提供并发组件时应该详细的说明时序约定，否则用户将无所措手足。<br>
 * 2.EventLoopGroup也可以有自己的线程 - 一种常见的情况是Group是一个监控线程。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface IEventLoopGroup extends IScheduledExecutorService, ScheduledExecutorService, Iterable<IEventLoop> {

    /**
     * 选择一个 {@link IEventLoop}用于接下来的任务调度
     */
    @Nonnull
    IEventLoop select();

    /**
     * 注意；如果包含不定数量的EventLoop，返回的是快照。
     */
    @Nonnull
    @Override
    Iterator<IEventLoop> iterator();

    // endregion
}