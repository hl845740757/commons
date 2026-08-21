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

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import java.util.List;

/**
 * DynamicArray的边界测试
 * (主要覆盖len为64整数倍时的word边界，以及nullFactor大于1不主动压缩的场景)
 * <p>
 * 注意：探测mask损坏必须构造elementCount小于len的状态，
 * 否则containsNull/indexOf(null)会因elementCount==len而短路返回，掩盖问题。
 *
 * @author wjybxx
 * date - 2025/4/13
 */
public class DynamicArrayBoundaryTest {

    // region null-index

    /** len为64整数倍时，lastIndexOf(null)应返回真实的null下标 */
    @ParameterizedTest
    @ValueSource(ints = {64, 128, 192})
    void testLastNullIndexAtWordBoundary(int size) {
        DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f); // nullFactor>1，不主动压缩
        for (int i = 0; i < size; i++) arr.add("v" + i);
        Assertions.assertEquals(size, arr.length());

        arr.set(10, null);
        Assertions.assertEquals(size - 1, arr.elementCount());
        Assertions.assertEquals(10, arr.indexOf(null));
        Assertions.assertEquals(10, arr.lastIndexOf(null));
    }

    /** len为64整数倍时，删除元素触发的压缩不应越界 */
    @ParameterizedTest
    @ValueSource(ints = {64, 128})
    void testCompressAtWordBoundary(int size) {
        DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.add("v" + i);
        arr.set(10, null);

        arr.compress(true);
        Assertions.assertEquals(size - 1, arr.length());
        Assertions.assertEquals(size - 1, arr.elementCount());
        Assertions.assertFalse(arr.containsNull());
        Assertions.assertEquals("v11", arr.get(10));
        Assertions.assertEquals("v" + (size - 1), arr.get(size - 2));
    }

    /** 默认nullFactor=0时，len恰为64整数倍时remove会立即压缩 */
    @ParameterizedTest
    @ValueSource(ints = {64, 128})
    void testRemoveAtWordBoundaryAutoCompress(int size) {
        DynamicArray<String> arr = new DefaultDynamicArray<>(8); // nullFactor=0，总是压缩
        for (int i = 0; i < size; i++) arr.add("v" + i);
        Assertions.assertEquals(size, arr.length());

        arr.remove("v10");
        Assertions.assertEquals(size - 1, arr.length());
        Assertions.assertEquals("v11", arr.get(10));
    }

    // endregion

    // region insert-bit

    /**
     * len为64整数倍时insert，需将最高位进位到下一个word。
     * 若进位丢失，mask会误报一个不存在的null（幽灵null）。
     */
    @ParameterizedTest
    @ValueSource(ints = {64, 128, 192})
    void testInsertCarryAcrossWordBoundary(int size) {
        DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.add("v" + i);

        arr.insert(0, "X"); // len: size -> size+1，原index(size-1)移到index(size)
        Assertions.assertEquals(size + 1, arr.length());
        Assertions.assertEquals("X", arr.get(0));
        Assertions.assertEquals("v" + (size - 1), arr.get(size));

        // 构造唯一的真实null，使elementCount<len，避免短路掩盖mask损坏
        arr.set(10, null);
        Assertions.assertEquals(size, arr.elementCount());
        Assertions.assertEquals(10, arr.indexOf(null));
        Assertions.assertEquals(10, arr.lastIndexOf(null));
    }

    /** insert后压缩：mask损坏会导致真实元素被当作空洞覆盖，造成数据丢失 */
    @ParameterizedTest
    @ValueSource(ints = {64, 128})
    void testCompressAfterInsertAtWordBoundary(int size) {
        DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f);
        for (int i = 0; i < size; i++) arr.add("v" + i);
        arr.insert(0, "X");
        arr.set(10, null); // 唯一null

        arr.compress(true);
        Assertions.assertEquals(size, arr.length());
        Assertions.assertEquals(size, arr.elementCount());

        List<String> list = arr.toList();
        Assertions.assertEquals(size, list.size());
        Assertions.assertEquals("X", list.get(0));
        Assertions.assertEquals("v" + (size - 1), list.get(size - 1), "尾元素不应丢失");
        Assertions.assertFalse(list.contains(null));
    }

    /** 逐个size扫描insert后的mask完整性 */
    @Test
    void testInsertMaskIntegrityAcrossSizes() {
        for (int size = 4; size <= 200; size++) {
            DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f);
            for (int i = 0; i < size; i++) arr.add("v" + i);
            arr.insert(0, "X");

            // 制造唯一真实null
            arr.set(3, null);
            Assertions.assertEquals(3, arr.indexOf(null), "size=" + size);
            Assertions.assertEquals(3, arr.lastIndexOf(null), "size=" + size + " 进位丢失");
            Assertions.assertEquals("v" + (size - 1), arr.get(size), "size=" + size + " 尾元素");
        }
    }

    /** insert到中间位置，元素顺序与mask均需正确 */
    @Test
    void testInsertMiddleAcrossBoundary() {
        for (int size = 60; size <= 132; size++) {
            DynamicArray<String> arr = new DefaultDynamicArray<>(size + 16, 2.0f);
            for (int i = 0; i < size; i++) arr.add("v" + i);

            arr.insert(30, "X");
            Assertions.assertEquals(size + 1, arr.elementCount(), "size=" + size);
            Assertions.assertFalse(arr.containsNull(), "size=" + size + " 不应有幽灵null");
            Assertions.assertEquals("X", arr.get(30), "size=" + size);
            Assertions.assertEquals("v30", arr.get(31), "size=" + size);
            Assertions.assertEquals("v" + (size - 1), arr.get(size), "size=" + size);
        }
    }

    // endregion

    // region clear

    /** 所有元素均为null时（未压缩），clear仍应重置length */
    @Test
    void testClearWhenAllNullResetsLength() {
        DynamicArray<String> arr = new DefaultDynamicArray<>(8, 2.0f);
        for (int i = 0; i < 5; i++) arr.add("v" + i);
        for (int i = 0; i < 5; i++) arr.set(i, null);
        Assertions.assertEquals(0, arr.elementCount());
        Assertions.assertEquals(5, arr.length());

        arr.clear();
        Assertions.assertEquals(0, arr.length());
        Assertions.assertEquals(0, arr.elementCount());
    }

    /** IndexedDynamicArray：全null时clear应重置length，且新元素落在下标0 */
    @Test
    void testIndexedClearWhenAllNull() {
        IndexedDynamicArray<Idx> arr = new IndexedDynamicArray<>(H.INST, 8, 2.0f);
        for (int i = 0; i < 5; i++) arr.add(new Idx());
        for (int i = 0; i < 5; i++) arr.set(i, null);

        arr.clear();
        Assertions.assertEquals(0, arr.length());

        Idx fresh = new Idx();
        arr.add(fresh);
        Assertions.assertEquals(0, fresh.qIndex, "clear后首个元素应落在下标0");
        Assertions.assertFalse(arr.containsNull(), "clear后不应残留null空洞");
    }

    /** SmallDynamicArray：全null时clear应重置length */
    @Test
    void testSmallClearWhenAllNull() {
        DynamicArray<String> arr = new SmallDynamicArray<>(8, 2.0f);
        for (int i = 0; i < 5; i++) arr.add("v" + i);
        for (int i = 0; i < 5; i++) arr.set(i, null);

        arr.clear();
        Assertions.assertEquals(0, arr.length());
    }

    /** 反复clear复用不应导致length单调增长 */
    @Test
    void testRepeatedClearNoLeak() {
        DynamicArray<String> arr = new DefaultDynamicArray<>(8, 2.0f);
        IndexedDynamicArray<Idx> iarr = new IndexedDynamicArray<>(H.INST, 8, 2.0f);
        for (int round = 0; round < 8; round++) {
            for (int i = 0; i < 5; i++) {
                arr.add("v" + i);
                iarr.add(new Idx());
            }
            for (int i = 0; i < arr.length(); i++) arr.set(i, null);
            for (int i = 0; i < iarr.length(); i++) iarr.set(i, null);
            arr.clear();
            iarr.clear();
            Assertions.assertEquals(0, arr.length(), "round=" + round);
            Assertions.assertEquals(0, iarr.length(), "round=" + round);
        }
    }

    /** clear后复用：mask需彻底清零 */
    @Test
    void testClearThenReuseAtWordBoundary() {
        DynamicArray<String> arr = new DefaultDynamicArray<>(64, 2.0f);
        for (int i = 0; i < 64; i++) arr.add("v" + i);
        arr.clear();
        Assertions.assertEquals(0, arr.length());

        for (int i = 0; i < 64; i++) arr.add("x" + i);
        Assertions.assertEquals(64, arr.elementCount());
        Assertions.assertFalse(arr.containsNull());
        Assertions.assertEquals(-1, arr.indexOf(null));

        arr.set(10, null);
        Assertions.assertEquals(10, arr.lastIndexOf(null));
    }

    // endregion

    // region ctor

    /**
     * SmallDynamicArray的容量上限为64，构造时即应校验
     * (否则elementsMask只有64位，越界位会回绕污染低位)
     */
    @Test
    void testSmallCtorCapacityValidation() {
        Assertions.assertThrows(IllegalArgumentException.class, () -> new SmallDynamicArray<>(65));
        Assertions.assertThrows(IllegalArgumentException.class, () -> new SmallDynamicArray<>(100));
        Assertions.assertDoesNotThrow(() -> new SmallDynamicArray<>(0));
        Assertions.assertDoesNotThrow(() -> new SmallDynamicArray<>(64));
    }

    /** initCapacity为0时，首次add不应越界 */
    @Test
    void testZeroInitCapacity() {
        DynamicArray<String> arr = new DefaultDynamicArray<>(0);
        arr.add("a");
        Assertions.assertEquals(1, arr.length());
        Assertions.assertEquals("a", arr.get(0));

        IndexedDynamicArray<Idx> iarr = new IndexedDynamicArray<>(H.INST, 0);
        Idx e = new Idx();
        iarr.add(e);
        Assertions.assertEquals(1, iarr.length());
        Assertions.assertEquals(0, e.qIndex);

        DynamicArray<String> sarr = new SmallDynamicArray<>(0);
        sarr.add("a");
        Assertions.assertEquals(1, sarr.length());
    }

    // endregion

    // region internal

    private static class H implements IndexedElementHelper<Idx> {

        static final H INST = new H();

        @Override
        public int collectionIndex(Object collection, Idx element) {
            return element.qIndex;
        }

        @Override
        public void collectionIndex(Object collection, Idx element, int index) {
            element.qIndex = index;
        }
    }

    private static class Idx {

        int qIndex = -1;
    }
    // endregion
}
