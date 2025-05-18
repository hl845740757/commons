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
package cn.wjybxx.base.concurrent;

import javax.annotation.Nonnull;

/**
 * 异步任务的上下文
 * 在异步和并发编程中，共享上下文是很必要的，且显式的共享优于隐式的共享。
 * 共享上下文可实现的功能：
 * 1.传递取消信号
 * 2.传递超时信息
 * 3.共享数据(K-V结果)
 * <p>
 * 关于上下文的设计，可阅读行为树的Task类
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
    IContext NONE = MiniContext.SHARABLE;

    /**
     * 任务绑定的状态
     * 1.任务之间通常不共享 -- 私有属性。
     * 2.运行时可能为null。
     */
    Object state();

    /**
     * 任务绑定的取消令牌（取消上下文）
     * 1.每个任务可有独立的取消信号 -- 私有属性。
     * 2.运行时不为null - 可返回{@link ICancelToken#NONE}。
     */
    @Nonnull
    ICancelToken cancelToken();

}