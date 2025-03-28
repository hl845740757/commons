#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 组件模式的实体抽象
/// 
/// </summary>
public interface IEntity
{
    #region CURD

    /** 添加组件 */
    void AddComponent(IComponent comp);

    /** 删除组件 */
    bool DelComponent(IComponent comp);

    /** 是否包含指定组件 */
    bool ContainsComponent(IComponent comp);

    /** 实体绑定的所有组件 - 这通常是个快照，只有运行期不可变的实体，可以共享List */
    IList<IComponent> GetComponents();

    /** 获取所有的组件 -- 可使用外部的List */
    int GetComponents(List<IComponent> outList);

    /** 获取当前组件数量 */
    int CountComponent();

    #endregion

    #region cid-泛型

    /** 获取指定组件id关联的第一个组件 */
    T GetComponent<T>(ComponentId<T> cid) where T : IComponent;

    /** 获取指定组件id关联的最后一个组件 */
    T GetLastComponent<T>(ComponentId<T> cid) where T : IComponent;

    /** 获取指定组件id关联的所有组件 */
    List<T> GetComponents<T>(ComponentId<T> cid) where T : IComponent;

    /** 获取指定组件id关联的所有组件，返回返回的组件数量 */
    int GetComponents<T>(ComponentId<T> cid, List<T> outList) where T : IComponent;

    /** 删除指定组件id关联的第一个组件 -- 可能不支持通过该接口删除 */
    T DelComponent<T>(ComponentId<T> cid) where T : IComponent;

    /** 删除指定组件id关联的最后一个组件 -- 可能不支持通过该接口删除 */
    T DelLastComponent<T>(ComponentId<T> cid) where T : IComponent;

    /** 删除指定组件id关联的所有组件 -- 可能不支持通过该接口删除 */
    List<T> DelComponents<T>(ComponentId<T> cid) where T : IComponent;

    /** 删除指定组件id关联的所有组件，返回删除的组件数量 */
    int DelComponents<T>(ComponentId<T> cid, List<T> outList) where T : IComponent;

    /** 统计指定组件id关联的组件数 */
    int CountComponent(ComponentId cid);

    #endregion

    #region cid-非泛型
    // 真泛型要付出大量的额外工作量...

    /** 获取指定组件id关联的第一个组件 */
    IComponent GetComponent(ComponentId cid);

    /** 获取指定组件id关联的最后一个组件 */
    IComponent GetLastComponent(ComponentId cid);

    /** 获取指定组件id关联的所有组件 */
    List<IComponent> GetComponents(ComponentId cid);

    /** 获取指定组件id关联的所有组件，返回返回的组件数量 */
    int GetComponents(ComponentId cid, List<IComponent> outList);

    /** 删除指定组件id关联的第一个组件 -- 可能不支持通过该接口删除 */
    IComponent DelComponent(ComponentId cid);

    /** 删除指定组件id关联的最后一个组件 -- 可能不支持通过该接口删除 */
    IComponent DelLastComponent(ComponentId cid);

    /** 删除指定组件id关联的所有组件 -- 可能不支持通过该接口删除 */
    List<IComponent> DelComponents(ComponentId cid);

    /** 删除指定组件id关联的所有组件，返回删除的组件数量 */
    int DelComponents(ComponentId cid, List<IComponent> outList);

    #endregion
}
}