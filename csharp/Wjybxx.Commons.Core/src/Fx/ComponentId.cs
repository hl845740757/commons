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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 组件ID
///
/// 1.可通过<see cref="ComponentDefineAttribute"/>定义组件元数据。
/// 2.组件ID用于定义组件的元数据，应当保持不可变。
/// 3.是否和Entity同生命周期（禁止提前删除），是属于组件实例的属性，并非这一类组件的共同属性。
/// 4.实现为类而非接口，以避免不必要的虚方法调用。
/// </summary>
[Immutable]
public partial class ComponentId : AbstractConstant
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
    /** 启用的函数，扫描重写的方法计算得到 -- 存在继承的情况下不可使用该值 */
    public readonly long enableFuncs;
    /** 超类组件id -- 即当前组件可充当目标组件 */
    public readonly ComponentId baseId;

    /** 业务自定义flags */
    public readonly long flags;
    /** 用于分组的键 */
    public readonly int groupKey;
    /** 挂载路径 */
    public readonly string mountPath;
    /** 用户扩展数据 -- 必须的不可变的 */
    public readonly object extraInfo;

    protected ComponentId(IBuilder builder)
        : base(builder) {
        Debug.Assert(builder.CacheIndex >= 0);
        this.index = builder.CacheIndex;
        this.kind = builder.Kind;
        this.shared = builder.Shared;
        this.maxCount = Math.Max(1, builder.MaxCount);
        this.enableFuncs = builder.EnableFuncs;
        this.baseId = builder.BaseId;

        this.flags = builder.Flags;
        this.groupKey = builder.GroupKey;
        this.mountPath = builder.MountPath;
        this.extraInfo = builder.ExtraInfo;
    }

    /** 是否是私有脚本 --- 需要被框架调度 */
    public bool IsPrivateScript => !shared && kind == ComponentKind.Script;

    /** 是否是目标组件的子类型 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSubtypeOf(ComponentId other) {
        if (ReferenceEquals(this, other)) {
            return true;
        }
        ComponentId tempBaseId = this.baseId;
        while (tempBaseId != null) {
            if (ReferenceEquals(tempBaseId, other)) {
                return true;
            }
            tempBaseId = tempBaseId.baseId;
        }
        return false;
    }

    /// <summary>
    /// 创建一个Builder
    /// </summary>
    /// <param name="name">组件id的名字</param>
    /// <typeparam name="T">组件的类型</typeparam>
    /// <returns></returns>
    public static Builder<T> NewBuilder<T>(string name) {
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
///
/// 注：泛型T除了辅助类型转换外，没有其它作用，因此不添加泛型约束 -- 会更易用。
/// </summary>
/// <typeparam name="T">泛型T用于辅助类型转换</typeparam>
public class ComponentId<T> : ComponentId
{
    public ComponentId(ComponentId.Builder<T> builder) : base(builder) {

    }
}
}