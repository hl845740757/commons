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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// <see cref="Double4"/>的文本输出格式
///
/// 注：
/// 1.解码时固定顺序读取，忽略字段名。
/// 2.慎重选择编码样式，选择错误可能导致数据丢失。
/// </summary>
[Flags]
public enum Double4Style
{
    /// <summary>
    /// 打印为数组格式(0)
    ///
    /// <![CDATA[
    /// [@D4 v0, v1, v2, v3]
    /// [@D4 v0, v1, v2]
    /// [@D4 v0, v1]
    /// ]]>
    /// </summary>
    Array = 0x00,
    /// <summary>
    /// 打印为向量格式(1)
    ///
    /// <![CDATA[
    /// {@D4 X: 1, Y: 1, z: 1, w: 1}
    /// {@D4 X: 1, Y: 1, z: 1}
    /// {@D4 X: 1, Y: 1}
    /// ]]>
    /// </summary>
    Vector = 0x01,
    /// <summary>
    /// 打印为颜色值格式(2)
    ///
    /// <![CDATA[
    /// {@D4 r: 1, g: 1, b: 1, a: 1}
    /// {@D4 r: 1, g: 1, b: 1}
    /// ]]>
    /// </summary>
    Rgba = 0x02,
    /// <summary>
    /// 打印为矩形值格式(3)
    ///
    /// <![CDATA[
    /// {@D4 x: 1, y: 1, w: 50, h: 50}
    /// ]]>
    /// 注：最大基础样式，不再扩展。
    /// </summary>
    Rect = 0x03,
    /// <summary>
    /// 限定Double4的长度为2，即只打印前两个数
    /// </summary>
    Len2 = 0x04,
    /// <summary>
    /// 限定Double4的长度为3，即只打印前三个数
    /// </summary>
    Len3 = 0x08,

    /// <summary>
    /// 浮点数禁用科学计数法，并最多保留小数点后3位(向最近的偶数舍入) -- 可能导致反序列化结果不相等
    /// </summary>
    NoExponent3 = 0x10,
    /// <summary>
    /// 浮点数禁用科学计数法，并最多保留小数点后7位(向最近的偶数舍入) -- 可能导致反序列化结果不相等
    /// </summary>
    NoExponent7 = 0x20,
    /// <summary>
    /// Value截断为整数 -- 可能导致反序列化结果不相等
    /// </summary>
    Integer = 0x40,
}
}