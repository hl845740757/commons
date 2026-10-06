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
using System.Reflection;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 生成代码默认都会实现该类
///
/// <h3>生成器规则</h3>
/// 1.非public字段，如果缺少'public getter'，则通过反射赋值；生成器会生成SetValue方法，方法名为<c>_Get{fieldName}</c>
/// 2.非public字段，如果缺少'public setter'，则通过反射赋值；生成器会生成SetValue方法，方法名为<c>_Set{fieldName}</c>
/// 3.值类型不可以包含需要反射读写的字段。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractDsonCodec<T> : IDsonCodec<T>
{
    [StableName]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Type GetEncoderType() => typeof(T);

    #region Write

    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features) {
        Type encoderType = GetEncoderType();
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType);
        WriteFields(writer, ref inst);
        writer.WriteEndObject();
    }

    [StableName]
    protected abstract void WriteFields(IDsonObjectWriter writer, ref T inst);

    #endregion

    #region Read

    public T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        SerializeHeader header = reader.ReadStartObject(GetEncoderType(), features);
        T inst = NewInstance(reader);
        if (header.localId != 0) {
            reader.PublishReference(header.localId, inst);
        }
        // 新版固定Switch-Case随机读
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string name = reader.ReadName();
            if (!ReadField(reader, ref inst, name)) {
                reader.SkipValue();
            }
        }
        reader.ReadEndObject();
        return inst;
    }

    /// <summary>
    /// 创建一个实例（可以是子类实例）
    /// 1. 如果是抽象类，应当抛出异常
    /// 2. 该方法可解决readonly字段问题，但还是尽量减少readonly使用。
    /// </summary>
    [StableName]
    protected abstract T NewInstance(IDsonObjectReader reader);

    /// <summary>
    /// 读取单个字段
    /// 1.新版限定为必须通过Switch-Case解码，以简化其它设计。
    /// 2.返回值用于判断超类是否成功读取了字段，也用于模板方法判断是否需要跳过Value。
    ///
    /// <h3>普通字段</h3>
    /// 如果普通字段声明了<see cref="SerializeReference"/>注解，
    /// 解码时应当先调用<see cref="IDsonObjectReader.TryReadRefId"/>方法判断目标是否被编码为引用Id；
    /// 如果字段被编码为引用Id，则调用<see cref="IDsonObjectReader.DeferReference"/>方法延迟注入引用。
    /// <![CDATA[
    ///     if (reader.TryReadRefId(out int refId)) {
    ///         reader.DeferReference(refId, this, inst, "child"); // 字段名为dson序列化名
    ///     } else {
    ///         inst.child = reader.ReadObject<ChildType>();
    ///     }
    /// ]]>
    /// 
    /// <h3>List/Dictionary字段</h3>
    /// 如果集合类型字段声明了<see cref="SerializeReference"/>注解，
    /// 解码时，普通集合类型应当先解码为<see cref="List{T}"/>类型，而字典类型应当先解码为<see cref="Dictionary{TKey,TValue}"/>类型，
    /// 如果字段不直接是<see cref="List{T}"/>和<see cref="Dictionary{TKey,TValue}"/>类型，
    /// 则应当调用<see cref="IDsonObjectReader.DeferToTargetType"/>延迟转换为目标类型，生成代码固定调用重载（扩展）方法。
    /// <![CDATA[
    ///     List<ChildType> list = reader.ReadObject<List<ChildType>>();
    ///     // 如果字段是List类型，则直接赋值
    ///     inst.Children = list;
    ///     // 如果字段不是List类型，如HashSet，则调用扩展方法转换
    ///     reader.DeferToTargetType(this, inst, "children", (HashSet<T>)null);
    /// ]]>
    /// 
    /// 如果集合字段没有声明<see cref="SerializeReference"/>注解，但属于特殊类型（不可变集合）或指定了<see cref="DsonPropertyAttribute.TargetType"/>属性，
    /// 则也需要调用<see cref="IDsonObjectReader.DeferToTargetType"/>转换目标类型。
    /// <![CDATA[
    ///     List<ChildType> list = reader.ReadObject<List<ChildType>>();
    ///     reader.DeferToTargetType(this, inst, "children", (ImmutableList<T>)null);
    /// ]]>
    /// </summary>
    [StableName]
    protected virtual bool ReadField(IDsonObjectReader reader, ref T inst, string name) {
        return false;
    }

    /// <summary>
    /// 设置字段的值
    /// 注：除string/bytes以外的引用类型通常都需要在该方法中处理。
    /// </summary>
    [StableName]
    public virtual bool SetField(T inst, string name, object value) {
        return false;
    }

    #endregion

    #region util

    // 用于生成代码反射查询字段 - 避免过于复杂的生成器逻辑
    protected static PropertyInfo InternalGetProperty(string name) {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var encoderType = typeof(T);
        while (true) {
            PropertyInfo? propertyInfo = encoderType.GetProperty(name, flags);
            if (propertyInfo != null) {
                return propertyInfo;
            }
            encoderType = encoderType.BaseType;
            if (encoderType == typeof(object) || encoderType == null) {
                break;
            }
        }
        throw new ArgumentException($"Property '{name}' not found.");
    }

    protected static FieldInfo InternalGetField(string name) {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var encoderType = typeof(T);
        while (true) {
            FieldInfo? fieldInfo = encoderType.GetField(name, flags);
            if (fieldInfo != null) {
                return fieldInfo;
            }
            encoderType = encoderType.BaseType;
            if (encoderType == typeof(object) || encoderType == null) {
                break;
            }
        }
        throw new ArgumentException($"Field '{name}' not found.");
    }

    #endregion
}
}