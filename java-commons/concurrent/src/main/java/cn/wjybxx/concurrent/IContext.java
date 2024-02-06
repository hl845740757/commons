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

/**
 * 异步任务的上下文
 * 在异步和并发编程中，共享上下文是很必要的，且显式的共享优于隐式的共享。
 * 共享上下文可实现的功能：
 * 1.传递取消信号
 * 2.传递超时信息
 * 3.共享数据(K-V结果)
 * <h3>上下文扩展</h3>
 * 由于这里的上下文和任务之间是组合关系，因此用户既可以通过实现更具体的上下文类型扩展，也可以仅通过扩展黑板实现。
 * 对于简单的情况：可通过实现更具体的Context类型解决。
 * 对于复杂的情况：建议通过黑板实现。
 * <p>
 * 关于上下文的设计，也可阅读我在行为树中的设计<a href="https://github.com/hl845740757/BTree">BTree</a>。
 *
 * @author wjybxx
 * date - 2023/11/18
 */
public interface IContext {

    /**
     * 空上下文
     * 1. 建议使用空上下文代替null
     * 2. 可以通过该对象创建子上下文
     */
    IContext NONE = Context.ofCancelToken(ICancelToken.NONE);

    /**
     * 任务绑定的取消令牌（取消上下文）
     * 1.每个任务可有独立的取消信号；
     * 2.运行时不为null；
     */
    @Nonnull
    ICancelToken cancelToken();

    /**
     * 任务运行时依赖的黑板（主要上下文）
     * 1.每个任务可有独立的黑板（数据）；
     * 2.一般而言，黑板需要实现递归向上查找。
     * <p>
     * 这里未直接实现为类似Map的读写接口，是故意的。
     * 因为提供类似Map的读写接口，会导致创建Context的开销变大，而在许多情况下是不必要的。
     * 将黑板设定为Object类型，既可以增加灵活性，也可以减少一般情况下的开销。
     */
    Object blackboard();

    /**
     * 共享属性（配置上下文）
     * 1.用于支持【数据和行为分离】的Task体系。
     * 2.共享属性应该是只读的、可共享的，因为它是配置。
     * <p>
     * 数据和行为分离是指：Task仅包含行为，其属性是外部传入的；属性可能是单个任务的，也可能是多个任务共享的。
     */
    Object sharedProps();

    /**
     * 去除取消令牌 -- 将取消令牌替换为{@link ICancelToken#NONE}。
     * 注意：不是创建子上下文，而是同级上下文。
     *
     * @return 如果当前取消令牌已是不可取消的令牌，则可返回自身。
     * @implNote 应当返回相同类型
     */
    IContext withoutCancelToken();

}