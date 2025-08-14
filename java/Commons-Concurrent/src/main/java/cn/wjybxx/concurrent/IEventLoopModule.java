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

import cn.wjybxx.base.fx.ComponentId;
import cn.wjybxx.base.fx.ComponentIdPool;
import cn.wjybxx.base.fx.ComponentKind;
import cn.wjybxx.base.fx.ComponentStatus;

import javax.annotation.Nonnull;

/**
 * 事件循环的模块，亦即EventLoop的组件
 * 1.只有为{@link ComponentKind#SCRIPT}类型时才会被事件循环特殊调度，
 * 否则只调用{@link #onAwake()}、{@link #onDestroy()}方法。
 * 2.执行顺序为
 * {@link #onAwake()}、{@link #resolveDependence()}、
 * {@link #start()}、
 * {@link #earlyUpdate()}、{@link #update()}、{@link #lateUpdate()}、
 * {@link #stop()}、
 * {@link #onDestroy()}
 * 3.如果支持{@link IAgentEvent}。可以实现{@link IAgentEventHandler}
 * <p>
 * 注意：这里的Update和游戏业务中的Update概念并不相同，游戏World中的FixedUpdate、Update、LateUpdate应当在Update场景的时候自行封装；
 * 但服务器通常可以直接使用这三个方法...
 *
 * @author wjybxx
 * date - 2023/11/17
 */
public interface IEventLoopModule {

    /** 事件循环的全局组件id池 */
    ComponentIdPool GLOBAL = ComponentIdPool.newPool();

    /** 获取绑定的事件循环，尚未挂载时为null */
    IEventLoop getEventLoop();

    /**
     * 获取组件id
     * 注意：组件在添加到实体后，组件id必须保持稳定
     */
    @Nonnull
    ComponentId<?> getCid();

    /**
     * 设置组件id
     * 注意：
     * 1.只有初始状态下可以设置
     * 2.泛型类如果想指向不同的组件id，必须手动设置组件id
     *
     * @throws IllegalStateException 如果组件不是{@link ComponentStatus#NEW}状态
     */
    void setCid(ComponentId<?> cid);

    /** 获取组件的状态 */
    ComponentStatus getStatus();

    /**
     * 组件在挂载到实体后调用；
     * 1.只能初始化自己的数据，不应该访问其它组件。
     * 2.该方法的设计初衷是处理反序列化的数据兼容性问题（成员可能不是正常构造的）
     * 3.组件之间不应该有顺序依赖
     */
    default void onAwake() {

    }

    /**
     * 从实体上删除时调用；
     * 1.只负责销毁自身的资源，数据也可能有较大的资源引用，因此也需要OnDestroy方法。
     * 2.只有挂载到实体上的组件会执行该方法。
     * 3.组件之间不应该有顺序依赖
     */
    default void onDestroy() {

    }

    /**
     * 解析依赖
     * <p>
     * 事件循环会在启动所有的模块之前调用该方法，此时所有的模块已执行{@link #onAwake()}。
     */
    default void resolveDependence() {

    }

    /**
     * worker会在启动时执行所有模块的start方法
     */
    default void start() {

    }

    /**
     * 该方法在所有module的{@link #update()}之前调用
     * <p>
     * Worker每帧会调用调用所有模块的EarlyUpdate方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     * 注意：这里的{@code Update}和游戏业务中的{@code Update}概念并不相同。
     */
    default void earlyUpdate() throws Exception {

    }

    /**
     * Worker每帧会调用调用所有模块的Update方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     */
    default void update() throws Exception {

    }

    /**
     * 在放在在所有module的{@link #update()}之后调用
     * <p>
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