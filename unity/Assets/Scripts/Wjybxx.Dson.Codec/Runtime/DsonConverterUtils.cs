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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
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
    public static StringStyle ToStringStyle(this SerializeFeatures features) {
        features &= SerializeFeatures.MaskStringStyles;
        return features switch
        {
            SerializeFeatures.StringUnquote => StringStyle.Unquote,
            SerializeFeatures.StringLine => StringStyle.SingleLine,
            SerializeFeatures.StringText => StringStyle.SimpleText,
            SerializeFeatures.StringDsonText => StringStyle.DsonText,
            _ => StringStyle.AutoQuote
        };
    }

    #endregion
}
}