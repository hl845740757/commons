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
/// Dson数据类型枚举
/// </summary>
public enum DsonType : sbyte
{
    /** 对象的结束标识 */
    EndOfObject = 0,

    Int32 = 1,
    Int64 = 2,
    Float = 3,
    Double = 4,
    Bool = 5,
    String = 6,
    Null = 7,
    Binary = 8,

    /// <summary>
    /// 对象指针
    /// </summary>
    Pointer = 11,
    /// <summary>
    /// 轻量对象指针
    /// </summary>
    LitePointer = 12,
    /// <summary>
    /// 日期时间
    /// </summary>
    DateTime = 13,
    /// <summary>
    /// 时间戳
    /// </summary>
    Timestamp = 14,

    /// <summary>
    /// 对象头信息，与Object类型编码格式类似
    /// 但header不可以再直接嵌入header
    /// </summary>
    Header = 29,
    /// <summary>
    /// 数组(v,v,v...)
    /// </summary>
    Array = 30,
    /// <summary>
    /// 普通对象(k,v,k,v...)
    /// </summary>
    Object = 31,
}

/// <summary>
/// DsonType的工具类
/// </summary>
public static class DsonTypes
{
    private static readonly DsonType[] LOOK_UP;

    /** 用于表示无效DsonType的共享值 */
    public static readonly DsonType INVALID = (DsonType)(-1);

    static DsonTypes() {
        LOOK_UP = new DsonType[(int)DsonType.Object + 1];
#if UNITY_EDITOR
        foreach (object dsonType in Enum.GetValues(typeof(DsonType))) {
            LOOK_UP[(int)dsonType] = (DsonType)dsonType;
        }
#else
        foreach (var dsonType in Enum.GetValues<DsonType>()) {
            LOOK_UP[(int)dsonType] = dsonType;
        }
#endif
    }

    /** DsonType是否表示Dson的Number */
    public static bool IsNumber(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => true,
            DsonType.Int64 => true,
            DsonType.Float => true,
            DsonType.Double => true,
            _ => false
        };
    }

    /** DsonType关联的值在二进制编码时是否包含WireType */
    public static bool HasWireType(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => true,
            DsonType.Int64 => true,
            _ => false
        };
    }

    /** DsonType是否表示容器类型；header不属于普通意义上的容器 */
    public static bool IsContainer(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Array;
    }

    /** DsonType是否是容器类型或Header */
    public static bool IsContainerOrHeader(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Array || dsonType == DsonType.Header;
    }

    /** Dson是否是KV结构 */
    public static bool IsObjectLike(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Header;
    }

    /** 通过Number获取对应的枚举 */
    public static DsonType ForNumber(int number) {
        return LOOK_UP[number];
    }
}
}