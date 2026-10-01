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
using Wjybxx.Dson.Types;

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
#if NET6_0_OR_GREATER
            || typeInfo == typeof(IReadOnlySet<T>)
#endif
            || typeInfo == typeof(HashSet<T>)) {
            return FactoryKind.HashSet;
        }
        // Dequeue
        if (typeInfo == typeof(MultiChunkDeque<T>)) {
            return FactoryKind.MultiChunkDequeue;
        }
        if (typeInfo == typeof(IDeque<T>)
            || typeInfo == typeof(ArrayDeque<>)) {
            return FactoryKind.ArrayDequeue;
        }
        return FactoryKind.Unknown;
    }

    private enum FactoryKind
    {
        Unknown,
        HashSet,
        LinkedHashSet,
        ArrayDequeue,
        MultiChunkDequeue,
    }

    public Type GetEncoderType() => encoderType;

    private ICollection<T> NewCollection(int count) {
        if (factory != null) return factory();
        return factoryKind switch
        {
            FactoryKind.HashSet => new HashSet<T>(count),
            FactoryKind.LinkedHashSet => new LinkedHashSet<T>(count),
            FactoryKind.ArrayDequeue => new ArrayDeque<T>(count),
            FactoryKind.MultiChunkDequeue => new MultiChunkDeque<T>(),
            _ => new List<T>(count)
        };
    }

    public void WriteObject(IDsonObjectWriter writer, ICollection<T> inst, Type declaredType, SerializeFeatures features) {
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        // T就是声明类型 - 接口类型foreach会导致迭代器装箱，但序列化性能不是重点关注项
        DsonCodecImpl<T> elementCodec = writer.GetInlinableCodec<T>();
        if (elementCodec != null) {
            Type elementType = typeof(T);
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
            foreach (T value in inst) {
                elementCodec.WriteObject(writer, in value, elementType, elementFeatures);
            }
            writer.WriteEndArray();
        } else {
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, inst.Count);
            foreach (T value in inst) {
                writer.WriteObject(in value, elementFeatures);
            }
            writer.WriteEndArray();
        }
    }

    public ICollection<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        if ((features & DeserializeFeatures.SerializeReference) != 0) {
            return ReadCollectionRef(reader, features);
        }

        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        int count = reader.ReadStartArray(encoderType, selfFeatures).count;
        ICollection<T> result = NewCollection(count);
        // T就是声明类型
        DsonCodecImpl<T> elementCodec = reader.GetInlinableCodec<T>();
        if (elementCodec != null) {
            Type elementType = typeof(T);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = elementCodec.ReadObject(reader, elementType, elementFeatures);
                result.Add(value);
            }
        } else {
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>(elementFeatures);
                result.Add(value);
            }
        }
        reader.ReadEndArray();
        return result;
    }

    private ICollection<T> ReadCollectionRef(IDsonObjectReader reader, DeserializeFeatures features) {
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        int count = reader.ReadStartArray(encoderType, selfFeatures).count;
        List<T> result = new List<T>(count);
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            if (reader.TryReadPtr(out int ptr)) {
                result.Add(default); // 预填充默认值 - 由于可能立即执行SetField，所以必须先填充
                reader.DeferReference(ptr, this, result, result.Count - 1);
            } else {
                T value = reader.ReadObject<T>(elementFeatures);
                result.Add(value);
            }
        }
        reader.ReadEndArray();
        return result;
    }

    public bool SetField(object inst, int index, object value) {
        if (inst is List<T> result) {
            result[index] = (T)value;
            return true;
        }
        return false;
    }
}
}