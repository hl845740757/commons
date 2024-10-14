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

    public virtual bool IsWriteAsArray => true;

    public void WriteObject(IDsonObjectWriter writer, ref IEnumerable<T> inst, Type declaredType, ObjectStyle style) {
        Type eleDeclaredType = typeof(T);
        foreach (T value in inst) {
            writer.WriteObject<T>(null, in value, eleDeclaredType);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IDsonCodec{T}.ReadObject"/>
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="factory">支持factory为集合类型</param>
    /// <returns></returns>
    public IEnumerable<T> ReadObject(IDsonObjectReader reader, Func<IEnumerable<T>>? factory = null) {
        if (factory != null) {
            Type eleDeclaredType = typeof(T);
            ICollection<T> result = factory() as ICollection<T> ?? new List<T>();
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                T value = reader.ReadObject<T>(null, eleDeclaredType);
                result.Add(value);
            }
            return result;
        }
        return ReadAsList(reader);
    }

    public static List<T> ReadAsList(IDsonObjectReader reader) {
        Type eleDeclaredType = typeof(T);

        List<T> result = new List<T>();
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            T value = reader.ReadObject<T>(null, eleDeclaredType);
            result.Add(value);
        }
        return result;
    }
}
}