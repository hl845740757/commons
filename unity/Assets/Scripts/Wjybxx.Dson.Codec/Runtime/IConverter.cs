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
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// 1.由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入。
/// 2.Converter接口去除了泛型相关的接口，这有利于减少API。
/// 这并不会对效率产生影响，因为Converter接收的对象绝大多数情况下都是Class，鲜有Struct。
/// </summary>
public interface IConverter
{
    /// <summary>
    /// 将一个对象转换为字节数组
    /// 
    /// 注意：如果对象的运行时类型和声明类型一致，则可省去编码结果中的类型信息。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">序列化特征值</param>
    /// <returns></returns>
    byte[] Write(object value, Type declaredType, SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取一个对象
    /// 
    /// 注意：如果对象的声明类型和写入的类型不兼容，则表示投影；factory用于支持将数据读取到既有实例或子类实例上。
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">反序列特征值</param>
    /// <param name="factory">对象工厂</param>
    /// <returns></returns>
    object Read(byte[] source, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    /// <summary>
    /// 将一个对象转换为字节数组
    /// 
    /// 注意：写入的字节数回设置到<see cref="DsonChunk"/>
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="chunk">二进制块</param>
    /// <param name="features">特征值</param>
    void Write(object value, Type declaredType, DsonChunk chunk, SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取一个对象
    ///
    /// 注意：读取的字节数会设置到<see cref="DsonChunk"/>
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">反序列特征值</param>
    /// <param name="factory">对象工厂</param>
    /// <returns></returns>
    object Read(DsonChunk source, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    #region Clone

    /// <summary>
    /// 克隆一个实例
    /// 1. 返回值的类型不一定和原始对象相同，这通常发生在集合对象上 —— 也可能是投影。
    /// 2. 如果Codec存在lazyDecode，也会导致不同
    /// </summary>
    /// <param name="value">要克隆的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="factory">返回对象类型工厂</param>
    /// <returns></returns>
    object CloneObject(object? value, Type declaredType, Type targetType, Func<object>? factory = null);

    #endregion
}
}