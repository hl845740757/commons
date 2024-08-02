#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 这里提供默认数字格式化方式
/// </summary>
public enum NumberStyle : byte
{
    /** 普通打印 -- 超过表示范围时会添加类型标签 */
    Simple = 0,
    /** 总是打印类型 */
    Typed = 1,

    /** 打印为无符号数 -- 超过表示范围时会添加类型标签；通常用于打印Flags类型 */
    Unsigned = 2,
    /** 打印为带类型无符号数；通常用于打印Flags类型 */
    TypedUnsigned = 3,

    /** 16进制，打印正负号 -- 不支持浮点数 */
    SignedHex = 4,
    /** 无符号16进制，按位打印 -- 不支持浮点数 */
    UnsignedHex = 5,

    /** 2进制，打印正负号 -- 不支持浮点数 */
    SignedBinary = 6,
    /** 无符号2进制，按位打印 -- 不支持浮点数 */
    UnsignedBinary = 7,

    /** 固定位数2进制，按位打印 -- 不支持浮点数 */
    FixedBinary = 8,
}
}