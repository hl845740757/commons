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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 1. 读取数组内普通成员时，name传null或零值，读取嵌套对象时使用无name参数的start方法
/// 2. 为减少API数量，我们的所有简单值读取都是带有name参数的，在已读取name的情况下，接口的name参数将被忽略。
/// </summary>
public interface IDsonObjectReader : IDisposable
{
    #region 简单值

    int ReadInt(string? name);

    long ReadLong(string? name);

    float ReadFloat(string? name);

    double ReadDouble(string? name);

    bool ReadBool(string? name);

    string ReadString(string? name);

    void ReadNull(string? name);

    byte[]? ReadBytes(string? name) {
        Binary binary = ReadBinary(name);
        return binary.UnsafeBuffer;
    }

    Binary ReadBinary(string? name);

    ObjectPtr ReadPtr(string? name);

    ObjectLitePtr ReadLitePtr(string? name);

    DateTime ReadDateTime(string? name);

    // ExtDateTime并不常见
    ExtDateTime ReadExtDateTime(string? name);

    Timestamp ReadTimestamp(string? name);

    #endregion

    #region object

    /// <summary>
    /// 从输入流中读取一个对象
    /// 注意：
    /// 1. 该方法对于无法精确解析的对象，可能返回一个不兼容的类型。
    /// 2. 目标类型可以与写入类型不一致，甚至无继承关系，只要数据格式兼容即可 —— 投影。
    /// 3. 由于声明类型并不能总是通过泛型参数获取，因此需要外部显式传入 —— 反射。
    /// </summary>
    /// <param name="name">字段的名字，数组元素和顶层对象的name可为null或空字符串</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="factory">对象工厂，创建的实例必须是声明类型的子类型</param>
    /// <typeparam name="T">返回值类型，避免装箱</typeparam>
    /// <returns></returns>
    T ReadObject<T>(string? name, Type declaredType, Func<T>? factory = null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    T ReadObject<T>(string? name, Func<T>? factory = null) {
        return ReadObject<T>(name, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object ReadObject(string? name, Type declaredType, Func<object>? factory = null) {
        return ReadObject<object>(name, declaredType, factory);
    }

    #endregion

    #region 流程

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
    /// 如果reader不支持随机读，当名字不匹配下一个值时将抛出异常。
    /// 返回false的情况下，可继续调用该方法或{@link #readDsonType()}读取下一个字段。
    /// </summary>
    /// <param name="name"></param>
    /// <returns>如果是Object上下文，如果字段存在则返回true，否则返回false；如果是Array上下文，如果尚未到达数组尾部，则返回true，否则返回false。</returns>
    bool ReadName(string? name);

    DsonType CurrentDsonType { get; }

    string CurrentName { get; }

    void ReadStartObject();

    void ReadEndObject();

    void ReadStartArray();

    void ReadEndArray();

    void SkipName();

    void SkipValue();

    void SkipToEndOfObject();

    byte[] ReadValueAsBytes(string name);

    /// <summary>
    /// 解码字典的key。
    /// 外部可以判断Key的类型，以避免拆装箱。
    /// </summary>
    /// <param name="keyString">字符串形式的key</param>
    /// <typeparam name="T">key的声明类型</typeparam>
    /// <returns>期望的结果类型</returns>
    T DecodeKey<T>(string keyString);

    /// <summary>
    /// 设置数组/object的value的类型，用于精确解析Dson文本。
    /// </summary>
    /// <param name="dsonType">value的类型</param>
    void SetComponentType(DsonType dsonType);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">字段的名字</param>
    /// <returns>如果存在对应的字段则返回true</returns>
    bool ReadStartObject(string? name) {
        if (ReadName(name)) {
            ReadStartObject();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">字段的名字</param>
    /// <returns>如果存在对应的字段则返回true</returns>
    bool ReadStartArray(string? name) {
        if (ReadName(name)) {
            ReadStartArray();
            return true;
        }
        return false;
    }

    #endregion

    #region 快捷方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    short ReadShort(string? name) {
        return (short)ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte ReadByte(string? name) {
        return (byte)ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    char ReadChar(string? name) {
        return (char)ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint ReadUint(string? name) {
        return (uint)ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ulong ReadUlong(string? name) {
        return (ulong)ReadLong(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ushort ReadUshort(string? name) {
        return (ushort)ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sbyte ReadSbyte(string? name) {
        return (sbyte)ReadInt(name);
    }

    #endregion
}
}