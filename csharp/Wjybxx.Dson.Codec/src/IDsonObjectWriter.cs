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

    /** 该方法默认会拷贝value，如果不想拷贝，可转为Binary */
    void WriteBytes(string? name, byte[]? value);

    void WriteBinary(string? name, Binary binary);

    void WriteBinary(string? name, DsonChunk chunk);

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
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="value">要写入的对象，GetType获取实际类型</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="style">如果为null，则表示使用对象对象默认的文本编码样式</param>
    /// <typeparam name="T">避免拆装箱</typeparam>
    void WriteObject<T>(string? name, in T? value, Type declaredType, ObjectStyle? style = null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteObject<T>(string? name, in T? value) {
        WriteObject(name, value, typeof(T));
    }

    #endregion

    #region 流程

    ConverterOptions Options { get; }

    string CurrentName { get; }

    void WriteName(string name);

    void WriteStartObject<T>(in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent);

    void WriteEndObject();

    void WriteStartArray<T>(in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent); // 数组一定是class，in修饰不是必须的

    void WriteEndArray();

    void WriteValueBytes(string name, DsonType dsonType, byte[] data);

    /// <summary>
    /// 编码字典的key
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T">key的声明类型，避免装箱</typeparam>
    /// <returns></returns>
    string EncodeKey<T>(T key);

    /// <summary>
    /// 打印换行，用于控制Dson文本的样式
    /// </summary>
    void Println();

    void Flush();

    // defaults
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartObject<T>(string name, in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartObject(in value, declaredType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteStartArray<T>(string name, in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent) {
        WriteName(name);
        WriteStartArray(value, declaredType, style);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUint(string? name, uint value, WireType wireType = WireType.Uint) {
        WriteInt(name, (int)value, wireType, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUint(string? name, uint value, WireType wireType, INumberStyle style) {
        WriteInt(name, (int)value, wireType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUlong(string? name, ulong value, WireType wireType = WireType.Uint) {
        WriteLong(name, (long)value, wireType, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteUlong(string? name, ulong value, WireType wireType, INumberStyle style) {
        WriteLong(name, (long)value, wireType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteShort(string? name, short value, WireType wireType = WireType.VarInt) {
        WriteInt(name, value, wireType, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteShort(string? name, short value, WireType wireType, INumberStyle style) {
        WriteInt(name, value, wireType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteByte(string? name, byte value, WireType wireType = WireType.VarInt) {
        WriteInt(name, value, wireType, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteByte(string? name, byte value, WireType wireType, INumberStyle style) {
        WriteInt(name, value, wireType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteChar(string? name, char value, WireType wireType = WireType.Uint) {
        WriteInt(name, value, wireType, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteChar(string? name, char value, WireType wireType, INumberStyle style) {
        WriteInt(name, value, wireType, style);
    }

    #endregion
}
}