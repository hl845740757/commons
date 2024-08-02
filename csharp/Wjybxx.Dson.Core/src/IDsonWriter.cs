#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Runtime.CompilerServices;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// 0. Object/Header先写入name再写入value，数组直接写入value。
/// 1. 写数组简单元素的时候，name为字符串类型时，传null或空字符串，name为数字类型时传0；
/// 2. 写数组对象元素时使用无name参数的start方法（实在不想定义太多的方法）；
/// 3. 为减少API数量，我们的所有简单值写入都是带有name参数的，在已经写入name的情况下，接口的name参数将被忽略。
/// 4. double、bool、null由于可以从无符号字符串精确解析得出，因此可以总是不输出类型标签；
/// 5. 内置结构体总是输出类型标签，且总是Flow模式，可以降低使用复杂度；
/// 6. C#由于有值类型，直接使用<see cref="FieldNumber"/>作为name参数，可大幅简化api；
/// </summary>
/// <typeparam name="TName">name的类型，string或<see cref="FieldNumber"/></typeparam>
public interface IDsonWriter<TName> : IDisposable where TName : IEquatable<TName>
{
    /** 刷新写缓冲区 */
    void Flush();

    /// <summary>
    /// 获取当前上下文的类型
    /// </summary>
    DsonContextType ContextType { get; }

    /// <summary>
    /// 获取当前写入的name -- 如果先调用WriteName
    /// </summary>
    TName CurrentName { get; }

    /// <summary>
    /// 当前是否处于等待写入name的状态
    /// </summary>
    bool IsAtName { get; }

    /// <summary>
    /// 编码的时候，用户总是习惯 name和value 同时写入，
    /// 但在写Array或Object容器的时候，不能同时完成，需要先写入name再开始写值。
    /// </summary>
    /// <param name="name"></param>
    void WriteName(TName name);

    #region 简单值

    /// <summary>
    /// 写入一个int值
    /// </summary>
    /// <param name="name">字段的名字</param>
    /// <param name="value">要写入的值</param>
    /// <param name="wireType">数字的二进制编码类型</param>
    /// <param name="style">数字的文本编码类型</param>
    void WriteInt32(TName name, int value, WireType wireType, INumberStyle style);

    void WriteInt64(TName name, long value, WireType wireType, INumberStyle style);

    void WriteFloat(TName name, float value, INumberStyle style);

    void WriteDouble(TName name, double value, INumberStyle style);

    void WriteBool(TName name, bool value);

    void WriteString(TName name, string value, StringStyle style = StringStyle.Auto);

    void WriteNull(TName name);

    void WriteBinary(TName name, Binary binary);

    void WriteBinary(TName name, DsonChunk chunk);

    void WritePtr(TName name, in ObjectPtr objectPtr);

    void WriteLitePtr(TName name, in ObjectLitePtr objectLitePtr);

    void WriteDateTime(TName name, in ExtDateTime dateTime);

    void WriteTimestamp(TName name, in Timestamp timestamp);

    // 快捷方法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteInt32(TName name, int value, WireType wireType) {
        WriteInt32(name, value, wireType, NumberStyles.Typed); // 默认需要打印类型，才能精确解析
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteInt64(TName name, long value, WireType wireType) {
        WriteInt64(name, value, wireType, NumberStyles.Typed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteFloat(TName name, float value) {
        WriteFloat(name, value, NumberStyles.Typed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteDouble(TName name, double value) {
        WriteDouble(name, value, NumberStyles.Simple);
    }

    #endregion

    #region 容器

    void WriteStartArray(ObjectStyle style = ObjectStyle.Indent);

    void WriteEndArray();

    void WriteStartObject(ObjectStyle style = ObjectStyle.Indent);

    void WriteEndObject();

    /// <summary>
    /// Header应该保持简单，因此通常应该使用Flow模式
    /// </summary>
    /// <param name="style">文本格式</param>
    void WriteStartHeader(ObjectStyle style = ObjectStyle.Flow);

    void WriteEndHeader();

    /// <summary>
    /// 开始写一个数组
    /// 1.数组内元素没有名字，因此name传 null、空字符串、0 即可
    /// <code>
    ///    writer.WriteStartArray(name, ObjectStyle.INDENT);
    ///    for (String coderName: coderNames) {
    ///        writer.WriteString(null, coderName);
    ///    }
    ///    writer.WriteEndArray();
    /// </code>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="style"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartArray(TName name, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartArray(style);
    }

    /// <summary>
    /// 开始写一个普通对象
    /// <code>
    ///    writer.WriteStartObject(name, ObjectStyle.INDENT);
    ///    writer.WriteString("name", "wjybxx")
    ///    writer.WriteInt32("age", 28)
    ///    writer.WriteEndObject();
    /// </code>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="style"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartObject(TName name, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartObject(style);
    }

    #endregion

    #region 特殊接口

    /// <summary>
    /// 写入一个简单对象头 -- 仅有一个clsName属性的header。
    /// 1.该接口是为<see cref="DsonTextWriter"/>定制的，以支持简写。
    /// 2.对于其它Writer，则等同于普通写入。
    /// </summary>
    /// <param name="clsName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        IDsonWriter<string> textWrite = (IDsonWriter<string>)this;
        textWrite.WriteStartHeader();
        textWrite.WriteString(DsonHeaders.Names_ClassName, clsName, StringStyle.AutoQuote);
        textWrite.WriteEndHeader();
    }

    /// <summary>
    /// 直接写入一个已编码的字节数组
    /// 1.请确保合法性
    /// 2.支持的类型与读方法相同
    /// 
    /// </summary>
    /// <param name="name">字段名字</param>
    /// <param name="type">DsonType</param>
    /// <param name="data">DsonReader.ReadValueAsBytes 读取的数据</param>
    void WriteValueBytes(TName name, DsonType type, byte[] data);

#nullable disable
    /// <summary>
    /// 附近一个数据到当前上下文
    /// </summary>
    /// <param name="userData">用户自定义数据</param>
    /// <returns>旧值</returns>
    object Attach(object userData);

    /// <summary>
    /// 获取附加到当前上下文的数据
    /// </summary>
    /// <returns></returns>
    object Attachment();

    #endregion
}
}