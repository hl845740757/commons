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
/// 提供常用集合类型的Codec
/// </summary>
public static class MoreCollectionCodecs
{
    #region 特殊集合

    /// <summary>
    /// <see cref="Stack{T}"/>不是<see cref="ICollection{T}"/>的子类......
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StackCodec<T> : IDsonCodec<Stack<T>>
    {
        public void WriteObject(IDsonObjectWriter writer, Stack<T> inst, Type declaredType, SerializeFeatures features) {
            EnumerableCodec<T>.WriteAsList(writer, inst, typeof(Stack<T>), declaredType, features);
        }

        public Stack<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
            // Stack并未实现ICollection接口，另外我们需要保持与序列化之前相同的顺序，需要将list反向转换为Stack
            List<T> list = EnumerableCodec<T>.ReadAsList(reader, typeof(Stack<T>), features);
            return CollectionUtil.ToStack(list);
        }
    }

    /// <summary>
    /// <see cref="Queue{T}"/>也不是<see cref="ICollection{T}"/>的子类...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueCodec<T> : IDsonCodec<Queue<T>>
    {
        public void WriteObject(IDsonObjectWriter writer, Queue<T> inst, Type declaredType, SerializeFeatures features) {
            EnumerableCodec<T>.WriteAsList(writer, inst, typeof(Stack<T>), declaredType, features);
        }

        public Queue<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
            List<T> list = EnumerableCodec<T>.ReadAsList(reader, typeof(Stack<T>), features);
            return CollectionUtil.ToQueue(list);
        }
    }

    #endregion
}
}