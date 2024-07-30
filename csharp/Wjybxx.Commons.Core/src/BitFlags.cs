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

using System.Runtime.CompilerServices;

namespace Wjybxx.Commons
{
/// <summary>
/// Flags工具类
/// </summary>
public class BitFlags
{
    #region int

    /** 是否设置了mask关联的任意bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnySet(int flags, int mask) {
        return (flags & mask) != 0;
    }

    /** 是否设置了mask关联的所有bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(int flags, int mask) {
        return (flags & mask) == mask;
    }

    /** 启用指定bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Set(int flags, int mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unset(int flags, int mask) {
        return (flags & ~mask);
    }

    /** 设置指定bit位 -- 全0或全1 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Set(int flags, int mask, bool enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSetAt(int flags, int idx) {
        int mask = 1 << idx;
        return (flags & mask) != 0;
    }

    /** 是否未设置指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotSetAt(int flags, int idx) {
        int mask = 1 << idx;
        return (flags & mask) == 0;
    }

    /** 设置指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetAt(int flags, int idx, bool enable) {
        int mask = 1 << idx;
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /// <summary>
    /// 获取bit表示的数字值
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <param name="offset">需要偏移的bit数</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetField(int flags, int mask, int offset) {
        return (flags & mask) >> offset;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <param name="offset">需要偏移的bit数</param>
    /// <param name="value">字段的值</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetField(int flags, int mask, int offset, int value) {
        int offsetValue = (value << offset) & mask; // & mask 去除非法位
        flags &= ~mask; // 去除旧值
        return flags | offsetValue;
    }

    /// <summary>
    /// 将字段部分设置为0
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UnsetField(int flags, int mask) {
        return flags & ~mask;
    }

    #endregion

    #region long

    /** 是否设置了mask关联的任意bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnySet(long flags, long mask) {
        return (flags & mask) != 0;
    }

    /** 是否设置了mask关联的所有bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(long flags, long mask) {
        return (flags & mask) == mask;
    }

    /** 启用指定bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Set(long flags, long mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Unset(long flags, long mask) {
        return (flags & ~mask);
    }

    /** 设置指定bit位 -- 全0或全1 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Set(long flags, long mask, bool enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSetAt(long flags, int idx) {
        long mask = 1L << idx;
        return (flags & mask) != 0;
    }

    /** 是否未设置指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotSetAt(long flags, int idx) {
        long mask = 1L << idx;
        return (flags & mask) == 0;
    }

    /** 设置指定下标的bit */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SetAt(long flags, int idx, bool enable) {
        long mask = 1L << idx;
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /// <summary>
    /// 获取bit表示的数字值
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <param name="offset">需要偏移的bit数</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetField(long flags, long mask, int offset) {
        return (flags & mask) >> offset;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <param name="offset">需要偏移的bit数</param>
    /// <param name="value">字段的值</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SetField(long flags, long mask, int offset, long value) {
        long offsetValue = (value << offset) & mask; // & mask 去除非法位
        flags &= ~mask; // 去除旧值
        return flags | offsetValue;
    }

    /// <summary>
    /// 将字段部分设置为0
    /// </summary>
    /// <param name="flags">flags当前值</param>
    /// <param name="mask">字段的掩码</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UnsetField(long flags, long mask) {
        return flags & ~mask;
    }

    #endregion
}
}