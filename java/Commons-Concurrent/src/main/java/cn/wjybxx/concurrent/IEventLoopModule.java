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

import cn.wjybxx.base.fx.ComponentIdPool;
import cn.wjybxx.base.fx.ComponentKind;
import cn.wjybxx.base.fx.IComponent;

/**
 * 事件循环的模块，亦即EventLoop的组件
 * 1.只有为{@link ComponentKind#SCRIPT}类型时才会被事件循环特殊调度，
 * 否则只调用{@link #onReady()}和{@link #onDestroy()}方法。
 * 2.执行顺序为
 * {@link #onReady()}、{@link #resolveDependence()}、
 * {@link #start()}、{@link #update()}、{@link #stop()}、
 * {@link #onDestroy()}
 * 3.如果支持{@link IAgentEvent}。可以实现{@link IAgentEventHandler}
 *
 * @author wjybxx
 * date - 2023/11/17
 */
public interface IEventLoopModule extends IComponent {

    /** 事件循环的全局组件id池 */
    ComponentIdPool GLOBAL = ComponentIdPool.newPool();

    /** 修正返回值类型 */
    @Override
    IEventLoop getEntity();

    /**
     * 处理依赖问题
     * 事件循环会在启动所有的模块之前调用该方法，此时所有的模块已执行{@link #onReady()}
     */
    default void resolveDependence() {

    }

    /**
     * worker会在启动时执行所有模块的start方法
     */
    default void start() {

    }

    /**
     * Worker每帧会调用调用所有模块的Update方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     */
    default void update() throws Exception {

    }

    /**
     * Worker每帧会调用调用所有模块的LateUpdate方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     */
    default void lateUpdate() throws Exception {

    }

    /**
     * Worker在停止时会调用所有模块的Stop方法，
     * 注意：默认按照启动顺序的逆顺序停止。
     */
    default void stop() {

    }
}