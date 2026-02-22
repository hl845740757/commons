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
/// 数组的统一解码器，需要根据泛型参数动态构造，以避免拆装箱。
/// 如果想提升性能，可以为常见基本类型数组提供定制的Codec，以避免低效的WriteObject/ReadObject。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ListCodec<T> : IDsonCodec<List<T>>
{
    public void WriteObject(IDsonObjectWriter writer, List<T> inst, Type declaredType, SerializeFeatures features) {
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        // T就是声明类型
        DsonCodecImpl<T> elementCodec = writer.GetInlinableCodec<T>();
        if (elementCodec != null) {
            Type elementType = typeof(T);
            writer.WriteStartArray(typeof(List<T>), declaredType, selfFeatures, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                elementCodec.WriteObject(writer, inst[i], elementType, elementFeatures);
            }
            writer.WriteEndArray();
        } else {
            writer.WriteStartArray(typeof(List<T>), declaredType, selfFeatures, inst.Count);
            for (int i = 0; i < inst.Count; i++) {
                writer.WriteObject(inst[i], elementFeatures);
            }
            writer.WriteEndArray();
        }
    }

    public List<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        int count = reader.ReadStartArray(typeof(List<T>), selfFeatures).count;
        List<T> result = new List<T>(count);
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
}
}