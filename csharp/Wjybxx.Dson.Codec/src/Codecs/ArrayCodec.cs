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

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 数组的统一解码器，需要根据泛型参数动态构造，以避免拆装箱。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ArrayCodec<T> : IDsonCodec<T[]>
{
    public void WriteObject(IDsonObjectWriter writer, T[] inst, Type declaredType, SerializeFeatures features) {
        EnumerableCodec<T>.WriteAsList(writer, inst, typeof(T[]), declaredType, features);
    }

    public T[] ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        // Header中的count并非精确值，因此不能直接构造最终数组
        List<T> list = EnumerableCodec<T>.ReadAsList(reader, typeof(T[]), features);
        return list.ToArray();
    }
}
}