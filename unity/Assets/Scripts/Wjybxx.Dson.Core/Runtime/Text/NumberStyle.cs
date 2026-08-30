#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 该枚举仅用于代码生成工具，用户直接使用<see cref="NumberStyles"/>
/// (需要保持枚举名和<see cref="NumberStyles"/>中的属性相同)
/// </summary>
[Flags]
public enum NumberStyle
{
    /// <summary>
    /// 普通打印 -- 超过表示范围时会添加类型标签
    /// </summary>
    Simple = 0,
    /// <summary>
    /// 十六进制，必定追加类型，默认无符号
    /// </summary>
    Hex = 0x01,
    /// <summary>
    /// 二进制，必定追加类型，默认无符号
    /// </summary>
    Binary = 0x02,

    /// <summary>
    /// 固定打印类型
    /// </summary>
    Typed = 0x10,
    /// <summary>
    /// 打印为有符号数，适用十六进制和二进制
    /// </summary>
    Signed = 0x20,
    /// <summary>
    /// 固定长度编码（全Bit编码），适用十六进制和二进制
    /// </summary>
    Fixed = 0x40,

    /// <summary>
    /// 浮点数禁用科学计数法，并最多保留小数点后3位(向最近的偶数舍入) -- 可能导致反序列化结果不相等
    /// </summary>
    NoExponent3 = 0x01 << 8,
    /// <summary>
    /// 浮点数禁用科学计数法，并最多保留小数点后7位(向最近的偶数舍入) -- 可能导致反序列化结果不相等
    /// </summary>
    NoExponent7 = 0x02 << 8,

    #region 常用组合

    /// <summary>
    /// 输出为无符号16进制
    /// </summary>
    SignedHex = Signed | Hex,
    /// <summary>
    /// 固定全长度的16进制
    /// </summary>
    FixedHex = Fixed | Hex,
    /// <summary>
    /// 固定全长度的二进制
    /// </summary>
    FixedBinary = Fixed | Binary,
    /// <summary>
    /// 打印类型并保留小数点后3位，用于float类型
    /// </summary>
    TypedNoExponent3 = Typed | NoExponent3,
    /// <summary>
    /// 打印类型并保留小数点后7位，用于float类型
    /// </summary>
    TypedNoExponent7 = Typed | NoExponent7,

    #endregion

    /// <summary>
    /// 进制掩码
    /// </summary>
    MaskRadixes = 0x0F,
}
}