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
package cn.wjybxx.dson.internal;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.io.DsonIOException;

/**
 * 这里的算法修改自commons-lang3，commons-lang3现在文件太多，不想引入。
 *
 * @author wjybxx
 * date - 2023/12/16
 */
public class CommonsLang3 {

    // region number
    public static boolean isParsable(String str) {
        if (ObjectUtils.isEmpty(str)) {
            return false;
        }
        if (str.charAt(str.length() - 1) == '.') {
            return false;
        }
        if (str.charAt(0) == '-') {
            if (str.length() == 1) {
                return false;
            }
            return withDecimalsParsing(str, 1);
        }
        return withDecimalsParsing(str, 0);
    }

    private static boolean withDecimalsParsing(final String str, final int beginIdx) {
        int decimalPoints = 0;
        for (int i = beginIdx; i < str.length(); i++) {
            final boolean isDecimalPoint = str.charAt(i) == '.';
            if (isDecimalPoint) {
                decimalPoints++;
            }
            if (decimalPoints > 1) {
                return false;
            }
            if (!isDecimalPoint && !Character.isDigit(str.charAt(i))) {
                return false;
            }
        }
        return true;
    }
    // endregion

    // region hex
    private static final char[] DIGITS_UPPER = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F'};

    public static void encodeHex(final byte[] data, final int dataOffset, final int dataLen,
                                 final char[] outBuffer, final int outOffset) {
        char[] toDigits = DIGITS_UPPER;
        for (int i = dataOffset, j = outOffset; i < dataOffset + dataLen; i++) {
            outBuffer[j++] = toDigits[(0xF0 & data[i]) >>> 4]; // 高4位
            outBuffer[j++] = toDigits[0x0F & data[i]]; // 低4位
        }
    }

    public static byte[] decodeHex(char[] data) {
        final int dateLen = data.length;
        if ((dateLen & 0x01) != 0) {
            throw new DsonIOException("string length not even: " + data.length);
        }
        byte[] result = new byte[dateLen >> 1];
        // two characters form the hex value.
        for (int i = 0, j = 0; j < dateLen; i++) {
            int f = toDigit(data[j], j) << 4;
            j++;
            f = f | toDigit(data[j], j);
            j++;
            result[i] = (byte) (f & 0xFF);
        }
        return result;
    }

    private static int toDigit(final char c, final int index) {
        return switch (c) {
            case '0' -> 0;
            case '1' -> 1;
            case '2' -> 2;
            case '3' -> 3;
            case '4' -> 4;
            case '5' -> 5;
            case '6' -> 6;
            case '7' -> 7;
            case '8' -> 8;
            case '9' -> 9;
            case 'a', 'A' -> 10;
            case 'b', 'B' -> 11;
            case 'c', 'C' -> 12;
            case 'd', 'D' -> 13;
            case 'e', 'E' -> 14;
            case 'f', 'F' -> 15;
            default -> throw new DsonIOException("Illegal hexadecimal character " + c + " at index " + index);
        };
    }
    // endregion
}