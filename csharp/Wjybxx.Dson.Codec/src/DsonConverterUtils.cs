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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
///
/// 1.将接口中的默认方法移至Util类，可以避免虚方法调用
/// 2.方法分为泛型版和非泛型版，非泛型版主要用于处理支持反射调用。
/// </summary>
[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
public static class DsonConverterUtils
{
    #region util

    /** 注意：默认情况下字典应该是一个数组对象，而不是普通的对象 */
    public static bool IsEncodeAsArray(Type encoderClass) {
        // c#不能直接测试是否是某个泛型原型的子类，好在字典也实现了IEnumerable，字典默认也需要编码为数组
        return encoderClass.IsArray || IsCollection(encoderClass, true);
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="ICollection{T}"/>类型
    /// </summary>
    /// <param name="type">要测试的类型</param>
    /// <param name="includeDictionary">是否包含字典类型</param>
    /// <returns></returns>
    public static bool IsCollection(Type type, bool includeDictionary = false) {
        Type typeOfCollection = typeof(ICollection<>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfCollection) {
            return true;
        }

        Type target = type.GetInterface(typeOfCollection.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfCollection;
        }
        return includeDictionary && IsDictionary(type);
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IList{T}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsList(Type type) {
        Type typeOfList = typeof(IList<>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfList) {
            return true;
        }

        Type target = type.GetInterface(typeOfList.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfList;
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="ISet{T}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSet(Type type) {
        Type typeOfSet = typeof(ISet<>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfSet) {
            return true;
        }

        Type target = type.GetInterface(typeOfSet.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfSet;
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IDictionary{K,V}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsDictionary(Type type) {
        Type typeOfDictionary = typeof(IDictionary<,>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfDictionary) {
            return true;
        }

        Type target = type.GetInterface(typeOfDictionary.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfDictionary;
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IGenericSet{T}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsGenericSet(Type type) {
        Type typeOfSet = typeof(IGenericSet<>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfSet) {
            return true;
        }

        Type target = type.GetInterface(typeOfSet.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfSet;
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IGenericDictionary{TKey,TValue}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsGenericDictionary(Type type) {
        Type typeOfDictionary = typeof(IGenericDictionary<,>);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeOfDictionary) {
            return true;
        }

        Type target = type.GetInterface(typeOfDictionary.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfDictionary;
        }
        return false;
    }

    /// <summary>
    /// 获取Codec类关联的解码类型
    /// </summary>
    /// <param name="codecType"></param>
    /// <returns></returns>
    public static Type GetEncoderType(Type codecType) {
        Type type = codecType.GetInterface(typeof(IDsonCodec<>).Name);
        if (type == null) {
            throw new ArgumentException($"Type {codecType} is not a DsonCodec");
        }
        return type.GetGenericArguments()[0];
    }

    #endregion

    // 对于Converter，泛型方法只是辅助编码的方法
#nullable disable

    #region converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Write<T>(this IConverter converter, T value) {
        return converter.Write(value, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, byte[] source, Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IConverter converter, T value, DsonChunk chunk) {
        converter.Write(value, typeof(T), chunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, DsonChunk source, Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CloneObject(this IConverter converter, object? value, Type declaredType, Func<object>? factory = null) {
        return converter.CloneObject(value, declaredType, declaredType, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T CloneObject<T>(this IConverter converter, T value, Func<object>? factory = null) {
        Type declaredType = typeof(T);
        return (T)converter.CloneObject(value, declaredType, declaredType, factory);
    }

    #endregion

    #region dson-converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IDsonConverter converter, T value, IDsonOutput output) {
        converter.Write(value, typeof(T), output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IDsonConverter converter, IDsonInput source, Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string WriteAsDson<T>(this IDsonConverter converter, T value, ObjectStyle? style = null) {
        return converter.WriteAsDson(value, typeof(T), style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, string source, Func<object>? factory = null) {
        return (T)converter.ReadFromDson(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsDson<T>(this IDsonConverter converter, T value, TextWriter writer, ObjectStyle? style = null) {
        converter.WriteAsDson(value, typeof(T), writer, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, TextReader source, Func<object>? factory = null) {
        return (T)converter.ReadFromDson(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonValue WriteAsDsonValue<T>(this IDsonConverter converter, T value) {
        return converter.WriteAsDsonValue(value, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDsonValue<T>(this IDsonConverter converter, DsonValue source, Func<object>? factory = null) {
        return (T)converter.ReadFromDsonValue(source, typeof(T), factory);
    }

    #endregion

    #region reader

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this IDsonObjectReader reader, string? name) {
        return (short)reader.ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IDsonObjectReader reader, string? name) {
        return (byte)reader.ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this IDsonObjectReader reader, string? name) {
        return (char)reader.ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(this IDsonObjectReader reader, string? name) {
        return (uint)reader.ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(this IDsonObjectReader reader, string? name) {
        return (ulong)reader.ReadLong(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IDsonObjectReader reader, string? name) {
        return (ushort)reader.ReadInt(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this IDsonObjectReader reader, string? name) {
        return (sbyte)reader.ReadInt(name);
    }

    // object
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadObject<T>(this IDsonObjectReader reader, string? name, Func<object>? factory = null) {
        return (T)reader.ReadObject(name, typeof(T), factory);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="name">字段的名字</param>
    /// <returns>如果存在对应的字段则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadStartObject(this IDsonObjectReader reader, string? name) {
        if (reader.ReadName(name)) {
            reader.ReadStartObject();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="name">字段的名字</param>
    /// <returns>如果存在对应的字段则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadStartArray(this IDsonObjectReader reader, string? name) {
        if (reader.ReadName(name)) {
            reader.ReadStartArray();
            return true;
        }
        return false;
    }

    #endregion

    #region writer

    // 这里使用simple -- 外部通常包含明确类型
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(this IDsonObjectWriter writer, string? name, int value) {
        writer.WriteInt(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(this IDsonObjectWriter writer, string? name, long value) {
        writer.WriteLong(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(this IDsonObjectWriter writer, string? name, float value) {
        writer.WriteFloat(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(this IDsonObjectWriter writer, string? name, double value) {
        writer.WriteDouble(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, string? name, short value) {
        writer.WriteInt(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, string? name, short value, INumberStyle style) {
        writer.WriteInt(name, value, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, string? name, byte value) {
        writer.WriteInt(name, value, NumberStyles.Simple); // c#的byte是无符号整数，sbyte才是有符号整数
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, string? name, byte value, INumberStyle style) {
        writer.WriteInt(name, value, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, string? name, char value) {
        writer.WriteInt(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, string? name, char value, INumberStyle style) {
        writer.WriteInt(name, value, style);
    }

    // unsigned
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, string? name, uint value) {
        writer.WriteInt(name, (int)value, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, string? name, uint value, INumberStyle style) {
        writer.WriteInt(name, (int)value, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, string? name, ulong value) {
        writer.WriteLong(name, (long)value, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, string? name, ulong value, INumberStyle style) {
        writer.WriteLong(name, (long)value, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, string? name, ushort value) {
        writer.WriteInt(name, value, NumberStyles.Unsigned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, string? name, ushort value, INumberStyle style) {
        writer.WriteInt(name, value, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, string? name, sbyte value) {
        writer.WriteInt(name, value, NumberStyles.Simple);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, string? name, sbyte value, INumberStyle style) {
        writer.WriteInt(name, value, style);
    }

    // 流程
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, ObjectStyle style, Type encoderType, Type declaredType, int count = -1) {
        writer.WriteStartObject(style);
        writer.WriteTypeInfo(encoderType, declaredType, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name, ObjectStyle style) {
        writer.WriteName(name);
        writer.WriteStartObject(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name, ObjectStyle style, Type encoderType, Type declaredType, int count = -1) {
        writer.WriteName(name);
        writer.WriteStartObject(style);
        writer.WriteTypeInfo(encoderType, declaredType, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, ObjectStyle style, Type encoderType, Type declaredType, int count = -1) {
        writer.WriteStartArray(style);
        writer.WriteTypeInfo(encoderType, declaredType, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name, ObjectStyle style) {
        writer.WriteName(name);
        writer.WriteStartArray(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name, ObjectStyle style, Type encoderType, Type declaredType, int count = -1) {
        writer.WriteName(name);
        writer.WriteStartArray(style);
        writer.WriteTypeInfo(encoderType, declaredType, count);
    }

    #endregion
}
}