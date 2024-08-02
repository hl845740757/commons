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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于解决泛型问题，提供统一抽象
/// </summary>
public interface IDsonCodec
{
    /// <summary>
    /// Codec关联的类型
    /// </summary>
    /// <returns></returns>
    Type GetEncoderClass();

    /// <summary>
    /// 该方法用于告知<see cref="DsonCodecImpl{T}"/>是否自动调用以下方法：
    /// <see cref="IDsonObjectWriter.WriteStartObject()"/>
    /// <see cref="IDsonObjectWriter.WriteEndObject"/>
    /// <see cref="IDsonObjectReader.ReadStartObject()"/>
    /// <see cref="IDsonObjectReader.ReadEndObject"/>
    /// 
    /// Q：禁用该属性有什么用？
    /// A：对于写；你可以将当前转换为另一个对象，然后再使用对应的codec进行编码；对于读：你可以使用另一个codec来解码当前二进制对象。
    /// 即：关闭该属性可以实现读替换(readReplace)和写替换(writeReplace)功能。
    /// 另外，还可以自行决定是写为Array还是Object。
    /// </summary>
    bool AutoStartEnd => true;

    /// <summary>
    /// 当前对象是否按照数组格式编码。
    /// 1.默认情况下，Map是被看做普通的数组的
    /// 2.该属性只有<see cref="AutoStartEnd"/>为true的时候有效。
    /// </summary>
    bool IsWriteAsArray => DsonConverterUtils.IsEncodeAsArray(GetEncoderClass());
}

/// <summary>
/// 对象编解码器。
/// Codec与<see cref="DsonCodecImpl{T}"/>协调工作，为典型的桥接模式。
/// 
/// 
/// 1. 编码的对象可能是'T'的子类；解码返回的对象也可能是'T'的子类。
/// 2. Codec的泛型'T'和参数declaredType可能并不兼容，因此必须显式传入。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDsonCodec<T> : IDsonCodec
{
    /// <summary>
    /// C#是真泛型，因此T就是其
    /// </summary>
    /// <returns></returns>
    Type IDsonCodec.GetEncoderClass() => typeof(T);

    /// <summary>
    /// 由于序列化的时候，可能触发实例数据变化，为支持结构体序列化，因此需要使用ref
    /// </summary>
    /// <param name="writer">writer</param>
    /// <param name="inst">要编码的实例</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="style">文本编码样式</param>
    void WriteObject(IDsonObjectWriter writer, ref T inst, Type declaredType, ObjectStyle style);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="factory">实例工厂</param>
    /// <returns></returns>
    T ReadObject(IDsonObjectReader reader, Type declaredType, Func<T>? factory = null);
}
}