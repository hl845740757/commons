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
using System.Diagnostics;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 提取工具方法
/// </summary>
public abstract class CollectionCodec
{
    public virtual bool AutoStartEnd => false;
    public virtual bool IsWriteAsArray => true;

    public static void WriteCollection<T>(IDsonObjectWriter writer, in IEnumerable<T> inst, Type declaredType, ObjectStyle style) {
        Type[] genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type eleDeclaredType = genericTypeArguments.Length > 0 ? declaredType.GenericTypeArguments[0] : typeof(object);

        writer.WriteStartArray(inst, declaredType, style);
        foreach (T value in inst) {
            writer.WriteObject<T>(null, in value, eleDeclaredType); // value向上转型为T
        }
        writer.WriteEndArray();
    }

    public static ICollection<T> ReadCollection<T>(IDsonObjectReader reader, Type declaredType, Func<ICollection<T>>? factory = null) {
        Type[] genericTypeArguments = DsonConverterUtils.GetGenericArguments(declaredType);
        Type eleDeclaredType = genericTypeArguments.Length > 0 ? declaredType.GenericTypeArguments[0] : typeof(object);

        ICollection<T> result = NewCollection(declaredType, factory);
        reader.ReadStartArray();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>(null, eleDeclaredType);
            result.Add(value);
        }
        reader.ReadEndArray();
        return result;
    }

    private static ICollection<T> NewCollection<T>(Type declaredType, Func<ICollection<T>>? factory) {
        if (factory != null) return factory.Invoke();
        if (declaredType.IsGenericType) {
            Type genericTypeDefinition = declaredType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(LinkedHashSet<>)) {
                return new LinkedHashSet<T>(); // 暂时未实现ISet接口
            }
            if (genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(ISet<>)) {
                return new HashSet<T>(); // 由于不能直接IsAssignableFrom测试，就测试几个特殊的类型
            }
        }
        return new List<T>();
    }
}

/// <summary>
/// 该Codec通常只用于编码
/// </summary>
/// <typeparam name="T"></typeparam>
public class ICollectionCodec<T> : CollectionCodec, IDsonCodec<ICollection<T>>
{
    private static readonly Func<ICollection<T>>? _factory = () => new List<T>();

    public void WriteObject(IDsonObjectWriter writer, ref ICollection<T> inst, Type declaredType, ObjectStyle style) {
        WriteCollection(writer, inst, declaredType, style);
    }

    public ICollection<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<ICollection<T>>? factory = null) {
        return ReadCollection(reader, declaredType, factory ?? _factory);
    }
}
}