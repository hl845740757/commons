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
using System.Reflection;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 单个泛型Codec的配置信息
/// </summary>
public readonly struct GenericCodecInfo
{
    /** 泛型定义类 */
    public readonly Type typeInfo;
    /** codec类型 */
    public readonly Type codecType;
    /** 声明工厂字段的类 -- 建议就是Codec类 */
    public readonly Type? factoryDeclaringType;
    /** 工厂字段的名字 -- factory不为null时不为null */
    public readonly string? factoryField;

    private GenericCodecInfo(Type typeInfo, Type codecType, Type? factoryDeclaringType, string? factoryField) {
        this.typeInfo = typeInfo;
        this.codecType = codecType;
        this.factoryDeclaringType = factoryDeclaringType;
        this.factoryField = factoryField;
    }

    /// <summary>
    /// 是否是无效结构
    /// </summary>
    public bool IsNull => typeInfo == null;

    /// <summary>
    /// 创建一个Item，适用于无需特殊构造泛型解码器
    /// </summary>
    /// <param name="genericType"></param>
    /// <param name="codecType"></param>
    /// <returns></returns>
    public static GenericCodecInfo Create(Type genericType, Type codecType) {
        return CreateImpl(genericType, codecType, null, null);
    }

    /// <summary>
    /// 创建一个Item，适用factory定义在codec类中的情况
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns></returns>
    public static GenericCodecInfo Create(Type genericType, Type codecType, string factoryFieldName) {
        return Create(genericType, codecType, codecType, factoryFieldName);
    }

    /// <summary>
    /// 通过工厂字段可以避免反射
    /// 
    /// </summary>
    /// <param name="genericType"></param>
    /// <param name="codecType"></param>
    /// <param name="factoryDeclaringType">声明工厂字段的类</param>
    /// <param name="factoryField">工厂字段的名字</param>
    /// <returns></returns>
    public static GenericCodecInfo Create(Type genericType, Type codecType, Type factoryDeclaringType, string factoryField) {
        if (factoryDeclaringType == null) throw new ArgumentNullException(nameof(factoryDeclaringType));
        if (factoryField == null) throw new ArgumentNullException(nameof(factoryField));
        if (factoryDeclaringType.GetField(factoryField, FactoryBindFlags) == null) {
            throw new ArgumentException($"factoryField is absent, type: {factoryDeclaringType}, field: {factoryField}");
        }
        // GenericTypeArguments 属性获取真实泛型参数，GetGenericArguments() 方法则获取泛型参数定义 -- 这名字差异太小
        if (factoryDeclaringType.GetGenericArguments().Length != genericType.GetGenericArguments().Length) {
            throw new ArgumentException("GenericArguments.Length error,"
                                        + $"genericType: {genericType}, factoryDeclaringType: {factoryDeclaringType}");
        }
        return CreateImpl(genericType, codecType, factoryDeclaringType, factoryField);
    }

    private static GenericCodecInfo CreateImpl(Type genericType, Type codecType, Type? factoryDeclaringType, string? factoryField) {
        if (!genericType.IsGenericTypeDefinition) throw new ArgumentException("genericType must be GenericTypeDefinition");
        if (!codecType.IsGenericTypeDefinition) throw new ArgumentException("codecType must be GenericTypeDefinition");

        // GenericTypeArguments 属性获取真实泛型参数，GetGenericArguments() 方法则获取泛型参数定义
        if (genericType.GetGenericArguments().Length != codecType.GetGenericArguments().Length) {
            throw new ArgumentException($"GenericArguments.Length error," +
                                        $"genericType: {genericType}, codecType: {codecType}");
        }
        if (codecType.GetInterface(typeof(IDsonCodec<>).Name) == null) {
            throw new ArgumentException("codecType must be IDsonCodec");
        }
        return new GenericCodecInfo(genericType, codecType, factoryDeclaringType, factoryField);
    }

    /// <summary>
    /// 字段的绑定Flags
    /// </summary>
    internal const BindingFlags FactoryBindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
}
}