#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 序列化特征值
///
/// <h3>Style弱化</h3>
/// 序列化框架支持Style会导致较大的复杂度，因此框架层仅挑选部分常用数字+字符串的Style合并到该枚举，以简化复杂度；
/// 如果用户需要更好的控制Style，可通过字段编解码代理自行编码。
///
/// Q：为什么Enum、Null、Zero仅支持字段加全局配置？为什么不支持类型作用域？
/// A：如果我们需要写入Null或者零值，通常是特殊领域的业务，该领域Null值和零值的处理通常一致，因此为该领域定制一个Converter即可。
/// （主要是因为支持类型作用域会导致较大的上下文查询开销）
/// </summary>
[Flags]
public enum SerializeFeatures : uint
{
    /// <summary>
    /// 序列化为引用，
    ///
    /// 1.支持字段 + 类型配置，等效<see cref="Wjybxx.Commons.SerializeReference"/>注解。
    /// 2.不可修改枚举数，代码生成器存在依赖。
    /// </summary>
    SerializeReference = 0x01,
    /// <summary>
    /// 序列化为内联值，即忽略
    ///
    /// 支持字段 + 类型配置，用于字段时可忽略类型的<see cref="Wjybxx.Commons.SerializeReference"/>注解。
    /// </summary>
    SerializeInline = 0x02,
    /// <summary>
    /// 强制写入字段类型名，忽略全局优化（字段级别）
    ///
    /// 注：
    /// 1.全局写的情况下嵌套对象必须写；但全局可选写的情况下嵌套对象就可以强制写。
    /// 2.作用于List/Map字段时，表示强制写入List/Map元素的类型名（List/Map的类型名价值小）。
    /// </summary>
    WriteTypeName = 0x04,
    /// <summary>
    /// 将普通object编码为Array
    /// 
    /// 1.如果开启该选项，将不写入object的字段名，只是顺序写入object的所有字段值。
    /// 2.这可以避免大量的字符串编解码，从而提升性能 - 适用于非持久化场景。
    /// 3.该选项仅对继承<see cref="AbstractDsonCodec"/>的编码器有效。
    /// 4.对象字段不可以有特殊的初始值 -- 否则会被反序列化覆盖。
    /// </summary>
    WriteAsArray = 0x08,

    /// <summary>
    /// 将字典编码为普通数组
    /// 0.标准字典KEY类型：int32/int64/uint32/uint64/string/enum。
    /// 1.如果未指定Map编码选项，当Key为标准类型时，默认编码为Document；否则编码为KV连续数组。
    /// 2.避免Key使用多态类型，虽然底层支持多态，但key使用多态类型并不是个好实践。
    /// 
    /// <code>
    ///  [K1, V1, K2, V2, K3, V3]
    /// </code>
    /// </summary>
    MapAsArray = 0x10,
    /// <summary>
    /// 将字典写为普通文档
    ///
    /// <code>
    /// { K1: V1, K2: V2, K3: V3}
    /// </code>
    /// </summary>
    MapAsDocument = 0x20,
    /// <summary>
    /// 将Pair写为子数组，无兼容性问题
    /// 
    /// <code>
    /// [[K1, V1], [K2, V2], [K3, V3]]
    /// </code>
    /// </summary>
    PairAsArray = 0x40,
    /// <summary>
    /// 将Pair写为子文档
    /// 
    /// <code>
    /// [{K1: V1}, {K2: V2}, {K3: V3}]
    /// </code>
    /// </summary>
    PairAsDocument = 0x80,

    /// <summary>
    /// 将枚举值序列化为数字（默认）
    ///
    /// 1.作用于List/Map时表示将其Value序列化为int值。
    /// 2.该特征值的重要作用在于建立缓存，避免每次写入都查询三级上下文。
    /// </summary>
    EnumAsNumber = 0x01 << 8,
    /// <summary>
    /// 将枚举值序列化为字符串
    ///
    /// 1.作用于List/Map时表示将其Value序列化为字符串。
    /// 2.由于枚举名的稳定性较差，通常不建议开启，因此建议尽量使用字段作用域。
    /// </summary>
    EnumAsString = 0x02 << 8,
    /// <summary>
    /// 枚举键序列化为数字（默认）
    /// </summary>
    EnumKeyAsNumber = 0x04 << 8,
    /// <summary>
    /// 枚举键序列化为字符串（默认int）
    /// </summary>
    EnumKeyAsString = 0x08 << 8,

    /// <summary>
    /// 序列化Null字段
    ///
    /// 注：支持字段、类型、全局作用域。
    /// </summary>
    WriteNullValue = 0x10 << 8,
    /// <summary>
    /// 跳过Null值
    /// </summary>
    SkipNullValue = 0x20 << 8,
    /// <summary>
    /// 序列化零值字段
    /// 
    /// 1.零值：数值类型0，bool类型false
    /// 2.支持字段、类型、全局作用域。
    /// </summary>
    WriteZeroValue = 0x40 << 8,
    /// <summary>
    /// 跳过零值
    /// </summary>
    SkipZeroValue = 0x80 << 8,

    /// <summary>
    /// 将Null值保持为Null值，禁用转换
    ///
    /// Q：为什么序列化需要支持Null值转为非Null值(默认值)，而反序列化不需要？
    /// A：因为程序可以主动处理null和默认值以实现安全性，而序列化得到的数据可能需要更严格的规范以保证安全性。
    /// </summary>
    NullValueAsNull = 0x01 << 16,
    /// <summary>
    /// 将Null字符串值写为空字符串。
    /// </summary>
    NullStringAsEmpty = 0x02 << 16,

    // Style的部分枚举值是重叠的，这通常不影响正确性
#pragma warning disable CA1069
    /// <summary>
    /// 缩进模式 - 默认模式
    ///
    /// 注：用于类型时表示该类型的默认样式，用于字段时表示字段的样式。
    /// </summary>
    ObjectIndent = 0x01 << 20,
    /// <summary>
    /// 流模式 - 线性模式
    /// </summary>
    ObjectFlow = 0x02 << 20,
    /// <summary>
    /// 将List/Map的元素编码为Indent模式（字段级别）
    /// </summary>
    ElementIndent = 0x04 << 20,
    /// <summary>
    /// 将List/Map的元素编码为Flow模式（字段级别）
    /// </summary>
    ElementFlow = 0x08 << 20,

    /// <summary>
    /// 字符串编码为自动引号格式（字段级别）
    ///
    /// 注：无特殊字符时编码为无引号模式，否则编码为引号模式。
    /// </summary>
    StringAutoQuote = 0x10 << 20,
    /// <summary>
    /// 字符串编码为无引号格式（内容不可以包含特殊字符）
    /// </summary>
    StringUnquote = 0x20 << 20,
    /// <summary>
    /// 字符串编码为Dson文本段
    /// </summary>
    StringText = 0x40 << 20,
    /// <summary>
    /// 字符串编码为单行字符模式（内容不可以包含换行符）
    /// </summary>
    StringLine = 0x80 << 20,

    /// <summary>
    /// 数字编码时带上类型符号，可与其它格式共存（字段级别）
    /// </summary>
    NumberTyped = 0x10 << 20,
    /// <summary>
    /// int32/int64编码为无符号整数（可与其它格式共存）
    /// </summary>
    NumberUnsigned = 0x20 << 20,
    /// <summary>
    /// 数字编码为16进制（不支持浮点数）
    /// </summary>
    NumberHex = 0x40 << 20,

    /// <summary>
    /// 使用蛇形命名法（默认字段名）
    /// 
    /// 注：只适用编辑期APT生成，非运行时属性。
    /// </summary>
    SnakeCase = 0x01 << 28,
#pragma warning restore CA1069

    /// <summary>
    /// Map编码样式的掩码
    /// </summary>
    MaskMapStyles = MapAsArray | MapAsDocument | PairAsArray | PairAsDocument,
    /// <summary>
    /// String编码样式的掩码
    /// </summary>
    MaskStringStyles = StringAutoQuote | StringUnquote | StringText | StringLine,
    /// <summary>
    /// Number编码样式的掩码
    /// </summary>
    MaskNumberStyles = NumberTyped | NumberUnsigned | NumberHex,
    /// <summary>
    /// List/Map元素的序列化特征值掩码（还有部分需要手动转换）
    /// </summary>
    MaskElementFeatures = SerializeReference | SerializeInline | WriteTypeName
                          | EnumAsNumber | EnumAsString
                          | NullValueAsNull | NullStringAsEmpty
                          | MaskStringStyles | MaskNumberStyles
}
}