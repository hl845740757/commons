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
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2024/8/4
 */
public class DelayedCompressListTest {

    private static final int capacity = 8;
    private static DelayedCompressList<Integer> list;
    private static int[] valArray;

    @BeforeEach
    void setUp() {
        list = new DelayedCompressList<>(capacity / 3); // 测试扩容
        for (int i = 0; i < capacity; i++) {
            list.add(i);
        }
        valArray = new int[capacity];
        for (int i = 0; i < capacity; i++) {
            valArray[i] = i;
        }
        ArrayUtils.shuffle(valArray);
    }

    @Test
    void testRemove() {
        for (int i = 0; i < valArray.length; i++) {
            Integer val = valArray[i];
            list.remove(val);

            Assertions.assertFalse(list.contains(val), "remove failed");
            for (int j = i + 1; j < valArray.length; j++) {
                Integer jVal = valArray[j];
                Assertions.assertTrue(list.contains(jVal), "val is absent" + jVal);
            }
        }
        Assertions.assertEquals(0, list.realSize());
    }

    @Test
    void testRemoveWhenIterating() {
        list.beginItr();
        try {
            for (int i = 0; i < valArray.length; i++) {
                Integer val = valArray[i];
                list.remove(val);

                Assertions.assertFalse(list.contains(val), "remove failed");
                for (int j = i + 1; j < valArray.length; j++) {
                    Integer jVal = valArray[j];
                    Assertions.assertTrue(list.contains(jVal), "val is absent" + jVal);
                }
            }
            Assertions.assertEquals(capacity, list.size());
        } finally {
            list.endItr();
            Assertions.assertEquals(0, list.realSize());
        }
    }

}