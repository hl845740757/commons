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
        Type target = type.GetInterface(typeOfDictionary.FullName!);
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeOfDictionary;
        }
        return false;
    }

    #endregion

    #region converter

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Write<T>(this IConverter converter, in T value) {
        return converter.Write(in value, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Write(this IConverter converter, object value, Type declaredType) {
        return converter.Write<object>(value, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, byte[] source, Func<T>? factory = null) {
        return converter.Read<T>(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Read(this IConverter converter, byte[] source, Type declaredType, Func<object>? factory = null) {
        return converter.Read<object>(source, declaredType, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IConverter converter, in T value, DsonChunk chunk) {
        converter.Write(in value, typeof(T), chunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IConverter converter, object value, Type declaredType, DsonChunk chunk) {
        converter.Write<object>(value, declaredType, chunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IConverter converter, DsonChunk source, Func<T>? factory = null) {
        return converter.Read<T>(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Read(this IConverter converter, DsonChunk source, Type declaredType, Func<object>? factory = null) {
        return converter.Read<object>(source, declaredType, factory);
    }

    /// <summary>
    /// 将对象写入指定buffer，并返回写入的字节数
    /// </summary>
    /// <param name="converter">converter</param>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="buffer">序列化输出buffer</param>
    /// <typeparam name="T">对象的声明类型</typeparam>
    /// <returns>写入的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Write<T>(this IConverter converter, in T value, Type declaredType, byte[] buffer) {
        DsonChunk chunk = new DsonChunk(buffer);
        converter.Write(in value, declaredType, chunk);
        return chunk.Used;
    }

    /// <summary>
    /// 将对象写入指定buffer，并返回写入的字节数
    /// </summary>
    /// <param name="converter">converter</param>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="buffer">序列化输出buffer</param>
    /// <returns>写入的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Write(this IConverter converter, object value, Type declaredType, byte[] buffer) {
        DsonChunk chunk = new DsonChunk(buffer);
        converter.Write<object>(value, declaredType, chunk);
        return chunk.Used;
    }

    #endregion

    #region dson-converter

    // 非泛型重载和默认T为声明类型的重载
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string WriteAsDson<T>(this IDsonConverter converter, in T value, ObjectStyle? style = null) {
        return converter.WriteAsDson<T>(in value, typeof(T), style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string WriteAsDson(this IDsonConverter converter, object value, Type declaredType, ObjectStyle? style = null) {
        return converter.WriteAsDson<object>(value, declaredType, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, string source, Func<T>? factory = null) {
        return converter.ReadFromDson<T>(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ReadFromDson(this IDsonConverter converter, string source, Type declaredType, Func<object>? factory = null) {
        return converter.ReadFromDson<object>(source, declaredType, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsDson<T>(this IDsonConverter converter, in T value, TextWriter writer, ObjectStyle? style = null) {
        converter.WriteAsDson(in value, typeof(T), writer, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsDson(this IDsonConverter converter, object value, Type declaredType, TextWriter writer, ObjectStyle? style = null) {
        converter.WriteAsDson<object>(value, declaredType, writer, style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDson<T>(this IDsonConverter converter, TextReader source, Func<T>? factory = null) {
        return converter.ReadFromDson<T>(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ReadFromDson(this IDsonConverter converter, TextReader source, Type declaredType, Func<object>? factory = null) {
        return converter.ReadFromDson<object>(source, declaredType, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonValue WriteAsDsonValue<T>(this IDsonConverter converter, in T value) {
        return converter.WriteAsDsonValue<T>(in value, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonValue WriteAsDsonValue(this IDsonConverter converter, object value, Type declaredType) {
        return converter.WriteAsDsonValue<object>(value, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadFromDsonValue<T>(this IDsonConverter converter, DsonValue source, Func<T>? factory = null) {
        return converter.ReadFromDsonValue<T>(source, typeof(T), factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ReadFromDsonValue(this IDsonConverter converter, DsonValue source, Type declaredType, Func<object>? factory = null) {
        return converter.ReadFromDsonValue<object>(source, declaredType, factory);
    }

    #endregion
}
}