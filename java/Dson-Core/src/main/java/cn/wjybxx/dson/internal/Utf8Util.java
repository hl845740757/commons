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

/**
 * 不建议直接使用，若确实想使用该算法，可考虑拷贝代码
 *
 * @author wjybxx
 * date - 2023/12/16
 */
public class Utf8Util {

    public static int utf8Length(final String str) {
        int utf16Length = str.length();

        int i = 0;
        for (; i < utf16Length && str.charAt(i) < 0x80; i++) {

        }
        if (i == utf16Length) {
            return i;
        }

        int total = i;
        while (i < utf16Length) {
            int c = Character.codePointAt(str, i);
            if (c < 0x80) {
                total += 1;
            } else if (c < 0x800) {
                total += 2;
            } else if (c < 0x10000) {
                total += 3;
            } else {
                total += 4;
            }
            i += Character.charCount(c);
        }
        return total;
    }

    /**
     * utf8编码
     *
     * @param str       要编码的字符串
     * @param outBuffer buffer
     * @param offset    buffer的偏移
     * @param length    buffer的可用长度
     * @return 编码的字节数
     */
    public static int utf8Encode(final String str, byte[] outBuffer, int offset, int length) {
        final int utf16Length = str.length();
        final int limit = offset + length;

        // 单字节字符串优化 -- ASCII码字符优化
        int i = 0;
        int j = offset;
        for (char c; i < utf16Length && (j + i < limit) && (c = str.charAt(i)) < 0x80; i++) {
            outBuffer[j++] = (byte) c;
        }
        if (i == utf16Length) {
            return i;
        }

        int total = i;
        while (i < utf16Length) {
            int c = Character.codePointAt(str, i);
            if (c < 0x80 && (j < limit)) {
                outBuffer[j++] = (byte) c;
                total += 1;
            } else if (c < 0x800 && (j + 2 <= limit)) {
                outBuffer[j++] = ((byte) (0xc0 + (c >> 6)));
                outBuffer[j++] = ((byte) (0x80 + (c & 0x3f)));
                total += 2;
            } else if (c < 0x10000 && (j + 3 <= limit)) {
                // 最大单字符码位为0xFFFF, 16位，3个UTF-8字节
                outBuffer[j++] = ((byte) (0xe0 + (c >> 12)));
                outBuffer[j++] = ((byte) (0x80 + ((c >> 6) & 0x3f)));
                outBuffer[j++] = ((byte) (0x80 + (c & 0x3f)));
                total += 3;
            } else {
                // 可能是由于空间不够进入到这里
                if (c < 0x10000 || (j + 4 > limit)) {
                    throw new ArrayIndexOutOfBoundsException("Failed writing " + c + " at index " + j);
                }
                outBuffer[j++] = ((byte) (0xf0 + (c >> 18)));
                outBuffer[j++] = ((byte) (0x80 + ((c >> 12) & 0x3f)));
                outBuffer[j++] = ((byte) (0x80 + ((c >> 6) & 0x3f)));
                outBuffer[j++] = ((byte) (0x80 + (c & 0x3f)));
                total += 4;
            }
            i += Character.charCount(c);
        }
        return total;
    }
}