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
 * 比特选项工具类
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class BitFlags {

    //region int

    /** 是否启用了所有选项 */
    public static boolean isSet(int flags, int mask) {
        return (flags & mask) == mask;
    }

    /**
     * 是否未启用选项。
     * 1.禁用任意bit即为未启用；
     * 2.和{@link #isSet(int, int)}相反关系
     */
    public static boolean isNotSet(int flags, int mask) {
        return (flags & mask) != mask;
    }

    /** 是否启用了任意选项 */
    public static boolean isAnySet(int flags, int mask) {
        return (flags & mask) != 0;
    }

    /** 是否禁用了所有选项 */
    public static boolean isAllNotSet(int flags, int mask) {
        return (flags & mask) == 0;
    }

    /** 启用指定bit */
    public static int set(int flags, int mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    public static int unset(int flags, int mask) {
        return (flags & ~mask);
    }

    public static int set(int flags, int mask, boolean enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    public static boolean isSetAt(int flags, int idx) {
        return isSet(flags, 1 << idx);
    }

    /** 是否未设置指定下标的bit */
    public static boolean isNotSetAt(int flags, int idx) {
        return isNotSet(flags, 1 << idx);
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

    // endregion

    // region long

    /** 是否启用了所有选项 */
    public static boolean isSet(long flags, long mask) {
        return (flags & mask) == mask;
    }

    /**
     * 是否未启用选项。
     * 1.禁用任意bit即为未启用；
     * 2.和{@link #isSet(long, long)}相反关系
     */
    public static boolean isNotSet(long flags, long mask) {
        return (flags & mask) != mask;
    }

    /** 是否启用了任意选项 */
    public static boolean isAnySet(long flags, long mask) {
        return (flags & mask) != 0;
    }

    /** 是否禁用了所有选项 */
    public static boolean isAllNotSet(long flags, long mask) {
        return (flags & mask) == 0;
    }

    /** 启用指定bit */
    public static long set(long flags, long mask) {
        return flags | mask;
    }

    /** 删除指定bit */
    public static long unset(long flags, long mask) {
        return (flags & ~mask);
    }

    public static long set(long flags, long mask, boolean enable) {
        if (enable) {
            return (flags | mask);
        } else {
            return (flags & ~mask);
        }
    }

    /** 是否设置了指定下标的bit */
    public static boolean isSetAt(long flags, long idx) {
        return isSet(flags, 1L << idx);
    }

    /** 是否未设置指定下标的bit */
    public static boolean isNotSetAt(long flags, long idx) {
        return isNotSet(flags, 1L << idx);
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
    // endregion

}