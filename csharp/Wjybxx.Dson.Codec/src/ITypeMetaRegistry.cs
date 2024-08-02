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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型元数据注册表
/// 注意：
/// 1. 必须保证同一个类在所有机器上的映射结果是相同的，这意味着你应该基于名字映射，而不能直接使用class对象的hash值。
/// 2. 一个类型的名字和唯一标识应尽量是稳定的，即同一个类的映射值在不同版本之间是相同的。
/// 3. id和类型之间应当是唯一映射的。
/// 4. 需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象） —— 在运行时通常不会变化。
///
/// <h3>泛型</h3>
/// 用户在初始化Registry时无需处理泛型类的TypeMeta，底层会动态生成对应的TypeMeta，用户只需要保证使用到的所有原始类型都注册了。
/// </summary>
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

    TypeMeta CheckedOfType(Type type) {
        TypeMeta? typeMeta = OfType(type);
        if (typeMeta == null) {
            throw new DsonCodecException("type is absent, type " + type);
        }
        return typeMeta;
    }

    TypeMeta CheckedOfName(string clsName) {
        TypeMeta? typeMeta = OfName(clsName);
        if (typeMeta == null) {
            throw new DsonCodecException("type is absent, clsName " + clsName);
        }
        return typeMeta;
    }

    /// <summary>
    /// 将包含的所有类型信息导出。
    /// 该方法主要用于聚合多个Registry为单个Registry，以提高查询效率。
    /// </summary>
    /// <returns></returns>
    List<TypeMeta> Export();
}
}