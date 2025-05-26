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
using System.Reflection;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 注入类型
/// </summary>
internal class InjectionType
{
    /// <summary>
    /// 被注入的类型
    /// </summary>
    public readonly Type type;
    /// <summary>
    /// 实现类的注入点
    /// </summary>
    public readonly ImmutableList<InjectionPoint> injectionPoints;
    /// <summary>
    /// 实体被创建后的钩子方法
    /// </summary>
    public readonly MethodInfo? onCreateHook;
    /// <summary>
    /// 实体被销毁前的钩子方法
    /// </summary>
    public readonly MethodInfo? onDisposeHook;

    public InjectionType(Type implType) {
        this.type = implType ?? throw new ArgumentNullException(nameof(implType));
        injectionPoints = Util.GetInjectPoints(implType);
        onCreateHook = Util.FindOnActiveMethod(implType);
        onDisposeHook = Util.FindOnDisposeMethod(implType);
    }

    /// <summary>
    /// 构造函数注入点
    /// </summary>
    public InjectionPoint? ConstructorInjectionPoint {
        get {
            if (injectionPoints.Count == 0) {
                return null;
            }
            // 我们约定构造器注入点固定排在第一位
            InjectionPoint injectionPoint = injectionPoints[0];
            return injectionPoint.memberInfo.MemberType == MemberTypes.Constructor
                ? injectionPoint
                : null;
        }
    }
}
}