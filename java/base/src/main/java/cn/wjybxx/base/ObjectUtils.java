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

import java.util.Arrays;
import java.util.Objects;

/**
 * 一些基础的扩展
 *
 * @author wjybxx
 * date - 2023/4/17
 */
@SuppressWarnings("unused")
public class ObjectUtils {

    /**
     * 如果给定参数为null，则返回给定的默认值，否则返回值本身
     * {@link Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V nullToDef(V obj, V def) {
        return obj == null ? def : obj;
    }

    // region equals/hash/toString

    public static int hashCode(Object first) {
        return Objects.hashCode(first);
    }

    public static int hashCode(Object first, Object second) {
        int result = Objects.hashCode(first);
        result = 31 * result + Objects.hashCode(second);
        return result;
    }

    public static int hashCode(Object first, Object second, Object third) {
        int result = Objects.hashCode(first);
        result = 31 * result + Objects.hashCode(second);
        result = 31 * result + Objects.hashCode(third);
        return result;
    }

    public static int hashCode(Object... args) {
        return Arrays.hashCode(args);
    }

    public static String toString(Object object, String nullDef) {
        return object == null ? nullDef : object.toString();
    }

    public static String toStringIfNotNull(Object object) {
        return object == null ? null : object.toString();
    }

    // endregion

    // region string
    // 没打算造一个StringUtils

    public static int length(CharSequence cs) {
        return cs == null ? 0 : cs.length();
    }

    public static boolean isEmpty(CharSequence cs) {
        return cs == null || cs.isEmpty();
    }

    public static boolean isBlank(CharSequence cs) {
        final int strLen = length(cs);
        if (strLen == 0) {
            return true;
        }
        for (int i = 0; i < strLen; i++) {
            if (!Character.isWhitespace(cs.charAt(i))) {
                return false;
            }
        }
        return true;
    }

    public static boolean isBlank(String string) {
        return string == null || string.isBlank();
    }

    /** 空字符串转默认字符串 */
    public static <T extends CharSequence> T emptyToDef(T str, T def) {
        return isEmpty(str) ? def : str;
    }

    /** 空白字符串转默认字符串 */
    public static <T extends CharSequence> T blankToDef(T str, T def) {
        return isBlank(str) ? def : str;
    }

    /** 空字符串转默认字符串 -- 避免string泛型转换 */
    public static String emptyToDef(String str, String def) {
        return isEmpty(str) ? def : str;
    }

    /** 空白字符串转默认字符串 -- 避免string泛型转换 */
    public static String blankToDef(String str, String def) {
        return isBlank(str) ? def : str;
    }

    /** 获取字符串的尾字符 */
    public static char lastChar(CharSequence value) {
        return value.charAt(value.length() - 1);
    }

    /** 首字母大写 */
    public static String firstCharToUpperCase(String str) {
        int length = length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isLowerCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toUpperCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    /** 首字母小写 */
    public static String firstCharToLowerCase(String str) {
        int length = length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isUpperCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toLowerCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    /** 是否包含不可见字符 */
    public static boolean containsWhitespace(final CharSequence cs) {
        final int strLen = length(cs);
        if (strLen == 0) {
            return false;
        }
        for (int i = 0; i < strLen; i++) {
            if (Character.isWhitespace(cs.charAt(i))) {
                return true;
            }
        }
        return false;
    }

    // endregion

    // region exception

    /** 是否是受检异常 */
    public static boolean isChecked(final Throwable throwable) {
        return !(throwable instanceof Error || throwable instanceof RuntimeException);
    }

    /** 是否是非受检异常 -- 通常指运行时异常 */
    public static boolean isUnchecked(final Throwable throwable) {
        return (throwable instanceof Error || throwable instanceof RuntimeException);
    }

    /**
     * 抛出原始异常，消除编译时警告
     *
     * @param <R> 方法正常执行的返回值类型
     */
    public static <R> R rethrow(final Throwable throwable) {
        return throwAsUncheckedException(throwable);
    }

    /**
     * 如果异常是非受检异常，则直接抛出，否则返回异常对象。
     */
    public static <T extends Throwable> T throwUnchecked(final T throwable) {
        if (isUnchecked(throwable)) {
            return throwAsUncheckedException(throwable);
        }
        return throwable; // 返回异常
    }

    /**
     * @param <R> 方法正常执行的返回值类型
     * @param <T> 异常类型约束
     */
    @SuppressWarnings("unchecked")
    private static <R, T extends Throwable> R throwAsUncheckedException(final Throwable throwable) throws T {
        throw (T) throwable;
    }

    // endregion

    private static final Double DOUBLE_DEFAULT = 0d;
    private static final Float FLOAT_DEFAULT = 0f;

    /** 获取一个类型的默认值 */
    @SuppressWarnings("unchecked")
    public static <T> T defaultValue(Class<T> type) {
        Objects.requireNonNull(type);
        if (type.isPrimitive()) {
            if (type == int.class) {
                return (T) Integer.valueOf(0);
            }
            if (type == long.class) {
                return (T) Long.valueOf(0L);
            }
            if (type == float.class) {
                return (T) FLOAT_DEFAULT;
            }
            if (type == double.class) {
                return (T) DOUBLE_DEFAULT;
            }
            if (type == boolean.class) {
                return (T) Boolean.FALSE;
            }
            if (type == byte.class) {
                return (T) Byte.valueOf((byte) 0);
            }
            if (type == short.class) {
                return (T) Short.valueOf((short) 0);
            }
            if (type == char.class) {
                return (T) Character.valueOf('\0');
            }
            throw new IllegalArgumentException("void");
        }
        return null;
    }
}