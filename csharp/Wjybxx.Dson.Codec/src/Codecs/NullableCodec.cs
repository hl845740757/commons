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

namespace Wjybxx.Dson.Codec.Codecs
{
public interface INullableCodec<T>
{
    bool HasValue(in T value);
}

/// <summary>
/// <see cref="Nullable{T}"/>的模板编解码器
/// 注意：Nullable编码时不会再封装一层，而是直接写内部值。
/// </summary>
/// <typeparam name="T"></typeparam>
public class NullableCodec<T> : IDsonCodec<T?>, INullableCodec<T?> where T : struct
{
    public bool HasValue(in T? inst) => inst.HasValue;

    public void WriteObject(IDsonObjectWriter writer, T? inst, Type declaredType, SerializeFeatures features) {
        // declaredType 是Nullable<T>的类型，不是T的声明类型
        // 为避免外部测试是否为null导致装箱，Nullable结构体的null测试由Codec处理
        if (inst.HasValue) {
            writer.WriteObject(inst.Value, features.GetElementFeatures());
        } else {
            writer.WriteNull();
        }
    }

    public T? ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        return reader.ReadObject<T>(features.GetElementFeatures(), factory);
    }
}
}