#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 定义一组要自动生成Codec的类
/// (表示当前类是一个配置文件)
///
/// 1.每一个字段表示一个需要序列化的类型。
/// 2.需要为目标类型定义特殊属性时，可使用<see cref="DsonCodecLinkerAttribute"/>注解。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[Serializable]
public sealed class DsonCodecLinkerGroupAttribute : Attribute
{
    /** 生成类的命名空间 -- 默认为配置类的命名空间 */
    public string? OutputNamespace { get; }

    public DsonCodecLinkerGroupAttribute(string? outputNamespace = null) {
        OutputNamespace = outputNamespace;
    }
}
}