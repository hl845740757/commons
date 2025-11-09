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
using Wjybxx.Dson.Text;

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
        writer.WriteStartArray(inst.GetType(), declaredType, selfFeatures, 0);
        foreach (T value in inst) {
            writer.WriteObject(in value, elementFeatures);
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// <inheritdoc cref="IDsonCodec{T}.ReadObject"/>
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="factory">支持factory为集合类型</param>
    /// <returns></returns>
    public IEnumerable<T> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        if (factory != null) {
            int count = reader.ReadStartArray().count;
            ICollection<T> result = factory() as ICollection<T> ?? new List<T>(count);
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>();
                result.Add(value);
            }
            reader.ReadEndArray();
            return result;
        }
        return ReadAsList(reader);
    }

    public static List<T> ReadAsList(IDsonObjectReader reader) {
        int count = reader.ReadStartArray().count;
        List<T> result = new List<T>(count);
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>();
            result.Add(value);
        }
        reader.ReadEndArray();
        return result;
    }
}
}