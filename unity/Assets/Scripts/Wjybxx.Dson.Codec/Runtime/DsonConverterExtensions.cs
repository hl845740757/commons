#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.IO;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Codec
{
public static class DsonConverterExtensions
{
#nullable disable

    #region converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Write<T>(this IConverter converter, T value, SerializeFeatures features = 0) {
        return converter.Write(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, byte[] source, DeserializeFeatures features = 0) {
        return (T)converter.Read(source, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IConverter converter, T value, DsonChunk chunk, SerializeFeatures features = 0) {
        converter.Write(value, typeof(T), chunk, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, DsonChunk source, DeserializeFeatures features = 0) {
        return (T)converter.Read(source, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object CloneObject(this IConverter converter, object? value, Type declaredType) {
        return converter.CloneObject(value, declaredType, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T CloneObject<T>(this IConverter converter, T value) {
        Type declaredType = typeof(T);
        return (T)converter.CloneObject(value, declaredType, declaredType);
    }

    #endregion

    #region dson-converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IDsonConverter converter, T value, IDsonOutput output, SerializeFeatures features = 0) {
        converter.Write(value, typeof(T), output, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IDsonConverter converter, IDsonInput source, DeserializeFeatures features = 0) {
        return (T)converter.Read(source, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string WriteAsDson<T>(this IDsonConverter converter, T value, SerializeFeatures features = 0) {
        return converter.WriteAsDson(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, string source, DeserializeFeatures features = 0) {
        return (T)converter.ReadFromDson(source, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsDson<T>(this IDsonConverter converter, T value, TextWriter writer, SerializeFeatures features = 0) {
        converter.WriteAsDson(value, typeof(T), writer, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, TextReader source, DeserializeFeatures features = 0) {
        return (T)converter.ReadFromDson(source, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonArray<string> WriteAsDsonCollection<T>(this IDsonConverter converter, T value, SerializeFeatures features = 0) {
        return converter.WriteAsDsonCollection(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDsonCollection<T>(this IDsonConverter converter, DsonArray<string> source, DeserializeFeatures features = 0) {
        return (T)converter.ReadFromDsonCollection(source, typeof(T), features);
    }

    #endregion

    #region reader

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (short)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (byte)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (char)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (uint)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (ulong)reader.ReadLong(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (ushort)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this IDsonObjectReader reader, string name, DeserializeFeatures features = 0) {
        return (sbyte)reader.ReadInt(name, features);
    }

    // 无name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (short)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (byte)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (char)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (uint)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (ulong)reader.ReadLong(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (ushort)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this IDsonObjectReader reader, DeserializeFeatures features = 0) {
        return (sbyte)reader.ReadInt(features);
    }

    #endregion

    #region write-primitive

    // name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, string name, short value, SerializeFeatures features = 0) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, string name, byte value, SerializeFeatures features = 0) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, string name, char value, SerializeFeatures features = 0) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, string name, uint value, SerializeFeatures features = 0) {
        writer.WriteInt(name, (int)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, string name, ulong value, SerializeFeatures features = 0) {
        writer.WriteLong(name, (long)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, string name, ushort value, SerializeFeatures features = 0) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, string name, sbyte value, SerializeFeatures features = 0) {
        writer.WriteInt(name, value, features);
    }

    // 无name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, short value, SerializeFeatures features = 0) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, byte value, SerializeFeatures features = 0) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, char value, SerializeFeatures features = 0) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, uint value, SerializeFeatures features = 0) {
        writer.WriteInt((int)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, ulong value, SerializeFeatures features = 0) {
        writer.WriteLong((long)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, ushort value, SerializeFeatures features = 0) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, sbyte value, SerializeFeatures features = 0) {
        writer.WriteInt(value, features);
    }

    #endregion

    #region write-object

    // 流程 - 简化用户调用
    // 不写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name,
                                        Type encoderType, SerializeFeatures features) {
        writer.WriteName(name);
        writer.WriteStartObject(encoderType, features);
    }

    // 条件写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name,
                                        Type encoderType, Type declaredType, SerializeFeatures features,
                                        int count = 0) {
        writer.WriteName(name);
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, count);
    }

    // 条件写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer,
                                        Type encoderType, Type declaredType, SerializeFeatures features,
                                        int count = 0) {
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, count);
    }

    // 不写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name,
                                       Type encoderType, SerializeFeatures features) {
        writer.WriteName(name);
        writer.WriteStartArray(encoderType, features);
    }

    // 条件写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name,
                                       Type encoderType, Type declaredType, SerializeFeatures features,
                                       int count = 0) {
        writer.WriteName(name);
        writer.WriteStartArray(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, count);
    }

    // 条件写入Header
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer,
                                       Type encoderType, Type declaredType, SerializeFeatures features,
                                       int count = 0) {
        writer.WriteStartArray(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, count);
    }

    #endregion

    #region 类型转换

    // 此处只定义常见的类型转换需求，特殊的类型转换需求用户可通过扩展方法实现
    // 方法参数只起着重载识别作用，传入时总是Null
#pragma warning disable CS8714

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, T[]? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return list.ToArray();
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, Queue<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return new Queue<T>(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, Stack<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return CollectionUtil.ToStack(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, HashSet<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return new HashSet<T>(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, LinkedHashSet<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return new LinkedHashSet<T>(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, ArrayDeque<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return new ArrayDeque<T>(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, ImmutableList<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return ImmutableList<T>.CreateRange(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, ImmutableSet<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return ImmutableSet<T>.CreateRange(list);
        });
    }

    public static void DeferToTargetType<K, V>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                               Dictionary<K, V> fieldValue, LinkedDictionary<K, V>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            Dictionary<K, V> list = (Dictionary<K, V>)obj;
            return new LinkedDictionary<K, V>(list);
        });
    }

    public static void DeferToTargetType<K, V>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                               Dictionary<K, V> fieldValue, ArrayDictionary<K, V>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            Dictionary<K, V> list = (Dictionary<K, V>)obj;
            return new ArrayDictionary<K, V>(list, fastPath: true);
        });
    }

    public static void DeferToTargetType<K, V>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                               Dictionary<K, V> fieldValue, SortedList<K, V>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            Dictionary<K, V> list = (Dictionary<K, V>)obj;
            return new SortedList<K, V>(list);
        });
    }

    public static void DeferToTargetType<K, V>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                               Dictionary<K, V> fieldValue, ImmutableDictionary<K, V>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            Dictionary<K, V> list = (Dictionary<K, V>)obj;
            return ImmutableDictionary<K, V>.CreateRange(list);
        });
    }

#if NET6_0_OR_GREATER
    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, System.Collections.Immutable.ImmutableList<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return System.Collections.Immutable.ImmutableList.CreateRange(list);
        });
    }

    public static void DeferToTargetType<T>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                            List<T> fieldValue, System.Collections.Immutable.ImmutableHashSet<T>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            List<T> list = (List<T>)obj;
            return System.Collections.Immutable.ImmutableHashSet.CreateRange(list);
        });
    }

    public static void DeferToTargetType<K, V>(this IDsonObjectReader reader, IDsonCodec codec, object inst, string fieldName,
                                               Dictionary<K, V> fieldValue, System.Collections.Immutable.ImmutableDictionary<K, V>? _) {
        reader.DeferToTargetType(codec, inst, fieldName, fieldValue, static obj => {
            Dictionary<K, V> list = (Dictionary<K, V>)obj;
            return System.Collections.Immutable.ImmutableDictionary.CreateRange(list);
        });
    }
#endif

    #endregion
}
}