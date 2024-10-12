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
public sealed class ArrayCodec<T> : IDsonCodec<T[]>
{
    public void WriteObject(IDsonObjectWriter writer, ref T[] inst, Type declaredType, ObjectStyle style) {
        // declaredType只影响inst是否写入类型，不影响数组元素是否写入类型
        Type eleDeclaredType = typeof(T);

        for (int i = 0; i < inst.Length; i++) {
            writer.WriteObject(null, inst[i], eleDeclaredType);
        }
    }

    public T[] ReadObject(IDsonObjectReader reader, Func<T[]>? factory = null) {
        Type eleDeclaredType = typeof(T);

        // 由于长度未知，只能先存储为List再转...
        List<T> result = new List<T>();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>(null, eleDeclaredType);
            result.Add(value);
        }
        return result.ToArray();
    }
}
}