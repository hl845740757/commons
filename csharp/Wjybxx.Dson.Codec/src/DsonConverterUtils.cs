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
using Wjybxx.Commons;
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
            throw new ArgumentException($"Type {codecType} is not DsonCodec");
        }
        return type.GetGenericArguments()[0];
    }

    #endregion

    // 对于Converter，泛型方法只是辅助编码的方法
#nullable disable

    #region converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Write<T>(this IConverter converter, T value,
                                  SerializeFeatures features = default) {
        return converter.Write(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, byte[] source,
                            DeserializeFeatures features = default,
                            Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), features, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IConverter converter, T value, DsonChunk chunk,
                                SerializeFeatures features = default) {
        converter.Write(value, typeof(T), chunk, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, DsonChunk source,
                            DeserializeFeatures features = default,
                            Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), features, factory);
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
    public static void Write<T>(this IDsonConverter converter, T value, IDsonOutput output,
                                SerializeFeatures features = default) {
        converter.Write(value, typeof(T), output, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IDsonConverter converter, IDsonInput source,
                            DeserializeFeatures features = default,
                            Func<object>? factory = null) {
        return (T)converter.Read(source, typeof(T), features, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string WriteAsDson<T>(this IDsonConverter converter, T value,
                                        SerializeFeatures features = default) {
        return converter.WriteAsDson(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, string source,
                                    DeserializeFeatures features = default,
                                    Func<object>? factory = null) {
        return (T)converter.ReadFromDson(source, typeof(T), features, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsDson<T>(this IDsonConverter converter, T value, TextWriter writer,
                                      SerializeFeatures features = default) {
        converter.WriteAsDson(value, typeof(T), writer, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, TextReader source,
                                    DeserializeFeatures features = default,
                                    Func<object>? factory = null) {
        return (T)converter.ReadFromDson(source, typeof(T), features, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonArray<string> WriteAsDsonCollection<T>(this IDsonConverter converter, T value,
                                                             SerializeFeatures features = default) {
        return converter.WriteAsDsonCollection(value, typeof(T), features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDsonCollection<T>(this IDsonConverter converter, DsonArray<string> source,
                                              DeserializeFeatures features = default,
                                              Func<object>? factory = null) {
        return (T)converter.ReadFromDsonCollection(source, typeof(T), features, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDsonCollection<T>(this IDsonConverter converter, DsonArray<string> source, long localId,
                                              DeserializeFeatures features = default,
                                              Func<object>? factory = null) {
        return (T)converter.ReadFromDsonCollection(source, localId, typeof(T), features, factory);
    }

    #endregion

#nullable enable

    #region reader

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (short)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (byte)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (char)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (uint)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (ulong)reader.ReadLong(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (ushort)reader.ReadInt(name, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this IDsonObjectReader reader, string name, DeserializeFeatures features = default) {
        return (sbyte)reader.ReadInt(name, features);
    }

    // 无name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (short)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (byte)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ReadChar(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (char)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (uint)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (ulong)reader.ReadLong(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (ushort)reader.ReadInt(features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(this IDsonObjectReader reader, DeserializeFeatures features = default) {
        return (sbyte)reader.ReadInt(features);
    }

    // object
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadObject<T>(this IDsonObjectReader reader, string name, DeserializeFeatures features = default,
                                  Func<object>? factory = null) {
        return (T)reader.ReadObject(name, typeof(T), features, factory);
    }

    #endregion

    #region write-primitive

    // name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, string name, short value, SerializeFeatures features = default) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, string name, byte value, SerializeFeatures features = default) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, string name, char value, SerializeFeatures features = default) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, string name, uint value, SerializeFeatures features = default) {
        if ((int)value < 0) features |= SerializeFeatures.NumberHex;
        writer.WriteInt(name, (int)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, string name, ulong value, SerializeFeatures features = default) {
        if ((long)value < 0) features |= SerializeFeatures.NumberHex;
        writer.WriteLong(name, (long)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, string name, ushort value, SerializeFeatures features = default) {
        writer.WriteInt(name, value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, string name, sbyte value, SerializeFeatures features = default) {
        writer.WriteInt(name, value, features);
    }

    // 无name版
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(this IDsonObjectWriter writer, short value, SerializeFeatures features = default) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(this IDsonObjectWriter writer, byte value, SerializeFeatures features = default) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteChar(this IDsonObjectWriter writer, char value, SerializeFeatures features = default) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(this IDsonObjectWriter writer, uint value, SerializeFeatures features = default) {
        if ((int)value < 0) features |= SerializeFeatures.NumberHex;
        writer.WriteInt((int)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(this IDsonObjectWriter writer, ulong value, SerializeFeatures features = default) {
        if ((long)value < 0) features |= SerializeFeatures.NumberHex;
        writer.WriteLong((long)value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(this IDsonObjectWriter writer, ushort value, SerializeFeatures features = default) {
        writer.WriteInt(value, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(this IDsonObjectWriter writer, sbyte value, SerializeFeatures features = default) {
        writer.WriteInt(value, features);
    }

    #endregion

    #region write-object

    // 流程
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name,
                                        Type encoderType, SerializeFeatures features) {
        writer.WriteName(name);
        writer.WriteStartObject(encoderType, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer, string name,
                                        Type encoderType, Type declaredType,
                                        SerializeFeatures features, SerializeHeader header = default) {
        writer.WriteName(name);
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, header);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer,
                                        Type encoderType, Type declaredType,
                                        SerializeFeatures features, SerializeHeader header = default) {
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, header);
    }

    // 用于简化集合的写入代码
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject(this IDsonObjectWriter writer,
                                        Type encoderType, Type declaredType,
                                        SerializeFeatures features, int count) {
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, new SerializeHeader()
        {
            count = count,
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name,
                                       Type encoderType, SerializeFeatures features) {
        writer.WriteName(name);
        writer.WriteStartArray(encoderType, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer, string name,
                                       Type encoderType, Type declaredType,
                                       SerializeFeatures features, SerializeHeader header = default) {
        writer.WriteName(name);
        writer.WriteStartArray(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, header);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer,
                                       Type encoderType, Type declaredType,
                                       SerializeFeatures features, SerializeHeader header = default) {
        writer.WriteStartArray(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, header);
    }

    // 用于简化集合的写入代码
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray(this IDsonObjectWriter writer,
                                       Type encoderType, Type declaredType,
                                       SerializeFeatures features, int count) {
        writer.WriteStartArray(encoderType, features);
        writer.WriteHeader(encoderType, declaredType, features, new SerializeHeader()
        {
            count = count,
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteHeader(this IDsonObjectWriter writer,
                                   Type encoderType, Type declaredType,
                                   SerializeFeatures features, int count) {
        writer.WriteHeader(encoderType, declaredType, features, new SerializeHeader()
        {
            count = count,
        });
    }

    #endregion

    #region features

    /// <summary>
    /// 获取Nullable/List/Map元素的写入特征值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SerializeFeatures GetElementFeatures(this SerializeFeatures features) {
        SerializeFeatures elementFeatures = (features & SerializeFeatures.MaskElementFeatures);
        if ((features & SerializeFeatures.ElementIndent) != 0) {
            elementFeatures |= SerializeFeatures.ObjectIndent;
        } // 
        else if ((features & SerializeFeatures.ElementFlow) != 0) {
            elementFeatures |= SerializeFeatures.ObjectFlow;
        }
        return elementFeatures;
    }

    /// <summary>
    /// 擦除Nullable/List/Map元素的写入特征值
    /// (应该只比GetElementFeatures少一处调用 —— Nullable)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SerializeFeatures ErasureElementFeatures(this SerializeFeatures features) {
        const SerializeFeatures mask = SerializeFeatures.MaskElementFeatures
                                       | SerializeFeatures.ElementIndent
                                       | SerializeFeatures.ElementFlow;
        return features & ~mask;
    }

    /// <summary>
    /// 获取Nullable/List/Map元素的解码特征值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DeserializeFeatures GetElementFeatures(this DeserializeFeatures features) {
        return features & DeserializeFeatures.MaskElementFeatures;
    }

    /// <summary>
    /// 擦除Nullable/List/Map元素的解码特征值
    /// (应该只比GetElementFeatures少一处调用 —— Nullable)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DeserializeFeatures ErasureElementFeatures(this DeserializeFeatures features) {
        return features & ~DeserializeFeatures.MaskElementFeatures;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NumberStyle ToNumberStyle(this SerializeFeatures features) {
        NumberStyle style = NumberStyle.Simple;
        if ((features & SerializeFeatures.MaskNumberStyles) == 0) { // 大概率
            return style;
        }
        if ((features & SerializeFeatures.NumberTyped) != 0) {
            style |= NumberStyle.Typed;
        }
        if ((features & SerializeFeatures.NumberHex) != 0) {
            style |= NumberStyle.Hex;
        }
        if ((features & SerializeFeatures.NumberSigned) != 0) {
            style |= NumberStyle.Signed;
        }
        // 长度控制
        if ((features & SerializeFeatures.NumberFixed) != 0) {
            style |= NumberStyle.Fixed;
        } else if ((features & SerializeFeatures.NumberNoExponent3) != 0) {
            style |= NumberStyle.NoExponent3;
        } else if ((features & SerializeFeatures.NumberNoExponent7) != 0) {
            style |= NumberStyle.NoExponent7;
        }
        return style;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4Style ToDouble4Style(this SerializeFeatures features) {
        Double4Style style = Double4Style.Array;
        if ((features & SerializeFeatures.MaskDouble4Styles) == 0) { // 大概率
            return style;
        }
        SerializeFeatures basicStyle = features & SerializeFeatures.Double4AsArray;
        style = basicStyle switch
        {
            SerializeFeatures.Double4AsVector => Double4Style.Vector,
            SerializeFeatures.Double4AsRgba => Double4Style.Rgba,
            _ => Double4Style.Array
        };
        if ((features & SerializeFeatures.Double4Len2) != 0) {
            style |= Double4Style.Len2;
        } else if ((features & SerializeFeatures.Double4Len3) != 0) {
            style |= Double4Style.Len3;
        }
        if ((features & SerializeFeatures.Double4AsInt) != 0) {
            style |= Double4Style.Integer;
        }
        if ((features & SerializeFeatures.NumberNoExponent3) != 0) {
            style |= Double4Style.NoExponent3;
        } else if ((features & SerializeFeatures.NumberNoExponent7) != 0) {
            style |= Double4Style.NoExponent7;
        }
        return style;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringStyle ToStringStyle(this SerializeFeatures features) {
        features &= SerializeFeatures.MaskStringStyles;
        return features switch
        {
            SerializeFeatures.StringUnquote => StringStyle.Unquote,
            SerializeFeatures.StringText => StringStyle.DsonText,
            SerializeFeatures.StringLine => StringStyle.SingleLine,
            _ => StringStyle.AutoQuote
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapStyle ToMapStyle(this SerializeFeatures features) {
        features &= SerializeFeatures.MaskMapStyles;
        return features switch
        {
            SerializeFeatures.MapAsDocument => MapStyle.Document,
            SerializeFeatures.PairAsArray => MapStyle.PairAsArray,
            SerializeFeatures.PairAsDocument => MapStyle.PairAsDocument,
            _ => MapStyle.Array
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInitCapacity(this DeserializeFeatures features) {
        features = (features & DeserializeFeatures.InitCapacity3);
        return features switch
        {
            DeserializeFeatures.InitCapacity1 => 10,
            DeserializeFeatures.InitCapacity2 => 24,
            DeserializeFeatures.InitCapacity3 => 48,
            _ => 0
        };
    }

    #endregion
}
}