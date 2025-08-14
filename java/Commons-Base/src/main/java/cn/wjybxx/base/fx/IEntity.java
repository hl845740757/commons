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

import java.util.List;

/**
 * 组件模式的实体抽象
 * 注：这只是个参考抽象。
 *
 * @author wjybxx
 * date - 2024/6/22
 */
public interface IEntity {

    // region CURD

    /** 添加组件 */
    void addComponent(IComponent comp);

    /** 删除组件 */
    boolean delComponent(IComponent comp);

    /** 是否包含指定组件 */
    boolean containsComponent(IComponent comp);

    /** 实体绑定的所有组件 - 这通常是个快照，只有运行期不可变的实体，可以共享List */
    List<? extends IComponent> getComponents();

    /** 获取所有的组件 -- 可使用外部的List */
    void getComponents(List<IComponent> outList);

    /** 获取当前组件数量 */
    int getComponentCount();

    // endregion

    // region cid

    /** 获取指定组件id关联的第一个组件 */
    <T> T getComponent(ComponentId<T> cid);

    /** 获取指定组件id关联的最后一个组件 */
    <T> T getLastComponent(ComponentId<T> cid);

    /** 获取指定组件id关联的所有组件 */
    <T> List<T> getComponents(ComponentId<T> cid);

    /** 获取指定组件id关联的所有组件，返回返回的组件数量 */
    <T> void getComponents(ComponentId<T> cid, List<? super T> outList);

    /** 删除指定组件id关联的第一个组件 -- 可能不支持通过该接口删除 */
    <T> T delComponent(ComponentId<T> cid);

    /** 删除指定组件id关联的最后一个组件 -- 可能不支持通过该接口删除 */
    <T> T delLastComponent(ComponentId<T> cid);

    /** 删除指定组件id关联的所有组件 -- 可能不支持通过该接口删除 */
    <T> List<T> delComponents(ComponentId<T> cid);

    /** 删除指定组件id关联的所有组件，返回删除的组件数量 */
    <T> int delComponents(ComponentId<T> cid, List<? super T> outList);

    /** 统计指定组件id关联的组件数 */
    int countComponent(ComponentId<?> cid);

    // endregion
}