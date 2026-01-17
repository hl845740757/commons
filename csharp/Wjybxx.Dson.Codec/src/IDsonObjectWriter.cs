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
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 1. Object/Header先写入name再写入value，数组直接写入value。
/// 2. 已写入name的情况下，调用包含name的写入value方法时，name将被忽略。
/// 3. 在未写入name的情况下，由<see cref="SerializeFeatures"/>决定是否写入null值和零值。
/// </summary>
public interface IDsonObjectWriter : IDisposable
{
    #region 基础值

    // 这里使用simple -- 外部通常包含明确类型
    void WriteInt(string name, int value, SerializeFeatures features = default);

    void WriteLong(string name, long value, SerializeFeatures features = default);

    void WriteFloat(string name, float value, SerializeFeatures features = default);

    void WriteDouble(string name, double value, SerializeFeatures features = default);

    void WriteBool(string name, bool value, SerializeFeatures features = default);

    void WriteString(string name, string? value, SerializeFeatures features = default);

    /** 如果尚未写入name，则根据features决定是否写入 */
    void WriteNull(string name, SerializeFeatures features = default);

    void WriteBytes(string name, byte[] bytes, int offset, int len);

    /** bytes默认为不可共享对象 -- 如果不期望拷贝，可先包装为Binary */
    void WriteBytes(string name, byte[]? bytes, SerializeFeatures features = default);

    /** Binary默认为可共享对象 - feature用于处理null值*/
    void WriteBinary(string name, Binary? binary, SerializeFeatures features = default);

    // 内建结构体
    void WritePtr(string name, ObjectPtr objectPtr);

    void WriteDateTime(string name, DateTime dateTime);

    // ExtDateTime并不常见
    void WriteExtDateTime(string name, ExtDateTime dateTime);

    void WriteTimestamp(string name, Timestamp timestamp);

    void WriteDouble4(string name, Double4 double4, SerializeFeatures features = default);

    #endregion

    #region 基础值-无name版

    void WriteInt(int value, SerializeFeatures features = default);

    void WriteLong(long value, SerializeFeatures features = default);

    void WriteFloat(float value, SerializeFeatures features = default);

    void WriteDouble(double value, SerializeFeatures features = default);

    void WriteBool(bool value, SerializeFeatures features = default);

    void WriteString(string? value, SerializeFeatures features = default);

    /** 注意：该方法一定会写入null -- 因为已写入name */
    void WriteNull();

    void WriteBytes(byte[] bytes, int offset, int len);

    /** bytes默认为不可共享对象 -- 如果不期望拷贝，可先包装为Binary */
    void WriteBytes(byte[]? bytes, SerializeFeatures features = default);

    /** Binary默认为可共享对象 -- feature用于处理null值 */
    void WriteBinary(Binary? binary, SerializeFeatures features = default);

    // 内建结构体
    void WritePtr(ObjectPtr objectPtr);

    void WriteDateTime(DateTime dateTime);

    // ExtDateTime并不常见
    void WriteExtDateTime(ExtDateTime dateTime);

    void WriteTimestamp(Timestamp timestamp);

    void WriteDouble4(Double4 double4, SerializeFeatures features = default);

    #endregion

    #region object

    /// <summary>
    /// 写嵌套对象
    /// 1.由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
    /// 2.如果尚未写入name且value为null，则根据features决定是否写入null。
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="value">要写入的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">特征值</param>
    void WriteObject(string name, object? value, Type declaredType, SerializeFeatures features = default);

    /// <summary>
    /// 写嵌套对象
    /// 
    /// 该接口用于避免结构体装箱
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="value">要写入的对象</param>
    /// <param name="features">特征值</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    void WriteObject<T>(string name, in T? value, SerializeFeatures features = default);

    /// <summary>
    /// 写嵌套对象
    /// </summary>
    void WriteObject(object? value, Type declaredType, SerializeFeatures features = default);

    /// <summary>
    /// 写嵌套对象
    /// </summary>
    void WriteObject<T>(in T? value, SerializeFeatures features = default);

    #endregion

    #region 流程

    IDsonConverter Converter { get; }
    ConverterOptions Options { get; }
    ITypeMetaRegistry TypeMetaRegistry { get; }
    IDsonCodecRegistry CodecRegistry { get; }

    /// <summary>
    /// 当前字段的名字
    /// </summary>
    string CurrentName { get; }

    /// <summary>
    /// 写入下一个字段的名字
    /// </summary>
    /// <param name="name"></param>
    void WriteName(string name);

    /// <summary>
    /// 虽然目前来看，encoderType(TypeMeta)并非必要属性，但还是建议用户正确传入
    /// </summary>
    /// <param name="encoderType">类型信息，用于嵌套对象获取信息</param>
    /// <param name="features">主要用于计算Style</param>
    void WriteStartObject(Type? encoderType, SerializeFeatures features = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeMeta">类型信息，用于嵌套对象获取信息</param>
    /// <param name="features">主要用于计算Style</param>
    void WriteStartObject(TypeMeta? typeMeta, SerializeFeatures features = default);

    void WriteEndObject();

    void WriteStartArray(Type encoderType, SerializeFeatures features = default);

    void WriteStartArray(TypeMeta typeMeta, SerializeFeatures features = default);

    void WriteEndArray();

    /// <summary>
    /// 写入对象头信息
    /// 
    /// 1.该方法应当在writeStartObject/Array后立即调用。
    /// 2.不写入Header的类型不支持被其它对象引用。
    /// 3.Header不支持自定义内容，因为框架只能解析固定的Header字段。
    /// 4.集合类型注意去除<see cref="SerializeFeatures.WriteTypeName"/>属性。
    /// <param name="encoderType">被编码的类型，不一定等于value的类型，可能是超类类型</param>
    /// <param name="declaredType">声明类型，用于判断是否写入类型信息</param>
    /// <param name="features">序列化特征值</param>
    /// </summary>
    void WriteHeader(Type encoderType, Type declaredType,
                     SerializeFeatures features, SerializeHeader header = default);

    /// <summary>
    /// 当前容器的类型元数据
    /// 
    /// 注：
    /// 1.如果当前是顶层对象，则为null；
    /// 2.如果用户在WriteStartObject/WriteStartArray方法时没有传入类型信息，则为null。
    /// </summary>
    TypeMeta? ContainerTypeMeta { get; }

    /// <summary>
    /// 查询可用于内联编码的Codec（用于集合加速）
    /// </summary>
    DsonCodecImpl<T>? GetInlinableCodec<T>();

    /// <summary>
    /// 写入已编码的二进制数据
    /// </summary>
    void WriteValueBytes(string name, DsonType dsonType, byte[] data);

    void Flush();

    #endregion
}
}