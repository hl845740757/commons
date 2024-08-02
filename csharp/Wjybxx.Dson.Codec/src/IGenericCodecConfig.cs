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
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 泛型类到泛型类的Codec的类型映射。
/// 由于泛型类的Codec不能被直接构造，因此只能先将其类型信息存储下来，待到确定泛型参数类型的时候再构造。
/// 考虑到泛型的反射构建较为复杂，因此我们不采用Type => Factory 的形式来配置，而是配置对应的Codec原型类；
/// 这可能增加类的数量，但代码的复杂度更低，更易于使用。
/// 
/// 注意：
/// 1. 数组和泛型是不同的，数组都对应<see cref="ArrayCodec{T}"/>，因此不需要再这里存储。
/// 2. 在dotnet6/7中不支持泛型协变和逆变，因此 Codec`1[IList`1[string]] 是不能赋值给 Codec`1[List`1[String]]的。
/// </summary>
public interface IGenericCodecConfig
{
    /// <summary>
    /// 获取可以编码目标泛型类的Codec原型 -- 可以向上匹配。
    /// 
    /// 注意：Codec需要和泛型定义类有相同的泛型参数列表。
    /// </summary>
    /// <param name="genericTypeDefine">目标泛型类</param>
    /// <returns></returns>
    Type? GetEncoderType(Type genericTypeDefine);

    /// <summary>
    /// 获取可以解码目标泛型类的Codec原型。
    ///
    /// 注意：Codec需要和泛型定义类有相同的泛型参数列表。
    /// </summary>
    /// <param name="genericTypeDefine">目标泛型类</param>
    /// <returns></returns>
    Type? GetDecoderType(Type genericTypeDefine);
}
}