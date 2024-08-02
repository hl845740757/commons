#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Linq;
using System.Reflection;
using Wjybxx.Commons.Apt;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 
/// </summary>
internal class AptFieldProps
{
#nullable disable
    /// <summary>
    /// 属性配置
    /// </summary>
    public DsonPropertyAttribute attribute;
#nullable enable
    /// <summary>
    /// 关联的自动属性（缓存）
    /// </summary>
    public PropertyInfo? autoProperty;
    /// <summary>
    /// 是否不序列化 -- 非null表示注解指定了值
    /// </summary>
    public bool? ignore;
    /// <summary>
    /// 实现类 -- 会被替换
    /// </summary>
    public Type? implType;

    public static AptFieldProps Parse(MemberInfo memberInfo) {
        AptFieldProps props = new AptFieldProps();
        props.attribute = memberInfo.GetCustomAttributes()
            .FirstOrDefault(e => e is DsonPropertyAttribute) as DsonPropertyAttribute;

        if (props.attribute != null) {
            // 需要处理泛型参数，将字段的泛型参数拷贝给Impl
            props.implType = props.attribute.Impl;
            if (props.implType != null && props.implType.IsGenericType) {
                Type fieldType = BeanUtils.GetMemberType(memberInfo);
                props.implType = props.implType.GetGenericTypeDefinition()
                    .MakeGenericType(fieldType.GetGenericArguments());
            }
        } else {
            props.attribute = new DsonPropertyAttribute(); // 赋默认值
            props.implType = null;
        }

        return props;
    }

    public void ParseIgnore(MemberInfo memberInfo) {
        DsonIgnoreAttribute? ignoreAttribute = memberInfo.GetCustomAttributes()
            .FirstOrDefault(e => e is DsonIgnoreAttribute) as DsonIgnoreAttribute;
        if (ignoreAttribute != null) {
            ignore = ignoreAttribute.Value;
        } else if (memberInfo.IsDefined(typeof(NonSerializedAttribute))) {
            ignore = true;
        }
    }
}
}