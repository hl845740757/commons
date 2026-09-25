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

    int ReadInt(string name, DeserializeFeatures features = 0);

    long ReadLong(string name, DeserializeFeatures features = 0);

    float ReadFloat(string name, DeserializeFeatures features = 0);

    double ReadDouble(string name, DeserializeFeatures features = 0);

    Fxp64 ReadFxp64(string name, DeserializeFeatures features = 0);

    bool ReadBool(string name, DeserializeFeatures features = 0);

    string ReadString(string name, DeserializeFeatures features = 0);

    void ReadNull(string name);

    byte[]? ReadBytes(string name, DeserializeFeatures features = 0);

    Binary? ReadBinary(string name, DeserializeFeatures features = 0);

    ObjectPtr ReadPtr(string name);

    DateTime ReadDateTime(string name);

    // ExtDateTime并不常见
    ExtDateTime ReadExtDateTime(string name);

    Timestamp ReadTimestamp(string name);

    Double4 ReadDouble4(string name);

    Long4 ReadLong4(string name);

    Fxp4 ReadFxp4(string name);

    // Enum接口未对泛型做限制，目的是支持任意非多态类型
    T ReadEnum<T>(string name, DeserializeFeatures features = 0);

    // List/Dictionary用于简化生成器代码
    List<T>? ReadList<T>(string name, DeserializeFeatures features = 0);

    Dictionary<K, V>? ReadDictionary<K, V>(string name, DeserializeFeatures features = 0);

    #endregion

    #region 基础值-无name版

    int ReadInt(DeserializeFeatures features = 0);

    long ReadLong(DeserializeFeatures features = 0);

    float ReadFloat(DeserializeFeatures features = 0);

    double ReadDouble(DeserializeFeatures features = 0);

    Fxp64 ReadFxp64(DeserializeFeatures features = 0);

    bool ReadBool(DeserializeFeatures features = 0);

    string ReadString(DeserializeFeatures features = 0);

    void ReadNull();

    byte[]? ReadBytes(DeserializeFeatures features = 0);

    Binary ReadBinary(DeserializeFeatures features = 0);


    ObjectPtr ReadPtr();

    DateTime ReadDateTime();

    // ExtDateTime并不常见
    ExtDateTime ReadExtDateTime();

    Timestamp ReadTimestamp();

    Double4 ReadDouble4();

    Long4 ReadLong4();

    Fxp4 ReadFxp4();

    // Enum接口未对泛型做限制，目的是支持任意非多态类型
    T ReadEnum<T>(DeserializeFeatures features = 0);

    // List/Dictionary用于简化生成器代码
    List<T>? ReadList<T>(DeserializeFeatures features = 0);

    Dictionary<K, V>? ReadDictionary<K, V>(DeserializeFeatures features = 0);

    #endregion

    #region object

    /// <summary>
    /// 从输入流中读取任意类型对象
    /// 
    /// 1.该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
    /// 2.目标类型可以与写入类型不一致，甚至无继承关系，只要数据格式兼容即可 —— 投影。
    /// 3.如果声明类型是<see cref="DsonValue"/>类型，将保留对象头信息。
    /// 4.由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="features">反序列化特征值</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    T ReadObject<T>(string name, DeserializeFeatures features = 0);

    /// <summary>
    /// 从输入流中读取任意类型对象
    /// </summary>
    T ReadObject<T>(DeserializeFeatures features = 0);

    // 非泛型接口用于类似反射这类无法进行类型转换的的场景
    object ReadObject(string name, Type declaredType, DeserializeFeatures features = 0);

    object ReadObject(Type declaredType, DeserializeFeatures features = 0);

    #endregion

    #region 流程

    IDsonConverter Converter { get; }
    ConverterOptions Options { get; }
    ITypeMetaRegistry TypeMetaRegistry { get; }
    IDsonCodecRegistry CodecRegistry { get; }

    /// <summary>
    /// 读取下一个数据的类型
    /// </summary>
    /// <returns></returns>
    DsonType ReadDsonType();

    /// <summary>
    /// 读取下一个值的名字
    /// </summary>
    /// <returns></returns>
    string ReadName();

    /// <summary>
    /// 读取下一个值的名字，名字不匹配时抛出异常
    /// </summary>
    /// <param name="name">期望的字段名</param>
    void ReadName(string? name);

    DsonType CurrentDsonType { get; }

    string CurrentName { get; }

    /// <summary>
    /// 虽然目前来看，encoderType(TypeMeta)并非必要属性，但还是建议用户正确传入
    /// </summary>
    /// <param name="encoderType">类型信息，用于嵌套对象获取信息</param>
    /// <param name="features">反序列化特征值</param>
    SerializeHeader ReadStartObject(Type encoderType, DeserializeFeatures features = 0);

    SerializeHeader ReadStartObject(TypeMeta? typeMeta, DeserializeFeatures features = 0);

    void ReadEndObject();

    SerializeHeader ReadStartArray(Type encoderType, DeserializeFeatures features = 0);

    SerializeHeader ReadStartArray(TypeMeta typeMeta, DeserializeFeatures features = 0);

    void ReadEndArray();

    void SkipName();

    void SkipValue();

    void SkipToEndOfObject();

    byte[] ReadValueAsBytes(string name);

    /// <summary>
    /// 延迟解析引用
    ///
    /// 注：通过<see cref="IDsonCodec.SetField"/>注入最终引用。
    /// </summary>
    /// <param name="codec">注入回调</param>
    /// <param name="inst">目标实例</param>
    /// <param name="fieldName">目标字段</param>
    /// <param name="ptr">目标指针</param>
    void DeferReference(IDsonCodec codec, object inst, string fieldName, int ptr);

    /// <summary>
    /// 延迟转换为目标类型
    ///
    /// 1.暂不适用于被序列化为引用的字段（暂时只用于List和Map）。
    /// 2.也通过<see cref="IDsonCodec.SetField"/>注入最终引用。
    /// 3.该方法通过Type查询共享的转换函数。
    /// </summary>
    /// <param name="codec">注入回调</param>
    /// <param name="inst">目标实例</param>
    /// <param name="fieldName">目标字段</param>
    /// <param name="tempValue">临时值</param>
    /// <param name="targetType">目标类型</param>
    void DeferToTargetType(IDsonCodec codec, object inst, string fieldName,
                           object tempValue, Type targetType);

    /// <summary>
    /// 延迟转换为目标类型
    /// </summary>
    /// <param name="codec">注入回调</param>
    /// <param name="inst">目标实例</param>
    /// <param name="fieldName">目标字段</param>
    /// <param name="tempValue">临时值</param>
    /// <param name="func">转换函数</param>
    void DeferToTargetType(IDsonCodec codec, object inst, string fieldName,
                           object tempValue, Func<object, object> func);

    /// <summary>
    /// 延迟执行目标对象的<see cref="IDsonCodec.AfterDecode"/>方法。
    /// </summary>
    /// <param name="codec">注入回调</param>
    /// <param name="inst">目标实例</param>
    void DeferInvokeAfterDecode(IDsonCodec codec, object inst);

    /// <summary>
    /// 发布引用
    /// 
    /// 注：Codec应该在创建实例以后立刻发布，以避免循环依赖时出现错误。
    /// </summary>
    void PublishReference<T>(T reference); // TODO 删除

    /// <summary>
    /// 获取当前容器的类型元数据
    ///
    /// 注：
    /// 1.如果当前是顶层对象，则为null；
    /// 2.如果用户在ReadStartObject/ReadStartArray方法时没有传入类型信息，则为null。
    /// </summary>
    /// <value></value>
    TypeMeta? ContainerTypeMeta { get; }

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