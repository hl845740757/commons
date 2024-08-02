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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// <see cref="IDsonCodec{T}"/>的注册表
/// 由于C#的真泛型，因此注册表通常不能实现为不可变的 —— 除非在运行前就知道所有的类型，提前注册。
/// </summary>
public interface IDsonCodecRegistry
{
    /// <summary>
    /// 获取类型对应的编解码器。
    /// 
    /// 注意：
    /// 1. 返回的Encoder可能是T的超类型的Codec，C#是真实泛型，dotnet6/7不支持泛型逆变，因此这里不定义为泛型方法。
    /// 2. 对于泛型类，需要动态创建其对应的Codec。
    /// 
    /// <code>
    ///   DsonCodecImpl.GetEncoderClass().IsAssignableFrom(type)
    /// </code>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rootRegistry">用于转换为查找父类Encoder</param>
    /// <returns></returns>
    DsonCodecImpl? GetEncoder(Type type, IDsonCodecRegistry rootRegistry);

    /// <summary>
    /// 获取类型对应的解码器。
    /// 
    /// 注意：
    /// 解码器必须目标类型一致，子类Codec不能安全解码超类数据，超类Codec返回的实例不能向下转型。
    /// <code>
    ///   DsonCodecImpl.GetEncoderClass() == type
    /// </code>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rootRegistry">用于转换为查找子类Decoder</param>
    /// <returns></returns>
    DsonCodecImpl? GetDecoder(Type type, IDsonCodecRegistry rootRegistry);
}
}