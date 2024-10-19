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
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 为减少API数量，我们的所有简单值写入都是带有name参数的，在已经写入name的情况下，接口的name参数将被忽略。
/// </summary>
public interface IDsonObjectWriter : IDisposable
{
    #region 基础api

    void WriteInt(string? name, int value, WireType wireType, INumberStyle style);

    void WriteLong(string? name, long value, WireType wireType, INumberStyle style);

    void WriteFloat(string? name, float value, INumberStyle style);

    void WriteDouble(string? name, double value, INumberStyle style);

    void WriteBool(string? name, bool value);

    void WriteString(string? name, string? value, StringStyle style = StringStyle.Auto);

    void WriteNull(string? name);

    /** bytes默认为不可共享对象 -- 如果不期望拷贝，可先包装为Binary */
    void WriteBytes(string? name, byte[]? bytes);

    void WriteBytes(string? name, byte[] bytes, int offset, int len);

    /** Binary默认为可共享对象 */
    void WriteBinary(string? name, Binary binary);

    // 内建结构体
    void WritePtr(string? name, in ObjectPtr objectPtr);

    void WriteLitePtr(string? name, in ObjectLitePtr objectLitePtr);

    void WriteDateTime(string? name, in DateTime dateTime);

    // ExtDateTime并不常见
    void WriteExtDateTime(string? name, in ExtDateTime dateTime);

    void WriteTimestamp(string? name, in Timestamp timestamp);

    #endregion

    #region object

    /// <summary>
    /// 写嵌套对象
    /// 1.由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="value">要写入的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="style">如果为null，则表示使用对象对象默认的文本编码样式</param>
    /// <typeparam name="T">避免拆装箱</typeparam>
    void WriteObject<T>(string? name, in T? value, Type declaredType, ObjectStyle? style = null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteObject<T>(string? name, in T? value, ObjectStyle? style = null) {
        WriteObject(name, value, typeof(T), style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteObject(string? name, object? value, Type declaredType, ObjectStyle? style = null) {
        WriteObject<object>(name, value, declaredType, style);
    }

    #endregion

    #region 流程

    ConverterOptions Options { get; }

    string CurrentName { get; }

    void WriteName(string name);

    /// <summary>
    /// 写入类型信息
    /// 该方法应当在writeStartObject/Array后立即调用，写在所有字段之前。
    /// </summary>
    /// <param name="encoderType">编码器绑定的类型，要写入的类型信息</param>
    /// <param name="declaredType">对象的声明类型，用于测试是否写入类型信息</param>
    void WriteTypeInfo(Type encoderType, Type declaredType);

    void WriteStartObject(ObjectStyle style);

    void WriteEndObject();

    void WriteStartArray(ObjectStyle style);

    void WriteEndArray();

    /** 写入已编码的二进制数据 */
    void WriteValueBytes(string name, DsonType dsonType, byte[] data);

    /// <summary>
    /// 编码字典的key
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T">key的声明类型，避免装箱</typeparam>
    /// <returns></returns>
    string EncodeKey<T>(T key);

    void Flush();

    // defaults
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartObject(ObjectStyle style, Type encoderType, Type declaredType) {
        WriteStartObject(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartObject(string name, ObjectStyle style) {
        WriteName(name);
        WriteStartObject(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartObject(string name, ObjectStyle style, Type encoderType, Type declaredType) {
        WriteName(name);
        WriteStartObject(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartArray(ObjectStyle style, Type encoderType, Type declaredType) {
        WriteStartArray(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartArray(string name, ObjectStyle style) {
        WriteName(name);
        WriteStartArray(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartArray(string name, ObjectStyle style, Type encoderType, Type declaredType) {
        WriteName(name);
        WriteStartArray(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    #endregion

    #region 快捷方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteInt(string? name, int value, WireType wireType = WireType.VarInt) {
        WriteInt(name, value, wireType, NumberStyles.Simple); // 这里使用simple -- 外部通常包含明确类型
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteLong(string? name, long value, WireType wireType = WireType.VarInt) {
        WriteLong(name, value, wireType, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteFloat(string? name, float value) {
        WriteFloat(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteDouble(string? name, double value) {
        WriteDouble(name, value, NumberStyles.Simple);
    }

    // short等不再允许指定WireType
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteShort(string? name, short value) {
        WriteInt(name, value, WireType.Sint, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteShort(string? name, short value, INumberStyle style) {
        WriteInt(name, value, WireType.Sint, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteByte(string? name, byte value) {
        WriteInt(name, value, WireType.Uint, NumberStyles.Simple); // c#的byte是无符号整数，sbyte才是有符号整数
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteByte(string? name, byte value, INumberStyle style) {
        WriteInt(name, value, WireType.Uint, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteChar(string? name, char value) {
        WriteInt(name, value, WireType.Uint, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteChar(string? name, char value, INumberStyle style) {
        WriteInt(name, value, WireType.Uint, style);
    }

    // unsigned
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUint(string? name, uint value) {
        WriteInt(name, (int)value, WireType.Uint, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUint(string? name, uint value, INumberStyle style) {
        WriteInt(name, (int)value, WireType.Uint, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUlong(string? name, ulong value) {
        WriteLong(name, (long)value, WireType.Uint, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUlong(string? name, ulong value, INumberStyle style) {
        WriteLong(name, (long)value, WireType.Uint, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUShort(string? name, ushort value) {
        WriteInt(name, value, WireType.Uint, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUShort(string? name, ushort value, INumberStyle style) {
        WriteInt(name, value, WireType.Uint, style);
    }

    #endregion
}
}