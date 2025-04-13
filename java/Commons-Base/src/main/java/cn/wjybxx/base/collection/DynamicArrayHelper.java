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

    static void insertBit(long[] elementsMask, int len, int bitIndex) {
        int wordIndex = wordIndex(bitIndex);
        for (int idx = wordCount(len) - 1; idx > wordIndex; idx--) {
            elementsMask[idx] = elementsMask[idx] << 1;
        }
        // word所在位置单独调整
        long word = elementsMask[wordIndex];
        int index = bitIndex & 63;
        long high = (word << 1) & (-1L << (index + 1)); // [0, index] 全0，使index位为0
        long lower = (word) & ((1L << index) - 1); // [0, index -1] 全1
        elementsMask[wordIndex] = high | lower;
    }

    static void setBit(long[] elementsMask, int fromIndex, int toIndex) {
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

    static void clearBit(long[] elementsMask, int fromIndex, int toIndex) {
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
}