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

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.List;
import java.util.RandomAccess;

import static cn.wjybxx.base.ObjectUtils.toStringIfNotNull;

/**
 * 这里包含了一些我们常用的前置条件检查，没有做太多的优化。
 *
 * @author wjybxx
 * date - 2023/4/27
 */
@SuppressWarnings("unused")
public class Preconditions {

    // region checkArgument

    public static void checkArgument(boolean b) {
        if (!b) {
            throw new IllegalArgumentException();
        }
    }

    public static void checkArgument(boolean b, @Nullable Object message) {
        if (!b) {
            throw new IllegalArgumentException(toStringIfNotNull(message));
        }
    }

    public static void checkState(boolean expression) {
        if (!expression) {
            throw new IllegalStateException();
        }
    }

    public static void checkState(boolean expression, @Nullable Object message) {
        if (!expression) {
            throw new IllegalStateException(toStringIfNotNull(message));
        }
    }
    // endregion

    // region null检查

    public static <T> T checkNotNull(T v) {
        if (v == null) throw new NullPointerException();
        return v;
    }

    public static <T> T checkNotNull(T v, @Nullable Object message) {
        if (v == null) throw new NullPointerException(toStringIfNotNull(message));
        return v;
    }

    // endregion

    // region 数字检查

    /** 检查数字是否为正数 */
    public static int checkPositive(int v) {
        if (v <= 0) {
            throw new IllegalArgumentException(checkPositiveMsg(v, null));
        }
        return v;
    }

    /** 检查数字是否为正数 */
    public static int checkPositive(int v, String desc) {
        if (v <= 0) {
            throw new IllegalArgumentException(checkPositiveMsg(v, desc));
        }
        return v;
    }

    /** 检查数字是否为正数 */
    public static long checkPositive(long v) {
        if (v <= 0) {
            throw new IllegalArgumentException(checkPositiveMsg(v, null));
        }
        return v;
    }

    /** 检查数字是否为正数 */
    public static long checkPositive(long v, String desc) {
        if (v <= 0) {
            throw new IllegalArgumentException(checkPositiveMsg(v, desc));
        }
        return v;
    }

    private static String checkPositiveMsg(long v, String desc) {
        if (desc == null) {
            return String.format("value expected positive, but found: %d", v);
        } else {
            return String.format("%s expected positive, but found: %d", desc, v);
        }
    }

    //

    /** 检查数字似乎为‘非负数’ */
    public static int checkNonNegative(int v) {
        if (v < 0) {
            throw new IllegalArgumentException(checkNonNegativeMsg(v, null));
        }
        return v;
    }

    /** 检查数字似乎为‘非负数’ */
    public static int checkNonNegative(int v, String desc) {
        if (v < 0) {
            throw new IllegalArgumentException(checkNonNegativeMsg(v, desc));
        }
        return v;
    }

    /** 检查数字似乎为‘非负数’ */
    public static long checkNonNegative(long v) {
        if (v < 0) {
            throw new IllegalArgumentException(checkNonNegativeMsg(v, null));
        }
        return v;
    }

    /** 检查数字似乎为‘非负数’ */
    public static long checkNonNegative(long v, String desc) {
        if (v < 0) {
            throw new IllegalArgumentException(checkNonNegativeMsg(v, desc));
        }
        return v;
    }

    private static String checkNonNegativeMsg(long v, String desc) {
        if (desc == null) {
            return String.format("value expected nonnegative, but found: %d", v);
        } else {
            return String.format("%s expected nonnegative, but found: %d", desc, v);
        }
    }

    //

    /** 检查value是否在指定范围内 */
    public static int checkBetween(int v, int min, int max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(checkBetweenMsg(v, min, max, null));
        }
        return v;
    }

    /** 检查value是否在指定范围内 */
    public static int checkBetween(int v, int min, int max, String desc) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(checkBetweenMsg(v, min, max, desc));
        }
        return v;
    }

    /** 检查value是否在指定范围内 */
    public static long checkBetween(long v, long min, long max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(checkBetweenMsg(v, min, max, null));
        }
        return v;
    }

    /** 检查value是否在指定范围内 */
    public static long checkBetween(long v, long min, long max, String desc) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(checkBetweenMsg(v, min, max, desc));
        }
        return v;
    }

    private static String checkBetweenMsg(long v, long min, long max, String desc) {
        if (desc == null) {
            return String.format("value expected between range[%d, %d], but found: %d", min, max, v);
        } else {
            return String.format("%s expected between range[%d, %d], but found: %d", desc, min, max, v);
        }
    }
    // endregion

    // region 字符串检查

    public static String checkNotEmpty(String value) {
        return checkNotEmpty(value, null);
    }

    public static String checkNotEmpty(String value, @Nullable String desc) {
        if (ObjectUtils.isEmpty(value)) {
            throwArgumentException("value cant be empty", "%s cant be empty", desc);
        }
        return value;
    }

    public static String checkNotBlank(String value) {
        return checkNotBlank(value, null);
    }

    public static String checkNotBlank(String value, @Nullable String desc) {
        if (ObjectUtils.isBlank(value)) {
            throwArgumentException("value cant be blank", "%s cant be blank", desc);
        }
        return value;
    }

    public static String checkNoneWhiteSpace(String value) {
        return checkNoneWhiteSpace(value, null);
    }

    public static String checkNoneWhiteSpace(String value, @Nullable String desc) {
        if (ObjectUtils.containsWhitespace(value)) {
            throwArgumentException("value cant contains whitespace", "%s cant contains whitespace", desc);
        }
        return value;
    }

    // endregion

    // region 集合检查

    public static void checkNotEmpty(Collection<?> collection) {
        checkNotEmpty(collection, null);
    }

    public static void checkNotEmpty(Collection<?> collection, @Nullable String desc) {
        if (collection == null || collection.isEmpty()) {
            throwArgumentException("collection cant be empty", "%s cant be empty", desc);
        }
    }

    public static void checkNotEmpty(Object[] array) {
        checkNotEmpty(array, null);
    }

    public static void checkNotEmpty(Object[] array, @Nullable String desc) {
        if (array == null || array.length == 0) {
            throwArgumentException("array cant be empty", "%s cant be empty", desc);
        }
    }

    /** 检查集合里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Collection<?> c) {
        checkNullElements(c, null);
    }

    public static void checkNullElements(Collection<?> c, @Nullable String desc) {
        if (c instanceof RandomAccess) {
            List<?> list = (List<?>) c;
            for (int i = 0; i < list.size(); i++) {
                if (list.get(i) == null) {
                    throwArgumentException("collection contains null elements", "%s contains null elements", desc);
                }
            }
        } else {
            for (Object element : c) {
                if (element == null) {
                    throwArgumentException("collection contains null elements", "%s contains null elements", desc);
                }
            }
        }
    }

    /** 检查数组里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Object[] array) {
        checkNullElements(array, null);
    }

    /** 检查数组里是否存在null，如果元素里存在null则抛出异常 */
    public static void checkNullElements(Object[] array, String desc) {
        for (Object element : array) {
            if (element == null) {
                throwArgumentException("array contains null elements", "%s contains null elements", desc);
            }
        }
    }

    private static void throwArgumentException(String defaultMsg, String formatMsg, String desc) {
        if (desc == null) {
            throw new IllegalArgumentException(defaultMsg);
        } else {
            throw new IllegalArgumentException(formatMsg.formatted(desc));
        }
    }
    // endregion

    // region 数组下标

    public static int checkElementIndex(int index, int size) {
        return checkElementIndex(index, size, "index");
    }

    public static int checkElementIndex(int index, int size, String desc) {
        // Carefully optimized for execution by hotspot (explanatory comment above)
        if (index < 0 || index >= size) {
            throw new IndexOutOfBoundsException(badElementIndex(index, size, desc));
        }
        return index;
    }

    private static String badElementIndex(int index, int size, String desc) {
        if (desc == null) desc = "value";
        if (index < 0) {
            return String.format("%s (%s) must not be negative", desc, index);
        } else if (size < 0) {
            throw new IllegalArgumentException("negative size: " + size);
        } else { // index >= size
            return String.format("%s (%s) must be less than size (%s)", desc, index, size);
        }
    }

    // endregion

}