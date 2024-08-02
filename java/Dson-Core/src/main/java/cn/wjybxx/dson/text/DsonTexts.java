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

package cn.wjybxx.dson.text;

import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.internal.CommonsLang3;

import java.util.BitSet;
import java.util.Objects;
import java.util.Set;

/**
 * Dson的文本表示法
 * 类似json但不是json
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonTexts {

    // 类型标签
    public static final String LABEL_INT32 = "i";
    public static final String LABEL_INT64 = "L";
    public static final String LABEL_UINT32 = "ui";
    public static final String LABEL_UINT64 = "uL";
    public static final String LABEL_FLOAT = "f";
    public static final String LABEL_DOUBLE = "d";
    public static final String LABEL_BOOL = "b";
    public static final String LABEL_STRING = "s";
    public static final String LABEL_NULL = "N";

    /** 单行纯文本，字符串不需要加引号，不对内容进行转义 */
    public static final String LABEL_STRING_LINE = "sL";

    public static final String LABEL_BINARY = "bin";
    public static final String LABEL_PTR = "ptr";
    public static final String LABEL_LITE_PTR = "lptr";
    public static final String LABEL_DATETIME = "dt";
    public static final String LABEL_TIMESTAMP = "ts";

    public static final String LABEL_BEGIN_OBJECT = "{";
    public static final String LABEL_END_OBJECT = "}";
    public static final String LABEL_BEGIN_ARRAY = "[";
    public static final String LABEL_END_ARRAY = "]";
    public static final String LABEL_BEGIN_HEADER = "@{";

    // 行首标签
    public static final char HEAD_COMMENT = '#';
    public static final char HEAD_APPEND = '-';
    public static final char HEAD_APPEND_LINE = '|';
    public static final char HEAD_SWITCH_MODE = '^';

    /** 内建结构体标签 */
    private static final Set<String> builtinStructLabels = Set.of(
            LABEL_PTR, LABEL_LITE_PTR, LABEL_DATETIME, LABEL_TIMESTAMP
    );

    /** 有特殊含义的字符串 */
    private static final Set<String> parseableStrings = Set.of("true", "false",
            "null", "undefine",
            "NaN", "Infinity", "-Infinity");

    /**
     * 规定哪些不安全较为容易，规定哪些安全反而不容易
     * 这些字符都是128内，使用bitset很快，还可以避免第三方依赖
     */
    private static final BitSet unsafeCharSet = new BitSet(128);

    static {
        char[] tokenCharArray = "{}[],:/@\"\\".toCharArray();
        for (char c : tokenCharArray) {
            unsafeCharSet.set(c);
        }
    }

    /** 是否是缩进字符 */
    public static boolean isIndentChar(int c) {
        return c == ' ' || c == '\t';
    }

    /** 是否是不安全的字符，不能省略引号的字符 */
    public static boolean isUnsafeStringChar(int c) {
        return unsafeCharSet.get(c) || Character.isWhitespace(c);
    }

    /**
     * 是否是安全字符，可以省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，
     * 因此不能因为每个字符安全，就认为整个字符串安全
     */
    public static boolean isSafeStringChar(int c) {
        return !unsafeCharSet.get(c) && !Character.isWhitespace(c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，否则既难以保证正确性，性能也差
     */
    public static boolean canUnquoteString(String value, int maxLengthOfUnquoteString) {
        if (value.isEmpty() || value.length() > maxLengthOfUnquoteString) { // 长字符串都加引号，避免不必要的计算
            return false;
        }
        if (parseableStrings.contains(value)) { // 特殊字符串值
            return false;
        }
        for (int i = 0; i < value.length(); i++) { // 这遍历的不是unicode码点，但不影响
            char c = value.charAt(i);
            if (isUnsafeStringChar(c)) {
                return false;
            }
        }
        if (isParsable(value)) { // 可解析的数字类型，这个开销大放最后检测
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static boolean isASCIIText(String text) {
        for (int i = 0, len = text.length(); i < len; i++) {
            if (text.charAt(i) < 32 || text.charAt(i) > 126) {
                return false;
            }
        }
        return true;
    }

    // region bool/null

    public static boolean parseBool(String str) {
        if (str.equals("true") || str.equals("1")) return true;
        if (str.equals("false") || str.equals("0")) return false;
        throw new IllegalArgumentException("invalid bool str: " + str);
    }

    public static void checkNullString(String str) {
        if ("null".equals(str)) {
            return;
        }
        throw new IllegalArgumentException("invalid null str: " + str);
    }
    // endregion

    //region 数字

    /** 是否是可解析的数字类型 */
    public static boolean isParsable(String str) {
        int length = str.length();
        if (length == 0 || length > 67 + 16) { // 最长也不应该比二进制格式长，16是下划线预留
            return false;
        }
        return CommonsLang3.isParsable(str);
    }

    public static int parseInt32(String rawStr) {
        String str = deleteUnderline(rawStr);
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str.charAt(0);
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset + 2 < str.length() && str.charAt(lookOffset) == '0') {
            char baseChar = str.charAt(lookOffset + 1);
            if (baseChar == 'x' || baseChar == 'X') {
                return sign * Integer.parseUnsignedInt(str, lookOffset + 2, str.length(), 16);
            }
            if (baseChar == 'b' || baseChar == 'B') {
                return sign * Integer.parseUnsignedInt(str, lookOffset + 2, str.length(), 2);
            }
        }
        return sign * Integer.parseUnsignedInt(str, lookOffset, str.length(), 10);
    }

    public static long parseInt64(final String rawStr) {
        String str = deleteUnderline(rawStr);
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str.charAt(0);
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset + 2 < str.length() && str.charAt(lookOffset) == '0') {
            char baseChar = str.charAt(lookOffset + 1);
            if (baseChar == 'x' || baseChar == 'X') {
                return sign * Long.parseUnsignedLong(str, lookOffset + 2, str.length(), 16);
            }
            if (baseChar == 'b' || baseChar == 'B') {
                return sign * Long.parseUnsignedLong(str, lookOffset + 2, str.length(), 2);
            }
        }
        return sign * Long.parseUnsignedLong(str, lookOffset, str.length(), 10);
    }

    public static float parseFloat(String rawStr) {
        String str = deleteUnderline(rawStr);
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        return Float.parseFloat(str);
    }

    public static double parseDouble(String rawStr) {
        String str = deleteUnderline(rawStr);
        if (str.isEmpty()) {
            throw new NumberFormatException(rawStr);
        }
        return Double.parseDouble(str);
    }

    public static String deleteUnderline(String str) {
        if (str.indexOf('_') < 0) { // 避免额外字符串
            return str;
        }
        int length = str.length();
        if (str.charAt(0) == '_' || str.charAt(length - 1) == '_') { // 首尾字符不能是下划线
            throw new NumberFormatException(str);
        }
        StringBuilder sb = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.acquire();
        try {
            boolean hasUnderline = false;
            for (int i = 0; i < length; i++) {
                char c = str.charAt(i);
                if (c == '_') {
                    if (hasUnderline) throw new NumberFormatException(str); // 不能多个连续下划线
                    hasUnderline = true;
                } else {
                    sb.append(c);
                    hasUnderline = false;
                }
            }
            return sb.toString();
        } finally {
            ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.release(sb);
        }
    }

    // endregion

    /** 获取类型名对应的Token类型 */
    public static DsonTokenType tokenTypeOfClsName(String label) {
        Objects.requireNonNull(label);
        return switch (label) {
            case LABEL_INT32 -> DsonTokenType.INT32;
            case LABEL_INT64 -> DsonTokenType.INT64;
            case LABEL_FLOAT -> DsonTokenType.FLOAT;
            case LABEL_DOUBLE -> DsonTokenType.DOUBLE;
            case LABEL_BOOL -> DsonTokenType.BOOL;
            case LABEL_STRING, LABEL_STRING_LINE -> DsonTokenType.STRING;
            case LABEL_NULL -> DsonTokenType.NULL;
            case LABEL_BINARY -> DsonTokenType.BINARY;
            default -> {
                if (builtinStructLabels.contains(label)) {
                    yield DsonTokenType.BUILTIN_STRUCT;
                }
                yield DsonTokenType.SIMPLE_HEADER;
            }
        };
    }

    /** 获取dsonType关联的无位置Token */
    public static DsonToken clsNameTokenOfType(DsonType dsonType) {
        return switch (dsonType) {
            case INT32 -> new DsonToken(DsonTokenType.INT32, LABEL_INT32, -1);
            case INT64 -> new DsonToken(DsonTokenType.INT64, LABEL_INT64, -1);
            case FLOAT -> new DsonToken(DsonTokenType.FLOAT, LABEL_FLOAT, -1);
            case DOUBLE -> new DsonToken(DsonTokenType.DOUBLE, LABEL_DOUBLE, -1);
            case BOOL -> new DsonToken(DsonTokenType.BOOL, LABEL_BOOL, -1);
            case STRING -> new DsonToken(DsonTokenType.STRING, LABEL_STRING, -1);
            case NULL -> new DsonToken(DsonTokenType.NULL, LABEL_NULL, -1);
            case BINARY -> new DsonToken(DsonTokenType.BINARY, LABEL_BINARY, -1);
            case POINTER -> new DsonToken(DsonTokenType.BUILTIN_STRUCT, LABEL_PTR, -1);
            case LITE_POINTER -> new DsonToken(DsonTokenType.BUILTIN_STRUCT, LABEL_LITE_PTR, -1);
            case DATETIME -> new DsonToken(DsonTokenType.BUILTIN_STRUCT, LABEL_DATETIME, -1);
            case TIMESTAMP -> new DsonToken(DsonTokenType.BUILTIN_STRUCT, LABEL_TIMESTAMP, -1);
            default -> throw new IllegalArgumentException();
        };
    }

}