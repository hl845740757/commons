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

#nullable enable
using System.Collections.Generic;

namespace Wjybxx.Commons.Apt
{
/// <summary>
/// 属性（注解）的值
/// </summary>
public readonly struct AptAttributeValue
{
    /// <summary>
    /// 注解属性的名字
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 如果是基础数值类型，返回类型为对应的基本类型；如果是string，则返回类型为string；
    /// 如果是枚举，返回值类型为<see cref="int"/>；
    /// 如果是数组，返回值类型为<see cref="IList{T}"/>；
    /// 如果是其它类型，自行约定。
    /// </summary>
    public object? Value { get; }

    public AptAttributeValue(string name, object? value) {
        Name = name;
        Value = value;
    }

    public override string ToString() {
        return $"{nameof(Name)}: {Name}, {nameof(Value)}: {Value}";
    }
}
}