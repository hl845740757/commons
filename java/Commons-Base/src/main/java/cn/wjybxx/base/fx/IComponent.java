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

package cn.wjybxx.base.fx;

import javax.annotation.Nonnull;

/**
 * 组件
 * <p>
 * 注意：共享组件的声明周期相关的方法都会被忽略。
 *
 * @author wjybxx
 * date - 2024/6/22
 */
public interface IComponent {

    /** 获取组件挂载的实体，尚未挂载的情况下返回null */
    IEntity getEntity();

    /**
     * 获取组件id
     * 注意：组件在添加到实体后，组件id必须保持稳定
     */
    @Nonnull
    ComponentId<?> getCid();

    /**
     * 设置组件id
     * 注意：只有初始状态下可以设置
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
    default void onReady() {

    }

    /**
     * 从实体上删除时调用；
     * 1.只负责销毁自身的资源，数据也可能有较大的资源引用，因此也需要OnDestroy方法。
     * 2.只有挂载到实体上的组件会执行该方法。
     * 3.组件之间不应该有顺序依赖
     */
    default void onDestroy() {

    }
}