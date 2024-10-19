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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型元数据配置
/// 
/// <h3>泛型</h3>
/// 用户在初始化Config时无需处理泛型类的TypeMeta，底层会动态生成对应的TypeMeta，用户只需要保证使用到的所有原始类型都注册了即可。
///
/// <h3>合并规则</h3>
/// 多个Config合并时，越靠近用户，优先级越高 -- 因为这一定能解决冲突。
/// </summary>
public class TypeMetaConfig
{
    private readonly IDictionary<Type, TypeMeta> type2MetaDic;
    private readonly IDictionary<string, TypeMeta> name2MetaDic;

    public TypeMetaConfig() {
        type2MetaDic = new Dictionary<Type, TypeMeta>(32);
        name2MetaDic = new Dictionary<string, TypeMeta>(32);
    }

    public TypeMetaConfig(TypeMetaConfig other) {
        this.type2MetaDic = other.type2MetaDic.ToImmutableLinkedDictionary();
        this.name2MetaDic = other.name2MetaDic.ToImmutableLinkedDictionary();
    }

    #region factory

    public static TypeMetaConfig FromTypeMetas(params TypeMeta[] typeMetas) {
        return new TypeMetaConfig().AddAll(typeMetas)
            .ToImmutable();
    }

    public static TypeMetaConfig FromTypeMetas(IEnumerable<TypeMeta> typeMetas) {
        return new TypeMetaConfig().AddAll(typeMetas)
            .ToImmutable();
    }

    public static TypeMetaConfig FromConfigs(IEnumerable<TypeMetaConfig> configs) {
        TypeMetaConfig result = new TypeMetaConfig();
        foreach (TypeMetaConfig other in configs) {
            result.MergeFrom(other);
        }
        return result.ToImmutable();
    }

    /** 转为不可变实例 */
    public TypeMetaConfig ToImmutable() {
        if (type2MetaDic is Dictionary<Type, TypeMeta>) {
            return new TypeMetaConfig(this);
        }
        return this;
    }

    #endregion

    #region update

    public void Clear() {
        type2MetaDic.Clear();
        name2MetaDic.Clear();
    }

    public TypeMetaConfig MergeFrom(TypeMetaConfig other) {
        foreach (TypeMeta typeMeta in other.type2MetaDic.Values) {
            Add(typeMeta);
        }
        return this;
    }

    public TypeMetaConfig AddAll(IEnumerable<TypeMeta> typeMetas) {
        foreach (TypeMeta typeMeta in typeMetas) {
            Add(typeMeta);
        }
        return this;
    }

    /** 添加TypeMeta，会检测冲突 */
    public TypeMetaConfig Add(TypeMeta typeMeta) {
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
        if (type2MetaDic.Remove(typeInfo, out TypeMeta typeMeta)) {
            foreach (string clsName in typeMeta.clsNames) {
                name2MetaDic.Remove(clsName);
            }
        }
        return typeMeta;
    }

    // 以下为快捷方法
    public TypeMetaConfig Add(Type type, string clsName) {
        Add(TypeMeta.Of(type, clsName));
        return this;
    }

    public TypeMetaConfig Add(Type type, params string[] clsNames) {
        Add(TypeMeta.Of(type, clsNames));
        return this;
    }

    public TypeMetaConfig Add(Type type, ObjectStyle style, string clsName) {
        Add(TypeMeta.Of(type, style, clsName));
        return this;
    }

    public TypeMetaConfig Add(Type type, ObjectStyle style, params string[] clsNames) {
        Add(TypeMeta.Of(type, style, clsNames));
        return this;
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

    #region 默认配置

    public static TypeMetaConfig Default { get; } = NewDefaultConfig().ToImmutable();

    /// <summary>
    /// 创建一个默认配置
    /// 由于集合的命名难以统一，因此作为可选项
    /// </summary>
    /// <param name="includeCollections"></param>
    /// <returns></returns>
    public static TypeMetaConfig NewDefaultConfig(bool includeCollections = true) {
        TypeMetaConfig config = new TypeMetaConfig();
        config.Add(typeof(int), DsonTexts.LabelInt32, "int", "int32");
        config.Add(typeof(long), DsonTexts.LabelInt64, "long", "int64");
        config.Add(typeof(float), DsonTexts.LabelFloat, "float");
        config.Add(typeof(double), DsonTexts.LabelDouble, "double");
        config.Add(typeof(bool), DsonTexts.LabelBool, "bool", "boolean");
        config.Add(typeof(string), DsonTexts.LabelString, "string");
        config.Add(typeof(Binary), DsonTexts.LabelBinary, "bytes");
        config.Add(typeof(ObjectPtr), DsonTexts.LabelPtr);
        config.Add(typeof(ObjectLitePtr), DsonTexts.LabelLitePtr);
        config.Add(typeof(ExtDateTime), DsonTexts.LabelDateTime);
        config.Add(typeof(Timestamp), DsonTexts.LabelTimestamp);
        // 基础类型
        config.Add(typeof(uint), DsonTexts.LabelUInt32, "uint", "uint32");
        config.Add(typeof(ulong), DsonTexts.LabelUInt64, "ulong", "uint64");
        config.Add(typeof(short), "int16", "short");
        config.Add(typeof(ushort), "uint16", "ushort");
        config.Add(typeof(byte), "byte");
        config.Add(typeof(sbyte), "sbyte");
        config.Add(typeof(char), "char");
        // 特殊组件
        config.Add(typeof(object), "Object", "object"); // object会作为泛型参数...
        config.Add(typeof(Nullable<>), "Nullable"); // Nullable

        // 基础集合
        config.Add(typeof(ICollection<>), "ICollection", "ICollection`1");
        config.Add(typeof(IList<>), "IList", "IList`1");
        config.Add(typeof(List<>), "List", "List`1");

        config.Add(typeof(IDictionary<,>), "IDictionary", "IDictionary`2");
        config.Add(typeof(Dictionary<,>), "Dictionary", "Dictionary`2");
        config.Add(typeof(LinkedDictionary<,>), "LinkedDictionary", "LinkedDictionary`2");
        config.Add(typeof(ConcurrentDictionary<,>), "ConcurrentDictionary", "ConcurrentDictionary`2");

        config.Add(typeof(DictionaryEncodeProxy<>), "DictionaryEncodeProxy", "MapEncodeProxy");
        return config;
    }

    #endregion
}
}