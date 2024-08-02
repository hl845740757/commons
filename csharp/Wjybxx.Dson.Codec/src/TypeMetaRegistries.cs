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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// <see cref="ITypeMetaRegistry"/>的工具类
/// </summary>
public static class TypeMetaRegistries
{
    /// <summary>
    /// 通过类型集合和映射函数构建注册表
    /// </summary>
    /// <param name="typeSet">类型集合</param>
    /// <param name="mapper">类型到元数据的映射函数</param>
    public static ITypeMetaRegistry FromMapper(ISet<Type> typeSet, Func<Type, TypeMeta> mapper) {
        List<TypeMeta> typeMetas = new List<TypeMeta>();
        foreach (Type type in typeSet) {
            TypeMeta typeMeta = mapper.Invoke(type);
            if (typeMeta.type != type) {
                throw new InvalidOperationException("type: " + type);
            }
            typeMetas.Add(typeMeta);
        }
        return FromMetas(typeMetas);
    }

    public static ITypeMetaRegistry FromRegistries(params ITypeMetaRegistry[] registries) {
        return FromRegistries((IEnumerable<ITypeMetaRegistry>)registries);
    }

    /// <summary>
    /// 合并多个注册表为单个注册表。
    /// 注意：不适用数据动态变化的注册表。
    /// </summary>
    public static ITypeMetaRegistry FromRegistries(IEnumerable<ITypeMetaRegistry> registries) {
        List<TypeMeta> typeMetas = new List<TypeMeta>();
        foreach (ITypeMetaRegistry registry in registries) {
            typeMetas.AddRange(registry.Export());
        }
        return FromMetas(typeMetas);
    }

    public static ITypeMetaRegistry FromMetas(params TypeMeta[] typeMetas) {
        return FromMetas((IEnumerable<TypeMeta>)typeMetas);
    }

    /// <summary>
    /// 通过TypeMetas构建简单的注册表
    /// </summary>
    public static ITypeMetaRegistry FromMetas(IEnumerable<TypeMeta> typeMetas) {
        Dictionary<Type, TypeMeta> type2MetaDic = new Dictionary<Type, TypeMeta>();
        Dictionary<string, TypeMeta> name2MetaDic = new Dictionary<string, TypeMeta>();
        foreach (TypeMeta typeMeta in typeMetas) {
            if (type2MetaDic.ContainsKey(typeMeta.type)) {
                throw new ArgumentException($"type: {typeMeta.type} is duplicate");
            }
            type2MetaDic[typeMeta.type] = typeMeta;

            foreach (string clsName in typeMeta.clsNames) {
                if (name2MetaDic.ContainsKey(clsName)) {
                    throw new ArgumentException($"clsName: {clsName} is duplicate, type: {typeMeta.type}");
                }
                name2MetaDic[clsName] = typeMeta;
            }
        }
        return new TypeMetaRegistryImpl(type2MetaDic, name2MetaDic);
    }

    private class TypeMetaRegistryImpl : ITypeMetaRegistry
    {
        private readonly Dictionary<Type, TypeMeta> _type2MetaDic;
        private readonly Dictionary<string, TypeMeta> _name2MetaDic;

        public TypeMetaRegistryImpl(Dictionary<Type, TypeMeta> type2MetaDic,
                                    Dictionary<string, TypeMeta> name2MetaDic) {
            _type2MetaDic = type2MetaDic;
            _name2MetaDic = name2MetaDic;
        }

        public TypeMeta? OfType(Type type) {
            _type2MetaDic.TryGetValue(type, out TypeMeta typeMeta);
            return typeMeta;
        }

        public TypeMeta? OfName(string clsName) {
            _name2MetaDic.TryGetValue(clsName, out TypeMeta typeMeta);
            return typeMeta;
        }

        public List<TypeMeta> Export() {
            return new List<TypeMeta>(_type2MetaDic.Values);
        }
    }
}
}