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
/// </summary>
[Flags]
public enum SerializeFeatures : uint
{
    /// <summary>
    /// 序列化为引用
    ///
    /// 1.支持字段 + 类型配置，等效<see cref="Wjybxx.Commons.SerializeReference"/>注解。
    /// 2.不可修改枚举数，代码生成器存在依赖。
    /// </summary>
    SerializeReference = 0x01,
    /// <summary>
    /// 序列化为内联值，即忽略
    ///
    /// 1.支持字段 + 类型配置，用于字段时可忽略类型的<see cref="Wjybxx.Commons.SerializeReference"/>注解。
    /// 2.用于字符串字段时，表示禁用SST。
    /// </summary>
    SerializeInline = 0x02,

    // 集合特征值之间可重复
#pragma warning disable CA1069
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
    PairAsArray = 0x30,
    /// <summary>
    /// 将Pair写为子文档
    /// 
    /// <code>
    /// [{K1: V1}, {K2: V2}, {K3: V3}]
    /// </code>
    /// </summary>
    PairAsDocument = 0x40,
#pragma warning restore CA1069

    /// <summary>
    /// 序列化Null字段
    ///
    /// 注：支持字段、类型、全局作用域。
    /// </summary>
    WriteNullValue = 0x01 << 8,
    /// <summary>
    /// 跳过Null值
    /// </summary>
    SkipNullValue = 0x02 << 8,
    /// <summary>
    /// 序列化零值字段
    /// 
    /// 1.零值：数值类型0，bool类型false
    /// 2.支持字段、类型、全局作用域。
    /// </summary>
    WriteZeroValue = 0x04 << 8,
    /// <summary>
    /// 跳过零值
    /// </summary>
    SkipZeroValue = 0x08 << 8,

    //--注意：原子值的特征值可以重叠，如String和Enum，但原子值不能和容器类型重叠
#pragma warning disable CA1069
    /// <summary>
    /// 将枚举值序列化为数字（默认）
    ///
    /// 1.作用于List/Map时表示将其Value序列化为int值。
    /// 2.该特征值的重要作用在于建立缓存，避免每次写入都查询三级上下文。
    /// </summary>
    EnumAsNumber = 0x10 << 8,
    /// <summary>
    /// 将枚举值序列化为字符串（默认int）
    ///
    /// 1.作用于List/Map时表示将其Value序列化为字符串。
    /// 2.由于枚举名的稳定性较差，因此只可用于字段或枚举定义，用在它处无效。
    /// 3.字典的key需要单独标记，未标记的情况下区域枚举定义上的特征值。
    /// </summary>
    EnumAsString = 0x20 << 8,
    /// <summary>
    /// 枚举键序列化为数字（默认）
    /// </summary>
    EnumKeyAsNumber = 0x40 << 8,
    /// <summary>
    /// 枚举键序列化为字符串（默认int）
    /// </summary>
    EnumKeyAsString = 0x80 << 8,

    /// <summary>
    /// 将Null字符串值写为空字符串。
    /// 
    /// 注：虽然字典的Key也可能为字符串，但字典的Key通常不应该为null。
    /// </summary>
    NullStringAsEmpty = 0x10 << 8,
    /// <summary>
    /// 将Null值保持为Null值，禁用转换
    ///
    /// 注：禁用转换后如果想写入null值，需启用<see cref="WriteNullValue"/>。
    /// </summary>
    NullStringAsNull = 0x20 << 8,
#pragma warning restore CA1069

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

    // 字符串样式是枚举值
    /// <summary>
    /// 字符串编码为无引号格式（不可以包含特殊字符）
    /// </summary>
    StringUnquote = 0x10 << 20,
    /// <summary>
    /// 字符串编码为单行字符串模式（不可以包含换行符）
    /// </summary>
    StringLine = 0x20 << 20,
    /// <summary>
    /// 字符串编码为普通文本块
    /// </summary>
    StringText = 0x30 << 20,
    /// <summary>
    /// 字符串编码为Dson文本块
    /// </summary>
    StringDsonText = 0x40 << 20,

    // 数字样式是Flags
    /// <summary>
    /// 数字编码为16进制（不支持浮点数）
    /// </summary>
    NumberHex = 0x10 << 20,
    /// <summary>
    /// 数字编码时带上类型符号，可与其它格式共存（字段级别）
    /// </summary>
    NumberTyped = 0x20 << 20,
    /// <summary>
    /// int32/int64编码为有符号16进制
    /// </summary>
    NumberSigned = 0x40 << 20,
    /// <summary>
    /// int32/int64编码为固定长度16进制
    /// </summary>
    NumberFixed = 0x80 << 20,

    /// <summary>
    /// 限定Double4/Long4/Fxp4长度为2
    /// 注：该特征值并不直接作用于Double4等类型，而是告知用户的Codec将自定义结构转换为Double4写入时要写入的分量数。
    /// </summary>
    Vector2 = 0x10 << 20,
    /// <summary>
    /// 限定Double4/Long4/Fxp4长度为3
    /// </summary>
    Vector3 = 0x20 << 20,

    /// <summary>
    /// 限定浮点数保留小数点后3位(慎用)
    /// </summary>
    NumberNoExponent3 = 0x01 << 28,
    /// <summary>
    /// 限定浮点数保留小数点后7位(慎用)
    /// </summary>
    NumberNoExponent7 = 0x02 << 28,

    /// <summary>
    /// Map编码样式的掩码
    /// </summary>
    MaskMapStyles = MapAsArray | MapAsDocument | PairAsArray | PairAsDocument,
    /// <summary>
    /// String编码样式的掩码
    /// </summary>
    MaskStringStyles = StringUnquote | StringLine | StringText | StringDsonText,
    /// <summary>
    /// Number编码样式的掩码
    /// </summary>
    MaskNumberStyles = NumberHex | NumberTyped | NumberSigned | NumberFixed
                       | NumberNoExponent3 | NumberNoExponent7,
    /// <summary>
    /// 
    /// </summary>
    MaskVectorStyles = Vector2 | Vector3,

    /// <summary>
    /// List/Map元素的序列化特征值掩码（还有部分需要手动转换）
    /// </summary>
    MaskElementFeatures = SerializeReference | SerializeInline
                                             | EnumAsNumber | EnumAsString
                                             | NullStringAsNull | NullStringAsEmpty
                                             | MaskStringStyles | MaskNumberStyles | MaskVectorStyles
}
}