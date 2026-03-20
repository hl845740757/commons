/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.base.collection;

/**
 * @author wjybxx
 * date - 2025/4/13
 */
final class DynamicArrayHelper {

    private static final long WORD_MASK = -1;
    private static final int ADDRESS_BITS_PER_WORD = 6;

    public static int wordIndex(int index) {
        return index >> ADDRESS_BITS_PER_WORD;
    }

    public static int wordCount(int bitCount) {
        return (bitCount >> ADDRESS_BITS_PER_WORD) + 1;
    }

    public static boolean isCompressionNeeded(float nullFactor, int len, int elementCount) {
        if (nullFactor == 0f) return true;
        if (nullFactor > 1f) return false;

        int nullCount = len - elementCount;
        if (nullFactor == 1f) {
            return nullCount == len;
        }
        return nullCount > 0 && nullCount >= len * nullFactor;
    }

    // region index-null

    public static int firstNullIndex(long[] elementsMask, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = wordCount(len);
        for (int idx = 0; idx < wordCount; idx++) {
            long word = elementsMask[idx];
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + Long.numberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    public static int lastNullIndex(long[] elementsMask, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = wordCount(len);
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = elementsMask[idx];
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - Long.numberOfLeadingZeros(~word));
        }
        throw new AssertionError();
    }

    public static int firstNullIndex(long lowBits, long highBits, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = len > 64 ? 2 : 1;
        for (int idx = 0; idx < wordCount; idx++) {
            long word = idx == 1 ? highBits : lowBits;
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + Long.numberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    public static int lastNullIndex(long lowBits, long highBits, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = len > 64 ? 2 : 1;
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = idx == 1 ? highBits : lowBits;
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - Long.numberOfLeadingZeros(~word));
        }
        throw new AssertionError();
    }

    // endregion

    // region bit-update

    public static void insertBit(long[] elementsMask, int len, int bitIndex) {
        int wordIndex = wordIndex(bitIndex);
        for (int idx = wordCount(len) - 1; idx > wordIndex; idx--) {
            long sign = elementsMask[idx - 1] < 0 ? 1 : 0; // 下个word的最高位
            elementsMask[idx] = (elementsMask[idx] << 1) | sign;
        }
        // word所在位置单独调整
        long word = elementsMask[wordIndex];
        long lowMask = (1L << (bitIndex & 63)) - 1;
        long low = (word & lowMask);
        long high = (word & ~lowMask) << 1;
        elementsMask[wordIndex] = high | low;
    }

    public static void setBit(long[] elementsMask, int fromIndex, int toIndex) {
        if (fromIndex == toIndex) return;

        // Increase capacity if necessary
        int startWordIndex = wordIndex(fromIndex);
        int endWordIndex = wordIndex(toIndex - 1);

        long firstWordMask = WORD_MASK << fromIndex;
        long lastWordMask = WORD_MASK >>> -toIndex; // ((endWordIndex + 1) * 64  - toIndex)
        if (startWordIndex == endWordIndex) {
            // Case 1: One word
            elementsMask[startWordIndex] |= (firstWordMask & lastWordMask);
        } else {
            // Case 2: Multiple words
            // Handle first word
            elementsMask[startWordIndex] |= firstWordMask;

            // Handle intermediate words, if any
            for (int i = startWordIndex + 1; i < endWordIndex; i++)
                elementsMask[i] = WORD_MASK;

            // Handle last word (restores invariants)
            elementsMask[endWordIndex] |= lastWordMask;
        }
    }

    public static void clearBit(long[] elementsMask, int fromIndex, int toIndex) {
        if (fromIndex == toIndex) return;

        int startWordIndex = wordIndex(fromIndex);
        int endWordIndex = wordIndex(toIndex - 1);

        long firstWordMask = WORD_MASK << fromIndex;
        long lastWordMask = WORD_MASK >>> -toIndex; // ((endWordIndex + 1) * 64  - toIndex)
        if (startWordIndex == endWordIndex) {
            // Case 1: One word
            elementsMask[startWordIndex] &= ~(firstWordMask & lastWordMask);
        } else {
            // Case 2: Multiple words
            // Handle first word
            elementsMask[startWordIndex] &= ~firstWordMask;

            // Handle intermediate words, if any
            for (int i = startWordIndex + 1; i < endWordIndex; i++)
                elementsMask[i] = 0;

            // Handle last word
            elementsMask[endWordIndex] &= ~lastWordMask;
        }
    }
    // endregion
}