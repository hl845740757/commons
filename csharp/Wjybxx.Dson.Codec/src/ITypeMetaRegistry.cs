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
using System.Collections.Generic;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型元数据注册表
/// 注意：需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象） —— 在运行时通常不会变化。
/// </summary>
[ThreadSafe]
public interface ITypeMetaRegistry
{
    /// <summary>
    /// 通过类型信息查询类型元数据.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    TypeMeta? OfType(Type type);

    /// <summary>
    /// 通过类型字符串名字查找元数据
    /// </summary>
    /// <param name="clsName"></param>
    /// <returns></returns>
    TypeMeta? OfName(string clsName);
}
}