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
using System.Collections.Concurrent;
using System.Reflection;

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 组件id池
/// 1.常量池用于定义命名空间
/// 2.用户使用的时候，应当封装一个静态类，尽量将常用的组件id都定义下来。
/// </summary>
public sealed class ComponentIdPool
{
    private readonly ConstantPool<ComponentId> pool;
    private readonly Func<Type, ComponentId.IBuilder>? cidParser;
    private readonly ConcurrentDictionary<Type, ComponentId> class2CidMap = new();

    public ComponentIdPool(ConstantPool<ComponentId> pool,
                           Func<Type, ComponentId.IBuilder>? cidParser) {
        this.pool = pool;
        this.cidParser = cidParser;
    }

    #region 委托

    /** 创建一个新的池：即新的命名空间 */
    public static ComponentIdPool NewPool() {
        return new ComponentIdPool(ConstantPool<ComponentId>.NewPool(null), null);
    }

    /// <summary>
    /// 创建一个新的池：即新的命名空间
    ///
    /// 注意：需要小心处理泛型类的组件id解析
    /// </summary>
    /// <param name="cidParser">id解析器</param>
    /// <returns></returns>
    public static ComponentIdPool NewPool(Func<Type, ComponentId.IBuilder> cidParser) {
        return new ComponentIdPool(ConstantPool<ComponentId>.NewPool(null), cidParser);
    }

    /** 构建一个组件id */
    public ComponentId<T> Create<T>(ComponentId.Builder<T> builder) where T : IComponent {
        return (ComponentId<T>)pool.NewInstance(builder);
    }

    /** 构建一个组件id -- 内部进行泛型转换 */
    public ComponentId Create(ComponentId.IBuilder builder) {
        return pool.NewInstance(builder);
    }

    /** 创建或使用既有的组件id */
    public ComponentId<T> ValueOf<T>(ComponentId.Builder<T> builder) where T : IComponent {
        return (ComponentId<T>)pool.ValueOf(builder);
    }

    /** 创建或使用既有的组件id -- 内部进行泛型转换 */
    public ComponentId ValueOf(ComponentId.IBuilder builder) {
        return pool.ValueOf(builder);
    }

    /** 组件id是否存在 */
    public bool Exists(string name) {
        return pool.Exists(name);
    }

    /** 获取对应的组件id */
    public ComponentId? Get(string name) {
        return pool.Get(name);
    }

    /** 如果不存在对应的组件id，则抛出异常 */
    public ComponentId GetOrThrow(string name) {
        return pool.GetOrThrow(name);
    }

    /** 创建常量池快照 */
    public ConstantMap<ComponentId> NewConstantMap() {
        return pool.NewConstantMap();
    }

    #endregion

    /// <summary>
    /// 主要用于运行时获取对应的组件id
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public ComponentId ValueOf(Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        // 先从缓存中查询
        if (class2CidMap.TryGetValue(type, out ComponentId cid)) {
            return cid;
        }
        // 优先处理重定向
        ComponentRedirect componentRedirect = type.GetCustomAttribute<ComponentRedirect>();
        if (componentRedirect != null) {
            cid = ValueOf(componentRedirect.baseType);
            class2CidMap.TryAdd(type, cid);
            return cid;
        }
        // 通过builder构建
        ComponentId.IBuilder builder;
        if (cidParser != null) {
            builder = cidParser.Invoke(type);
        } else {
            // 泛型类，在自动处理的情况下指向同一个组件id
            ComponentDefine componentDefine = type.GetCustomAttribute<ComponentDefine>();
            if (componentDefine == null) {
                string compName = GetRawTypeName(type);
                builder = ComponentId.NewBuilder(compName, type);
            } else {
                string compName = componentDefine.Name;
                if (string.IsNullOrWhiteSpace(compName)) {
                    compName = GetRawTypeName(type);
                }
                builder = ComponentId.NewBuilder(compName, type);
                builder.Kind = componentDefine.Kind;
                builder.Shared = componentDefine.Shared;
                builder.MaxCount = componentDefine.MaxCount;
                builder.Flags = componentDefine.Flags;
                builder.MountPath = componentDefine.MountPath;
            }
        }
        cid = pool.ValueOf(builder);
        class2CidMap.TryAdd(type, cid);
        return cid;
    }

    /// <summary>
    /// 获取的Type的简单名，泛型类指向相同的类名
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static string GetRawTypeName(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        return type.Namespace + "." + type.Name;
    }
}
}