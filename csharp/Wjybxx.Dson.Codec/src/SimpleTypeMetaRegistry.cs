#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
/// 
/// </summary>
public class SimpleTypeMetaRegistry : ITypeMetaRegistry
{
    // 使用bool避免字典使用接口类型，减少不必要的依赖和虚方法调用
    private readonly bool mutable;
    private readonly Dictionary<Type, TypeMeta> type2MetaDic;
    private readonly Dictionary<string, TypeMeta> name2MetaDic;

    public SimpleTypeMetaRegistry() {
        mutable = true;
        type2MetaDic = new Dictionary<Type, TypeMeta>(32);
        name2MetaDic = new Dictionary<string, TypeMeta>(32);
    }

    public SimpleTypeMetaRegistry(Dictionary<Type, TypeMeta> type2MetaDic,
                                  Dictionary<string, TypeMeta> name2MetaDic) {
        mutable = false;
        this.type2MetaDic = new Dictionary<Type, TypeMeta>(type2MetaDic);
        this.name2MetaDic = new Dictionary<string, TypeMeta>(name2MetaDic);
    }

    #region factory

    public static SimpleTypeMetaRegistry FromMapper(ISet<Type> typeSet, Func<Type, TypeMeta> mapper) {
        SimpleTypeMetaRegistry registry = new SimpleTypeMetaRegistry();
        foreach (Type type in typeSet) {
            TypeMeta typeMeta = mapper.Invoke(type);
            if (typeMeta.type != type) {
                throw new InvalidOperationException("type: " + type);
            }
            registry.Add(typeMeta);
        }
        return registry.ToImmutable();
    }

    public static SimpleTypeMetaRegistry FromTypeMetas(params TypeMeta[] typeMetas) {
        return new SimpleTypeMetaRegistry().AddAll(typeMetas)
            .ToImmutable();
    }
    public static SimpleTypeMetaRegistry FromTypeMetas(IEnumerable<TypeMeta> typeMetas) {
        return new SimpleTypeMetaRegistry().AddAll(typeMetas)
            .ToImmutable();
    }

    public static SimpleTypeMetaRegistry FromRegistries(IEnumerable<ITypeMetaRegistry> registries) {
        SimpleTypeMetaRegistry result = new SimpleTypeMetaRegistry();
        foreach (ITypeMetaRegistry other in registries) {
            result.MergeFrom(other);
        }
        return result.ToImmutable();
    }

    /** 转为不可变实例 */
    public SimpleTypeMetaRegistry ToImmutable() {
        return new SimpleTypeMetaRegistry(type2MetaDic, name2MetaDic);
    }

    #endregion

    #region update

    private void EnsureMutable() {
        if (!mutable) throw new InvalidOperationException("registry is immutable");
    }

    public void Clear() {
        EnsureMutable();
        type2MetaDic.Clear();
        name2MetaDic.Clear();
    }

    public SimpleTypeMetaRegistry MergeFrom(ITypeMetaRegistry other) {
        foreach (TypeMeta typeMeta in other.Export()) {
            Add(typeMeta);
        }
        return this;
    }

    public SimpleTypeMetaRegistry AddAll(IEnumerable<TypeMeta> typeMetas) {
        foreach (TypeMeta typeMeta in typeMetas) {
            Add(typeMeta);
        }
        return this;
    }

    public SimpleTypeMetaRegistry Add(TypeMeta typeMeta) {
        EnsureMutable();
        Type typeInfo = typeMeta.type;
        if (type2MetaDic.TryGetValue(typeMeta.type, out TypeMeta exist)) {
            if (exist.Equals(typeMeta)) {
                return this;
            }
            // 冲突需要用户解决 -- Codec的冲突是无害的，而TypeMeta的冲突是有害的
            throw new ArgumentException($"type conflict, type: {typeInfo}");
        }
        type2MetaDic[typeMeta.type] = typeMeta;

        foreach (string clsName in typeMeta.clsNames) {
            if (name2MetaDic.ContainsKey(clsName)) {
                throw new ArgumentException($"clsName conflict, type: {typeInfo}, clsName: {clsName}");
            }
            name2MetaDic[clsName] = typeMeta;
        }
        return this;
    }

    /** 删除给定类型的TypeMeta，主要用于解决冲突 */
    public TypeMeta? Remove(Type typeInfo) {
        EnsureMutable();
        if (type2MetaDic.Remove(typeInfo, out TypeMeta typeMeta)) {
            foreach (string clsName in typeMeta.clsNames) {
                name2MetaDic.Remove(clsName);
            }
        }
        return typeMeta;
    }

    #endregion

    public TypeMeta? OfType(Type type) {
        type2MetaDic.TryGetValue(type, out TypeMeta typeMeta);
        return typeMeta;
    }

    public TypeMeta? OfName(string clsName) {
        name2MetaDic.TryGetValue(clsName, out TypeMeta typeMeta);
        return typeMeta;
    }

    public List<TypeMeta> Export() {
        return new List<TypeMeta>(type2MetaDic.Values);
    }
}
}