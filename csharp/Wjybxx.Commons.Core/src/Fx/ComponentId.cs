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
using System.Diagnostics;

namespace Wjybxx.Commons.Fx
{
public partial interface ComponentId : IConstant
{
    /** 高速缓存下标 */
    public int Index { get; }
    /** 组件类型 */
    public ComponentKind Kind { get; }
    /** 是否是共享组件 */
    public bool Shared { get; }
    /** 最大可挂载数量 */
    public int MaxCount { get; }
    /** 最大可挂载数量 */
    public long EnableFuncs { get; }
    
    /** 业务自定义flags */
    public long Flags { get; }
    /** 挂载路径 */
    public string MountPath { get; }
    /** 自定义扩展数据 */
    public object ExtraInfo { get; }

    /** 是否是私有脚本 --- 需要被框架调度 */
    public bool IsPrivateScript => !Shared && Kind == ComponentKind.Script;

    /// <summary>
    /// 创建一个Builder
    /// </summary>
    /// <param name="name">组件id的名字</param>
    /// <typeparam name="T">组件的类型</typeparam>
    /// <returns></returns>
    public static Builder<T> NewBuilder<T>(string name) where T : IComponent {
        return new Builder<T>(name);
    }

    /// <summary>
    /// 创建一个Builder
    /// </summary>
    /// <param name="name">组件id的名字</param>
    /// <param name="type">组件的类型</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static IBuilder NewBuilder(string name, Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        Type builderType = typeof(Builder<>).MakeGenericType(type);
        object inst = Activator.CreateInstance(builderType, name); // 默认调用public构造函数
        return inst as IBuilder ?? throw new InvalidOperationException();
    }
}

/// <summary>
/// 组件Id
/// </summary>
/// <typeparam name="T">泛型T用于辅助类型转换</typeparam>
public class ComponentId<T> : AbstractConstant, ComponentId where T : IComponent
{
#nullable disable
    /** 高速缓存下标 */
    public readonly int index;
    /** 组件类型 */
    public readonly ComponentKind kind;
    /** 是否是共享组件 -- 通常共享组件的所有方法都不被框架调用；甚至不会被注入实体的引用 */
    public readonly bool shared;
    /** 最大可挂载数量 */
    public readonly int maxCount;
    /** 启用的函数，扫描重写的方法计算得到 -- 共享组件的该值将被修正为0 */
    public readonly long enableFuncs;

    /** 业务自定义flags */
    public readonly long flags;
    /** 挂载路径 */
    public readonly string mountPath;
    /** 用户扩展数据 -- 必须的不可变的 */
    public readonly object extraInfo;

    public ComponentId(ComponentId.Builder<T> builder)
        : base(builder) {
        Debug.Assert(builder.CacheIndex >= 0);
        this.index = builder.CacheIndex;
        this.kind = builder.Kind;
        this.shared = builder.Shared;
        this.maxCount = Math.Max(1, builder.MaxCount);
        this.enableFuncs = builder.EnableFuncs;

        this.flags = builder.Flags;
        this.mountPath = builder.MountPath;
        this.extraInfo = builder.ExtraInfo;
    }

    public int Index => index;
    public ComponentKind Kind => kind;
    public bool Shared => shared;
    public int MaxCount => maxCount;
    public long EnableFuncs => enableFuncs;
    public long Flags => flags;
    public string MountPath => mountPath;
    public object ExtraInfo => extraInfo;

    /** 是否是私有脚本 --- 需要被框架调度 */
    public bool IsPrivateScript => !shared && kind == ComponentKind.Script;
}
}