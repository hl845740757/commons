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
/// 是我天真了，C#特殊处理了Nullable的GetType，和装箱的效果一样，返回的是值的GetType，因此永远无法走到Nullable的Codec...
/// (除非编译期不可感知类型，即对象在编译期为泛型)
///
/// 在解码时，如果Dson中的值为null，会直接返回default；如果Dson中的值非null，会直接读取为目标值，然后强转为Nullable。
/// int是可以强转为int?的 —— C#的类型转换不是单纯基于继承的。
/// </summary>
/// <typeparam name="T"></typeparam>
public class NullableCodec<T> : IDsonCodec<T?> where T : struct
{
    public void WriteObject(IDsonObjectWriter writer, ref T? inst, Type declaredType, ObjectStyle style) {
        if (inst.HasValue) {
            writer.WriteObject("value", inst.Value);
        } else {
            writer.WriteNull("value");
        }
    }

    public T? ReadObject(IDsonObjectReader reader, Type declaredType, Func<T?>? factory = null) {
        DsonType dsonType = reader.ReadDsonType();
        T? r;
        if (dsonType == DsonType.Null) {
            reader.ReadNull("value");
            r = null;
        } else {
            r = reader.ReadObject<T>("value");
        }
        return r;
    }
}
}