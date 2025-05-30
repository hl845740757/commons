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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 集合默认编解码器
/// </summary>
/// <typeparam name="T"></typeparam>
public class CollectionCodec<T> : IDsonCodec<ICollection<T>>
{
    private readonly Type encoderType;
    private readonly Func<ICollection<T>>? factory;
    private readonly FactoryKind factoryKind; // 处理默认情况

    /// <summary>
    /// 动态构建Codec时会被调用
    /// </summary>
    /// <param name="encoderType"></param>
    /// <param name="factory"></param>
    public CollectionCodec(Type encoderType, Func<ICollection<T>>? factory = null) {
        this.encoderType = encoderType;
        this.factory = factory;
        if (factory == null) {
            this.factoryKind = ComputeFactoryKind(encoderType);
        }
    }

    private static FactoryKind ComputeFactoryKind(Type typeInfo) {
        if (typeInfo == typeof(IGenericSet<T>)
            || typeInfo == typeof(LinkedHashSet<T>)) {
            return FactoryKind.LinkedHashSet;
        }
        // Unity没有IReadOnlySet
        if (typeInfo == typeof(ISet<T>)
            || typeInfo == typeof(HashSet<T>)) {
            return FactoryKind.HashSet;
        }
        if (typeInfo == typeof(IDeque<T>)
            || typeInfo == typeof(MultiChunkDeque<T>)) {
            return FactoryKind.Dequeue;
        }
        return FactoryKind.Unknown;
    }

    private enum FactoryKind
    {
        Unknown,
        HashSet,
        LinkedHashSet,
        Dequeue
    }

    public Type GetEncoderType() => encoderType;

    /** <see cref="encoderType"/>一定是用户declaredType的子类型，因此创建实例时不依赖declaredType */
    private ICollection<T> NewCollection() {
        if (factory != null) return factory();
        return factoryKind switch
        {
            FactoryKind.HashSet => new HashSet<T>(),
            FactoryKind.LinkedHashSet => new LinkedHashSet<T>(),
            FactoryKind.Dequeue => new MultiChunkDeque<T>(),
            _ => new List<T>()
        };
    }

    protected virtual ICollection<T> ToImmutable(Type declaredType, ICollection<T> result) {
        if (declaredType.IsInterface
            || declaredType.GetGenericTypeDefinition() == typeof(ImmutableLinkedHastSet<>)) {
            return ImmutableLinkedHastSet<T>.CreateRange(result);
        }
        return result;
    }

    public void WriteObject(IDsonObjectWriter writer, in ICollection<T> inst, Type declaredType, ObjectStyle style) {
        foreach (T value in inst) {
            writer.WriteObject(null, in value); // value向上转型为T
        }
    }

    public ICollection<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        ICollection<T> result = factory != null ? (ICollection<T>)factory() : NewCollection();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>(null);
            result.Add(value);
        }
        return reader.Options.readAsImmutable ? ToImmutable(declaredType, result) : result;
    }
}
}