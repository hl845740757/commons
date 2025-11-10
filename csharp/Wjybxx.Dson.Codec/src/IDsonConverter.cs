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
using System.IO;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
///
/// <h3>对象图</h3>
/// 最新版本已支持对象图，Read方法默认只读取第一个对象以及它引用的对象；
/// 要想读取所有的对象，需要调用DsonCollection相关接口。
/// </summary>
public interface IDsonConverter : IConverter
{
    #region convert

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="output">输出流</param>
    /// <param name="features">特征值</param>
    void Write(object value, Type declaredType, IDsonOutput output, SerializeFeatures features = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">数据源</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">反序列化特征值</param>
    /// <param name="factory">实例工厂</param>
    /// <returns></returns>
    object Read(IDsonInput input, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    /// <summary>
    /// 将一个对象转换为字节数组
    /// 
    /// 1.如果对象的运行时类型和声明类型一致，则可省去编码结果中的类型信息。
    /// 2.被引用的对象会编码为顶层对象。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">序列化特征值</param>
    /// <returns></returns>
    string WriteAsDson(object value, Type declaredType, SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取一个对象
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">反序列化特征值</param>
    /// <param name="factory">实例工厂</param>
    /// <returns></returns>
    object ReadFromDson(string source, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    /// <summary>
    /// 将一个对象写入Writer
    /// (默认不关闭writer)
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="writer">接收输出</param>
    /// <param name="features">特征值</param>
    void WriteAsDson(object value, Type declaredType, TextWriter writer, SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取一个对象
    /// (默认不关闭Reader)
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">反序列化特征值</param>
    /// <param name="factory">实例工厂</param>
    object ReadFromDson(TextReader source, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    /// <summary>
    /// 序列化集合信息
    ///
    /// 注：与WriteAsDson的区别在于，该方法不写入集合自身的数据 —— 建议通过测试用例观察。
    /// </summary>
    /// <param name="collection">对象集合</param>
    /// <param name="declaredType">集合元素的声明类型</param>
    /// <param name="features">序列化特征值</param>
    string WriteCollectionAsDson(IEnumerable<object> collection, Type declaredType, SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取所有对象
    /// 
    /// 注：泛型T用于处理集合类型协变问题。
    /// </summary>
    /// <param name="dson">对象集合</param>
    /// <param name="declaredType">集合元素的声明类型</param>
    /// <param name="features">反序列化特征值</param>
    /// <param name="factory">集合元素的factory</param>
    List<T> ReadCollectionFromDson<T>(string dson, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null);

    /// <summary>
    /// 序列化多个对象，保留对象引用关系
    /// </summary>
    /// <param name="collection">对象集合</param>
    /// <param name="declaredType">集合元素的声明类型</param>
    /// <param name="features">序列化特征值</param>
    /// <returns></returns>
    DsonArray<string> WriteCollectionAsDsonCollection(IEnumerable<object> collection, Type declaredType,
                                                      SerializeFeatures features = default);

    /// <summary>
    /// 从数据源中读取所有对象
    /// 
    /// 注：用户可以缓存资源文件解析得到的<see cref="DsonArray{TK}"/>对象，以支持快速复制对象。
    /// </summary>
    /// <param name="collection">对象集合</param>
    /// <param name="declaredType">集合元素的声明类型</param>
    /// <param name="features">反序列化特征值</param>
    /// <param name="factory">集合元素的factory</param>
    List<T> ReadCollectionFromDsonCollection<T>(DsonArray<string> collection, Type declaredType,
                                                DeserializeFeatures features = default, Func<object>? factory = null);

    #endregion

    #region other

    /// <summary>
    /// 序列化选项
    /// </summary>
    ConverterOptions Options { get; }

    /// <summary>
    /// 类型源数据注册表
    /// </summary>
    ITypeMetaRegistry TypeMetaRegistry { get; }

    /// <summary>
    /// Codec注册表
    /// </summary>
    IDsonCodecRegistry CodecRegistry { get; }

    /// <summary>
    /// 在共享其它属性的情况，创建一个持有给定options的Converter。
    /// 我们通过options来控制Converter的上下文。
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    IDsonConverter WithOptions(ConverterOptions options);

    #endregion
}
}