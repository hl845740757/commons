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
using System.Runtime.CompilerServices;
using Wjybxx.Dson.Codec.Codecs;

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
    Type GetEncoderType();

    /// <summary>
    /// 设置字段的值
    /// 
    /// 1.用于支持序列化引用 + 常用集合（HashSet/Queue/Stack） + 常用不可变集合。
    /// 2.只要使用到以上特殊特性，都需要实现该接口。
    /// </summary>
    /// <param name="inst">类型实例</param>
    /// <param name="name">字段名</param>
    /// <param name="value">字段值</param>
    /// <returns></returns>
    bool SetField(object inst, string name, object? value) {
        return false;
    }

    // 用于注入List/Array的元素
    bool SetField(object inst, int index, object? value) {
        return false;
    }

    // 用于注入Dictionary的元素
    bool SetField(object inst, object keyArray, int index, object? value) {
        return false;
    }
}

/// <summary>
/// 对象编解码器。
/// Codec与<see cref="DsonCodecImpl{T}"/>协调工作，为典型的桥接模式。
/// 
/// 1. 编码的对象可能是'T'的子类；解码返回的对象也可能是'T'的子类。
/// 2. 泛型codec可以包含接收<see cref="Type"/>的构造函数，还可以接收一个Factory。
/// 3. 手写Codec尽量继承<see cref="AbstractDsonCodec{T}"/>.
///
/// 泛型Codec可参考：<see cref="CollectionCodec{T}"/>、<see cref="DictionaryCodec{K,V}"/>
/// </summary>
/// <typeparam name="T">实例类型，可能是EncoderType的超类</typeparam>
public interface IDsonCodec<T> : IDsonCodec
{
    /// <summary>
    /// C#是真泛型，因此T通常就是其编解码类型。
    /// 如果可以，在数据兼容的情况下，尽量将泛型'T'声明为抽象类或接口，然后通过动态的Type来绑定编解码类型。
    /// EncoderType必须存在对应的<see cref="TypeMeta"/>
    /// </summary>
    /// <returns></returns>
    Type IDsonCodec.GetEncoderType() => typeof(T);

    /// <summary>
    /// 将对象写入输出流。
    ///
    /// 1.name在外部已写入，因此基础类型写入value时name传null。
    /// 2.declaredType只影响外层类型信息是否写入，而不应向下传递；以字典为例，字典本身可能需要写入类型信息，而KV可能是不需要的。
    /// </summary>
    /// <param name="writer">writer</param>
    /// <param name="inst">要编码的实例</param>
    /// <param name="declaredType">对象的声明类型，用于判断是否写入类型信息</param>
    /// <param name="features">序列化特征值</param>
    void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features);

    /// <summary>
    /// 从输入流中解析指定对象。
    ///
    /// 1.name在外部已读取，可通过<see cref="IDsonObjectReader.CurrentName"/>获取。
    /// 2.T一定是declaredType的子类型，投影逻辑在外层处理。
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="declaredType">对象的声明类型，用于判断类型转换等</param>
    /// <param name="features">反序列化特征值</param>
    /// <returns></returns>
    T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features);

    /// <summary>
    /// 设置字段的值
    ///
    /// 1.用于支持序列化引用 + 常用集合（HashSet/Queue/Stack） + 常用不可变集合。
    /// 2.只要使用到以上特殊特性，都需要实现该接口。
    /// </summary>
    /// <param name="inst">类型实例</param>
    /// <param name="name">字段名</param>
    /// <param name="value">字段值</param>
    bool SetField(T inst, string name, object value) {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IDsonCodec.SetField(object inst, string name, object value) {
        return SetField((T)inst, name, value);
    }
}
}