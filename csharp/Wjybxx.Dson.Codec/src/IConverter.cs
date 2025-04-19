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
using System.Runtime.CompilerServices;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 1.由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
/// 2.非泛型接口用于反射等统一API。
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
    /// <typeparam name="T">运行时类型，避免装箱</typeparam>
    /// <returns></returns>
    byte[] Write<T>(in T value, Type declaredType);

    /// <summary>
    /// 从数据源中读取一个对象
    /// 
    /// 注意：如果对象的声明类型和写入的类型不兼容，则表示投影；factory用于支持将数据读取到既有实例或子类实例上。
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType"></param>
    /// <param name="factory">对象工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns></returns>
    T Read<T>(byte[] source, Type declaredType, Func<T>? factory = null);

    /// <summary>
    /// 将一个对象转换为字节数组
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="chunk">二进制块，写入的字节数设置到<see cref="DsonChunk"/></param>
    /// <typeparam name="T">对象的声明类型，可以是value的超类</typeparam>
    /// <returns>序列化结果</returns>
    void Write<T>(in T value, Type declaredType, DsonChunk chunk);

    /// <summary>
    /// 从数据源中读取一个对象
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType"></param>
    /// <param name="factory">对象工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns></returns>
    T Read<T>(DsonChunk source, Type declaredType, Func<T>? factory = null);

    #region Clone

    /// <summary>
    /// 克隆一个实例
    /// </summary>
    /// <param name="value">要克隆的对象</param>
    /// <param name="factory">返回对象类型工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns></returns>
    T CloneObject<T>(T? value, Func<T>? factory = null);

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