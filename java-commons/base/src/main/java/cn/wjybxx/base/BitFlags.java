/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.base;


/**
 * 比特标记工具类
 * tips: Commons-Lang3有个{@code BitField}类
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class BitFlags {

    //region int

    /** 是否设置了任意bit */
    public static boolean isSet(int flags, int mask) {
        return (flags & mask) != 0;
    }

    /** 是否设置了所有bit */
    public static boolean isAllSet(int flags, int mask) {
        return (flags & mask) == mask;
    }

    /** 启用指定bit */
    public static int set(int flags, int mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    public static int unset(int flags, int mask) {
        return (flags & ~mask);
    }

    /** 设置指定bit位 -- 全0或全1 */
    public static int set(int flags, int mask, boolean enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    public static boolean isSetAt(int flags, int idx) {
        int mask = 1 << idx;
        return (flags & mask) != 0;
    }

    /** 是否未设置指定下标的bit */
    public static boolean isNotSetAt(int flags, int idx) {
        int mask = 1 << idx;
        return (flags & mask) == 0;
    }

    /** 设置指定下标的bit */
    public static int setAt(int flags, int idx, boolean enable) {
        int mask = 1 << idx;
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /**
     * @param mask   字段的掩码
     * @param offset 需要偏移的bit数
     */
    public static int getField(int flags, int mask, int offset) {
        return (flags & mask) >> offset;
    }

    public static int setField(int flags, int mask, int offset, int value) {
        int offsetValue = (value << offset) & mask; // & mask 去除非法位
        flags &= ~mask; // 去除旧值
        return flags & offsetValue;
    }

    public static int unsetField(int flags, int mask) {
        return flags & ~mask;
    }

    // endregion

    // region long

    /** 是否设置了任意bit */
    public static boolean isSet(long flags, long mask) {
        return (flags & mask) != 0;
    }

    /** 是否设置了所有bit */
    public static boolean isAllSet(long flags, long mask) {
        return (flags & mask) == mask;
    }

    /** 启用指定bit */
    public static long set(long flags, long mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    public static long unset(long flags, long mask) {
        return (flags & ~mask);
    }

    /** 设置指定bit位 -- 全0或全1 */
    public static long set(long flags, long mask, boolean enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    public static boolean isSetAt(long flags, long idx) {
        long mask = 1L << idx;
        return (flags & mask) != 0;
    }

    /** 是否未设置指定下标的bit */
    public static boolean isNotSetAt(long flags, long idx) {
        long mask = 1L << idx;
        return (flags & mask) == 0;
    }

    /** 设置指定下标的bit */
    public static long setAt(long flags, long idx, boolean enable) {
        long mask = 1L << idx;
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /**
     * @param mask   字段的掩码
     * @param offset 需要偏移的bit数
     */
    public static long getField(long flags, long mask, int offset) {
        return (flags & mask) >> offset;
    }

    public static long setField(long flags, long mask, int offset, long value) {
        long offsetValue = (value << offset) & mask; // & mask 去除非法位
        flags &= ~mask; // 去除旧值
        return flags & offsetValue;
    }

    public static long unsetField(long flags, long mask) {
        return flags & ~mask;
    }
    // endregion

}