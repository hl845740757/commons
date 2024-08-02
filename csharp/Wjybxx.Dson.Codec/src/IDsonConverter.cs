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
using System.IO;
using System.Runtime.CompilerServices;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public interface IDsonConverter : IConverter
{
    #region convert

    /// <summary>
    /// 将一个对象转换为字节数组
    /// 
    /// 注意：如果对象的运行时类型和声明类型一致，则可省去编码结果中的类型信息。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="style">缩进格式</param>
    /// <typeparam name="T">运行时类型，避免装箱</typeparam>
    /// <returns></returns>
    string WriteAsDson<T>(in T value, Type declaredType, ObjectStyle? style = null);

    /// <summary>
    /// 从数据源中读取一个对象
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="factory">实例工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns></returns>
    T ReadFromDson<T>(string source, Func<T>? factory = null);

    /// <summary>
    /// 将一个对象写入Writer
    /// (默认不关闭writer)
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="writer">接收输出</param>
    /// <param name="style">缩进格式</param>
    /// <typeparam name="T">运行时类型，避免装箱</typeparam>
    void WriteAsDson<T>(in T value, Type declaredType, TextWriter writer, ObjectStyle? style = null);

    /// <summary>
    /// 从数据源中读取一个对象
    /// (默认不关闭Reader)
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="factory">实例工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    T ReadFromDson<T>(TextReader source, Func<T>? factory = null);

    /// <summary>
    /// 将一个对象写为<see cref="DsonObject{TK}"/>或<see cref="DsonArray{TK}"/>
    /// </summary>
    /// <param name="value">顶层对象必须的容器对象，Object和数组</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <typeparam name="T">运行时类型，避免装箱</typeparam>
    /// <returns></returns>
    DsonValue WriteAsDsonValue<T>(in T value, Type declaredType);

    /// <summary>
    /// 从数据源中读取一个对象
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="factory">实例工厂</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    T ReadFromDsonValue<T>(DsonValue source, Func<T>? factory = null);

    /// <summary>
    /// 将Dson源解码为DsonValue中间对象 -- 只读取一个顶层对象。
    /// 外部可以保存该对象，以提高重复反序列化的效率。
    /// (默认不关闭Reader)
    /// </summary>
    /// <returns></returns>
    DsonValue ReadAsDsonValue(TextReader source);

    /// <summary>
    /// 将Dson源解码为DsonValue中间对象 -- 读取全部数据，header存储在外层容器(DsonArray)上。
    /// 外部可以保存该对象，以提高重复反序列化的效率。
    /// </summary>
    /// <returns></returns>
    DsonArray<string> ReadAsDsonCollection(TextReader chunk);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string WriteAsDson<T>(in T value, ObjectStyle? style = null) {
        return WriteAsDson(in value, typeof(T), style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteAsDson<T>(in T value, TextWriter writer, ObjectStyle? style = null) {
        WriteAsDson(in value, typeof(T), writer, style);
    }

    #endregion

    #region other

    /// <summary>
    /// 序列化选项
    /// </summary>
    ConverterOptions Options { get; }

    /// <summary>
    /// 类型源数据注册表
    /// </summary>
    ITypeMetaRegistry TypeMetaRegistry { get; }

    /// <summary>
    /// Codec注册表
    /// </summary>
    IDsonCodecRegistry CodecRegistry { get; }

    /// <summary>
    /// 在共享其它属性的情况，创建一个持有给定options的Converter。
    /// 我们通过options来控制Converter的上下文。
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    IDsonConverter WithOptions(ConverterOptions options);

    #endregion
}
}