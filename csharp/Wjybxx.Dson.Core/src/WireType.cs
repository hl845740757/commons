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

using System;

namespace Wjybxx.Dson
{
/// <summary>
/// 数字类型字段的编码方式
/// </summary>
public enum WireType
{
    /// <summary>
    /// 简单变长编码
    /// 1.该编码对于int32的负数数据而言，将固定占用10个字节，正数时等同于UINT编码；
    /// 2.该编码对于int64的负数数据而言，也固定占用10个字节，正数时等同于UINT编码；
    /// </summary>
    VarInt = 0,

    /// <summary>
    /// 按照无符号格式优化编码
    /// 1.该编码对于int32的负数数据而言，将固定占用5个字节；
    /// 2.该编码对于int64的负数数据而言，将固定占用10个字节；
    /// </summary>
    Uint = 1,

    /// <summary>
    /// 按照有符号数格式优化编码(ZigZag编码)
    /// </summary>
    Sint = 2,

    /// <summary>
    /// 固定长度编码
    /// 1. int32 固定4字节
    /// 2. int64 固定8字节
    /// </summary>
    Fixed = 3,
}

/// <summary>
/// WireType的工具类
/// </summary>
public static class WireTypes
{
    /** 通过number查找关联枚举 */
    public static WireType ForNumber(int number) {
        return number switch
        {
            0 => WireType.VarInt,
            1 => WireType.Uint,
            2 => WireType.Sint,
            3 => WireType.Fixed,
            _ => throw new ArgumentException(nameof(number))
        };
    }
}
}