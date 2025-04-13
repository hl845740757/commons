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

package cn.wjybxx.base.collection;

import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.MathCommon;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.RepeatedTest;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2024/8/4
 */
public class DynamicArrayTest {

    private static int capacity = 64;
    private static int repeat = 0;
    private static DynamicArray<Indexed> dynamicArray;
    private static Indexed[] valArray;
    private static final Int2ObjectMap<Indexed> cacheMap = new Int2ObjectOpenHashMap<>(1000);

    private static Indexed valueOf(int val) {
        Indexed indexed = cacheMap.get(val);
        if (indexed == null) {
            indexed = new Indexed(val);
            cacheMap.put(val, indexed);
        }
        return indexed;
    }

    @BeforeEach
    void setUp() {
        cacheMap.clear();
        if (MathCommon.isOdd(repeat++)) {
            capacity = 64;
            dynamicArray = new SmallDynamicArray<>(capacity / 3); // 测试扩容
        } else {
            capacity = 1000;
            dynamicArray = new IndexedDynamicArray<>(Helper.INST, capacity / 6); // 测试扩容
        }
        for (int i = 0; i < capacity; i++) {
            dynamicArray.add(valueOf(i));
        }
        valArray = new Indexed[capacity];
        for (int i = 0; i < capacity; i++) {
            valArray[i] = valueOf(i);
        }
        ArrayUtils.shuffle(valArray);
    }

    @RepeatedTest(2)
    void testRemove() {
        for (int i = 0; i < valArray.length; i++) {
            Indexed val = valArray[i];
            dynamicArray.remove(val);

            Assertions.assertFalse(dynamicArray.contains(val), "remove failed");
            for (int j = i + 1; j < valArray.length; j++) {
                Indexed jVal = valArray[j];
                Assertions.assertTrue(dynamicArray.contains(jVal), "val is absent" + jVal);
            }
        }
        Assertions.assertEquals(0, dynamicArray.elementCount());
    }

    @RepeatedTest(2)
    void testRemoveWhenIterating() {
        dynamicArray.beginItr();
        try {
            for (int i = 0; i < valArray.length; i++) {
                Indexed val = valArray[i];
                dynamicArray.remove(val);

                Assertions.assertFalse(dynamicArray.contains(val), "remove failed");
                for (int j = i + 1; j < valArray.length; j++) {
                    Indexed jVal = valArray[j];
                    Assertions.assertTrue(dynamicArray.contains(jVal), "val is absent" + jVal);
                }
            }
            Assertions.assertEquals(capacity, dynamicArray.length());
        } finally {
            dynamicArray.endItr();
        }
        Assertions.assertEquals(0, dynamicArray.elementCount());
    }

    @RepeatedTest(2)
    void testInsert() {
        // 先删除一半，再insert回去
        List<Indexed> arrayList = dynamicArray.toList();
        List<Indexed> removedList = new ArrayList<>(arrayList);
        Collections.shuffle(removedList);
        removedList.subList(0, removedList.size() / 2).clear();
        //
        for (Indexed val : removedList) {
            if (!arrayList.remove(val)) {
                throw new AssertionError();
            }
            if (!dynamicArray.remove(val)) {
                throw new AssertionError();
            }
        }
        dynamicArray.compress(true);
        Assertions.assertEquals(arrayList.size(), dynamicArray.elementCount());
        Assertions.assertEquals(arrayList, dynamicArray.toList());
        // 插入
        for (Indexed val : removedList) {
            int index = MathCommon.SHARED_RANDOM.nextInt(arrayList.size());
            arrayList.add(index, val);
            dynamicArray.insert(index, val);
        }
        Assertions.assertEquals(arrayList.size(), dynamicArray.elementCount());
        Assertions.assertEquals(arrayList, dynamicArray.toList());
    }

    // region internal

    private static class Helper implements IndexedElementHelper<Indexed> {

        static final Helper INST = new Helper();

        @Override
        public int collectionIndex(Object collection, Indexed element) {
            return element.qIndex;
        }

        @Override
        public void collectionIndex(Object collection, Indexed element, int index) {
            element.qIndex = index;
        }
    }

    private static class Indexed {

        private final int val;
        int qIndex = -1;

        Indexed(int val) {
            this.val = val;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            Indexed indexed = (Indexed) o;
            return val == indexed.val;
        }

        @Override
        public int hashCode() {
            return val;
        }

        @Override
        public String toString() {
            return "Indexed{" +
                    "val=" + val +
                    ", qIndex=" + qIndex +
                    '}';
        }
    }
    // endregion

}