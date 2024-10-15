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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// <see cref="Nullable{T}"/>的模板编解码器
/// 注意：Nullable编码时不会再封装一层，而是直接写内部值。
/// </summary>
/// <typeparam name="T"></typeparam>
public class NullableCodec<T> : IDsonCodec<T?> where T : struct
{
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref T? inst, Type declaredType, ObjectStyle style) {
        // 我们对Nullable类型编码的时候进行了修正，会走到这里
        writer.WriteObject(null, inst.Value, typeof(T));
    }

    public T? ReadObject(IDsonObjectReader reader, Func<T?>? factory = null) {
        // declaredType 是Nullable<T>的类型，不是T的声明类型 -- 转为读代理
        return reader.ReadObject<T>(null, typeof(T), null);
    }
}
}