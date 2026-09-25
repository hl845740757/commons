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

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 注意，该Codec只能进行编码，不能进行解码 -- 默认只能解码为List。
/// </summary>
/// <typeparam name="T"></typeparam>
public class EnumerableCodec<T> : IDsonCodec<IEnumerable<T>>
{
    private readonly Type encoderType;

    public EnumerableCodec(Type encoderType) {
        this.encoderType = encoderType;
    }

    public Type GetEncoderType() => encoderType;

    public void WriteObject(IDsonObjectWriter writer, IEnumerable<T> inst, Type declaredType, SerializeFeatures features) {
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        // T就是声明类型
        DsonCodecImpl<T> elementCodec = writer.GetInlinableCodec<T>();
        int count = GetCount(inst);
        if (elementCodec != null) {
            Type elementType = typeof(T);
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, count);
            foreach (T e in inst) {
                elementCodec.WriteObject(writer, e, elementType, elementFeatures);
            }
        } else {
            writer.WriteStartArray(encoderType, declaredType, selfFeatures, count);
            foreach (T e in inst) {
                writer.WriteObject(e, elementFeatures);
            }
            writer.WriteEndArray();
        }
    }

    private static int GetCount(IEnumerable<T> inst) {
        if (inst is ICollection<T> collection) {
            return collection.Count;
        }
        if (inst is IReadOnlyCollection<T> readOnlyCollection) {
            return readOnlyCollection.Count;
        }
        return 0;
    }


    public IEnumerable<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        return ReadAsList(reader, encoderType, features);
    }

    public static List<T> ReadAsList(IDsonObjectReader reader, Type encoderType,
                                     DeserializeFeatures features) {
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        int count = reader.ReadStartArray(encoderType, selfFeatures).count;
        List<T> result = new List<T>(count);
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>(elementFeatures);
            result.Add(value);
        }
        reader.ReadEndArray();
        return result;
    }
}
}