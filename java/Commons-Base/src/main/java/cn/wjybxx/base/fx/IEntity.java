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
 * <p>
 * Q：需要提供根据{@link Class}查询组件的接口吗？
 * A：不需要！用户可以根据{@link Class}查询关联的组件id，再来查询。
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
    int getComponents(List<IComponent> outList);

    /** 获取当前组件数量 */
    int countComponent();

    // endregion

    // region cid

    /** 获取指定组件id关联的第一个组件 */
    <T extends IComponent> T getComponent(ComponentId<T> cid);

    /** 获取指定组件id关联的最后一个组件 */
    <T extends IComponent> T getLastComponent(ComponentId<T> cid);

    /** 获取指定组件id关联的所有组件 */
    <T extends IComponent> List<T> getComponents(ComponentId<T> cid);

    /** 获取指定组件id关联的所有组件，返回返回的组件数量 */
    <T extends IComponent> int getComponents(ComponentId<T> cid, List<? super T> outList);

    /** 删除指定组件id关联的第一个组件 -- 可能不支持通过该接口删除 */
    <T extends IComponent> T delComponent(ComponentId<T> cid);

    /** 删除指定组件id关联的最后一个组件 -- 可能不支持通过该接口删除 */
    <T extends IComponent> T delLastComponent(ComponentId<T> cid);

    /** 删除指定组件id关联的所有组件 -- 可能不支持通过该接口删除 */
    <T extends IComponent> List<T> delComponents(ComponentId<T> cid);

    /** 删除指定组件id关联的所有组件，返回删除的组件数量 */
    <T extends IComponent> int delComponents(ComponentId<T> cid, List<? super T> outList);

    /** 统计指定组件id关联的组件数 */
    int countComponent(ComponentId<?> cid);

    // endregion
}