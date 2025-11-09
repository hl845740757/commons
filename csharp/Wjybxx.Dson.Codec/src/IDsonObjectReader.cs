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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 1. Object/Header先读name再读value，数组直接读value。
/// 2. 已读取name的情况下，使用包含name的方法，name将被忽略。
/// </summary>
public interface IDsonObjectReader : IDisposable
{
    #region 基础值

    int ReadInt(string name);

    long ReadLong(string name);

    float ReadFloat(string name);

    double ReadDouble(string name);

    bool ReadBool(string name);

    string ReadString(string name);

    void ReadNull(string name);

    byte[]? ReadBytes(string name);

    Binary? ReadBinary(string name);

    ObjectPtr ReadPtr(string name);

    DateTime ReadDateTime(string name);

    // ExtDateTime并不常见
    ExtDateTime ReadExtDateTime(string name);

    Timestamp ReadTimestamp(string name);

    #endregion

    #region 基础值-无name版

    int ReadInt();

    long ReadLong();

    float ReadFloat();

    double ReadDouble();

    bool ReadBool();

    string ReadString();

    void ReadNull();

    byte[]? ReadBytes() {
        Binary binary = ReadBinary();
        return binary.UnsafeBuffer;
    }

    Binary ReadBinary();

    ObjectPtr ReadPtr();

    DateTime ReadDateTime();

    // ExtDateTime并不常见
    ExtDateTime ReadExtDateTime();

    Timestamp ReadTimestamp();

    #endregion

    #region object

    /// <summary>
    /// 从输入流中读取一个对象
    /// 注意：
    /// 1. 该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
    /// 2. 目标类型可以与写入类型不一致，甚至无继承关系，只要数据格式兼容即可 —— 投影。
    /// 3. 如果声明类型是的<see cref="DsonValue"/>类型，将保留对象头信息。
    /// 4. 由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="factory">对象工厂，创建的实例必须是声明类型的子类型</param>
    /// <returns></returns>
    object ReadObject(string name, Type declaredType, Func<object>? factory = null);

    /// <summary>
    /// 从输入流中读取一个对象
    /// 
    /// 该方法用于避免结构体类型装箱
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="factory">对象工厂，创建的实例必须是声明类型的子类型</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// </summary>
    T ReadObject<T>(string name, Func<object>? factory = null);

    /// <summary>
    /// 从输入流中读取一个对象
    /// </summary>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="factory">对象工厂，创建的实例必须是声明类型的子类型</param>
    /// <returns></returns>
    object ReadObject(Type declaredType, Func<object>? factory = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象工厂，创建的实例必须是声明类型的子类型</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns></returns>
    T ReadObject<T>(Func<object>? factory = null);

    #endregion

    #region 流程

    IDsonConverter Converter { get; }

    ConverterOptions Options { get; }

    DsonContextType ContextType { get; }

    /// <summary>
    /// 读取下一个数据的类型
    /// </summary>
    /// <returns></returns>
    DsonType ReadDsonType();

    /// <summary>
    /// 读取下一个值的名字
    /// 该方法只能在<see cref="ReadDsonType"/>后调用
    /// </summary>
    /// <returns></returns>
    string ReadName();

    /// <summary>
    /// 读取指定名字的值 -- 可实现随机读
    /// 如果尚未调用<see cref="ReadDsonType"/>，该方法将尝试跳转到该name所在的字段。
    /// 如果已调用<see cref="ReadDsonType"/>，则name必须与下一个name匹配。
    /// 如果已调用<see cref="ReadName()"/>，则name可以为null，否则必须当前name匹配。
    /// 返回false的情况下，可继续调用该方法或<see cref="ReadDsonType"/>读取下一个字段。
    /// </summary>
    /// <param name="name"></param>
    /// <returns>如果是Object上下文，如果字段存在则返回true，否则返回false；如果是Array上下文，如果尚未到达数组尾部，则返回true，否则返回false。</returns>
    bool ReadName(string? name);

    DsonType CurrentDsonType { get; }

    string CurrentName { get; }

    SerializeHeader ReadStartObject();

    void ReadEndObject();

    SerializeHeader ReadStartArray();

    void ReadEndArray();

    void SkipName();

    void SkipValue();

    void SkipToEndOfObject();

    byte[] ReadValueAsBytes(string name);

    /// <summary>
    /// 发布引用
    /// 
    /// 注：Codec应该在创建实例以后立刻发布，以避免循环依赖时出现错误。
    /// </summary>
    void PublishReference<T>(in T reference);

    /// <summary>
    /// 获取当前容器的类型元数据
    ///
    /// 注：如果当前是顶层对象，则返回值为null。
    /// </summary>
    /// <returns></returns>
    TypeMeta? GetContainerTypeMeta();

    /// <summary>
    /// 查询可用于内联编码的Codec
    /// </summary>
    DsonCodecImpl<T>? GetInlinableCodec<T>();

    /// <summary>
    /// 设置是否启用name池化
    /// </summary>
    /// <param name="value"></param>
    void SetEnableNameIntern(bool? value);

    /// <summary>
    /// 设置数组/object的value的类型，用于精确解析Dson文本。
    /// </summary>
    /// <param name="dsonType">value的类型</param>
    void SetComponentType(DsonType dsonType);

    #endregion
}
}