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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wjybxx.Commons;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 为支持数组和泛型，我们根据原型类型动态创建TypeMeta
/// </summary>
public sealed class DynamicTypeMetaRegistry : ITypeMetaRegistry
{
    /// <summary>
    /// 用户的原始的类型元数据
    /// </summary>
    private readonly TypeMetaConfig _config;
    private readonly ConcurrentDictionary<string, ClassName> classNamePool = new ConcurrentDictionary<string, ClassName>();
    private readonly ConcurrentDictionary<Type, TypeMeta> type2MetaDic = new ConcurrentDictionary<Type, TypeMeta>();
    private readonly ConcurrentDictionary<string, TypeMeta> name2MetaDic = new ConcurrentDictionary<string, TypeMeta>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config">元类型注册表</param>
    public DynamicTypeMetaRegistry(TypeMetaConfig config) {
        _config = config.ToImmutable();
    }

    #region OfType

    public TypeMeta? OfType(Type type) {
        TypeMeta typeMeta = _config.OfType(type);
        if (typeMeta != null) {
            return typeMeta;
        }
        if (type2MetaDic.TryGetValue(type, out typeMeta)) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型，或在基础注册表中不存在
        if (!type.IsGenericType && !type.IsArray) {
            return null;
        }

        ObjectStyle style;
        if (type.IsArray) {
            style = ObjectStyle.Indent;
        } else {
            Type rawType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            TypeMeta rawTypeMeta = _config.OfType(rawType);
            if (rawTypeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            style = rawTypeMeta.style; // 保留泛型类的Style
        }
        ClassName className = ClassNameOfType(type); // 放前方可检测泛型
        string mainClsName = className.ToString();

        // 需要动态生成TypeMeta并缓存下来
        typeMeta = TypeMeta.Of(type, style, mainClsName);
        type2MetaDic.TryAdd(type, typeMeta);
        name2MetaDic.TryAdd(mainClsName, typeMeta);
        return typeMeta;
    }

    #endregion

    #region OfName

    public TypeMeta? OfName(string clsName) {
        TypeMeta typeMeta = _config.OfName(clsName);
        if (typeMeta != null) {
            return typeMeta;
        }
        if (name2MetaDic.TryGetValue(clsName, out typeMeta)) {
            return typeMeta;
        }
        // 走到这里，通常意味着clsName是数组或泛型 -- 别名可能导致断言失败
        ClassName className = ParseName(clsName);
        // Debug.Assert(className.IsArray || className.IsGeneric);
        Type type = TypeOfClassName(className);

        // 通过Type初始化TypeMeta，我们尽量合并TypeMeta -- clsName包含空白时不缓存
        typeMeta = OfType(type);
        if (typeMeta == null) {
            throw new DsonCodecException("typeMeta absent, type: " + type);
        }
        if (typeMeta.clsNames.Contains(clsName) || ObjectUtil.ContainsWhitespace(clsName)) {
            return typeMeta;
        }
        // 覆盖数据
        {
            List<string> clsNames = new List<string>(typeMeta.clsNames.Count + 1);
            clsNames.AddRange(typeMeta.clsNames);
            clsNames.Add(clsName);

            typeMeta = TypeMeta.Of(type, typeMeta.style, clsNames);
            type2MetaDic[type] = typeMeta;
            foreach (string clsName2 in clsNames) {
                name2MetaDic[clsName2] = typeMeta;
            }
        }
        return typeMeta;
    }

    #endregion

    #region internal

    public ClassName ParseName(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        if (classNamePool.TryGetValue(clsName, out ClassName result)) {
            return result;
        }
        // 程序生成的clsName通常是紧凑的，不包含空白字符(缩进)的，因此可以安全缓存；
        // 如果clsName包含空白字符，通常是用户手写的，缓存有一定的风险性 —— 可能产生恶意缓存
        if (ObjectUtil.ContainsWhitespace(clsName)) {
            return ClassName.Parse(clsName);
        }
        result = ClassName.Parse(clsName);
        classNamePool.TryAdd(clsName, result);
        return result;
    }

    /// <summary>
    /// 根据Type查找对应的ClassName。
    /// 1.由于类型存在别名，一个Type的ClassName可能有很多个，且泛型参数还会导致组合，导致更多的类型名，但动态生成时我们只生成确定的一种。
    /// 2.解析的开销较大，需要缓存最终结果。
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private ClassName ClassNameOfType(Type type) {
        if (type.IsArray) {
            Type rootElementType = ArrayUtil.GetRootElementType(type);
            int arrayRank = ArrayUtil.GetArrayRank(type);
            string clsName = ClassNameOfType(rootElementType) + ArrayUtil.ArrayRankSymbol(arrayRank);
            return new ClassName(clsName);
        }
        if (type.IsGenericType) {
            // 泛型原型类必须存在于用户的注册表中
            TypeMeta typeMeta = _config.OfType(type.GetGenericTypeDefinition());
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            Type[] genericArguments = type.GenericTypeArguments; // 真实泛型参数
            List<ClassName> typeArgClassNames = new List<ClassName>(genericArguments.Length);
            foreach (Type genericArgument in genericArguments) {
                typeArgClassNames.Add(ClassNameOfType(genericArgument));
            }
            return new ClassName(typeMeta.MainClsName, typeArgClassNames);
        }
        // 非泛型非数组，必须存在于用户的注册表中
        {
            TypeMeta typeMeta = _config.OfType(type);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, type: " + type);
            }
            return new ClassName(typeMeta.MainClsName);
        }
    }

    /// <summary>
    /// 根据ClassName查找对应的Type。
    /// 1.ClassName到Type之间是多对一关系。
    /// 2.解析的开销较大，需要缓存最终结果。
    /// </summary>
    private Type TypeOfClassName(in ClassName className) {
        // 先解析泛型类，再构建数组
        int arrayRank = className.ArrayRank;
        Type elementType;
        if (arrayRank > 0) {
            // 获取数组根元素的类型
            elementType = TypeOfClassName(new ClassName(className.RootElement, className.typeArgs));
        } else {
            // 解析泛型原型 —— 泛型原型类必须存在于用户的注册表中
            TypeMeta typeMeta = _config.OfName(className.clsName);
            if (typeMeta == null) {
                throw new DsonCodecException("typeMeta absent, className: " + className);
            }
            elementType = typeMeta.type;
            // 解析泛型参数
            int typeArgsCount = className.typeArgs.Count;
            if (typeArgsCount > 0) {
                Type[] typeParameters = new Type[typeArgsCount];
                for (int index = 0; index < typeArgsCount; index++) {
                    typeParameters[index] = TypeOfClassName(className.typeArgs[index]);
                }
                elementType = elementType.MakeGenericType(typeParameters);
            }
        }
        // 构建多维数组 -- 与MakeArrayType(rank)接口获得的结果不一样
        while (arrayRank-- > 0) {
            elementType = elementType.MakeArrayType();
        }
        return elementType;
    }

    #endregion
}
}