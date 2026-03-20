#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Collections
{
internal static class DynamicArrayHelper
{
    public const int MAX_CAPACITY = int.MaxValue - 8;
    private const long WORD_MASK = -1;
    private const int ADDRESS_BITS_PER_WORD = 6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WordIndex(int index) {
        return index >> ADDRESS_BITS_PER_WORD;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WordCount(int bitCount) {
        return (bitCount >> ADDRESS_BITS_PER_WORD) + 1;
    }

    public static bool IsCompressionNeeded(float nullFactor, int len, int elementCount) {
        if (nullFactor == 0f) return true;
        if (nullFactor > 1f) return false;

        int nullCount = len - elementCount;
        if (nullFactor == 1f) {
            return nullCount == len;
        }
        return nullCount > 0 && nullCount >= len * nullFactor;
    }

    #region index-null

    public static int FirstNullIndex(long[] elementsMask, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = WordCount(len);
        for (int idx = 0; idx < wordCount; idx++) {
            long word = elementsMask[idx];
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + MathCommon.NumberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    public static int LastNullIndex(long[] elementsMask, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = WordCount(len);
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = elementsMask[idx];
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位还是-1
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - MathCommon.NumberOfLeadingZeros(~word));
        }
        throw new AssertionError();
    }

    public static int FirstNullIndex(long lowBits, long highBits, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = len > 64 ? 2 : 1;
        for (int idx = 0; idx < wordCount; idx++) {
            long word = idx == 1 ? highBits : lowBits;
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + MathCommon.NumberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    public static int LastNullIndex(long lowBits, long highBits, int len, int elementCount) {
        if (elementCount == len) return -1;
        int wordCount = len > 64 ? 2 : 1;
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = idx == 1 ? highBits : lowBits;
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位还是-1
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - MathCommon.NumberOfLeadingZeros(~word));
        }
        throw new AssertionError();
    }

    #endregion

    #region bit-update

    public static void InsertBit(long[] elementsMask, int len, int bitIndex) {
        int wordIndex = WordIndex(bitIndex);
        for (int idx = WordCount(len) - 1; idx > wordIndex; idx--) {
            long sign = elementsMask[idx - 1] < 0 ? 1 : 0; // 下个word的最高位
            elementsMask[idx] = (elementsMask[idx] << 1) | sign;
        }
        // word所在位置单独调整
        long word = elementsMask[wordIndex];
        long lowMask = 1L << (bitIndex & 63) - 1;
        long low = (word & lowMask);
        long high = (word & ~lowMask) << 1;
        elementsMask[wordIndex] = high | low;
    }

    public static void SetBit(long[] elementsMask, int fromIndex, int toIndex) {
        if (fromIndex == toIndex) return;

        // Increase capacity if necessary
        int startWordIndex = WordIndex(fromIndex);
        int endWordIndex = WordIndex(toIndex - 1);

        long firstWordMask = WORD_MASK << fromIndex;
        long lastWordMask = MathCommon.LogicalShiftRight(WORD_MASK, -toIndex); // ((endWordIndex + 1) * 64  - toIndex)
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

    public static void ClearBit(long[] elementsMask, int fromIndex, int toIndex) {
        if (fromIndex == toIndex) return;

        int startWordIndex = WordIndex(fromIndex);
        int endWordIndex = WordIndex(toIndex - 1);

        long firstWordMask = WORD_MASK << fromIndex;
        long lastWordMask = MathCommon.LogicalShiftRight(WORD_MASK, -toIndex); // ((endWordIndex + 1) * 64  - toIndex)
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

    #endregion
}
}