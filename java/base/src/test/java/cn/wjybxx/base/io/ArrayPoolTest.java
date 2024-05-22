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

package cn.wjybxx.base.io;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2024/1/6
 */
public class ArrayPoolTest {

    @Test
    void testRent() {
        int minLen = 64;
        int maxLen = 1024;

        SimpleArrayPool<byte[]> arrayPool = new SimpleArrayPool<>(byte[].class, 4, minLen, maxLen);
        {
            Assertions.assertEquals(arrayPool.acquire().length, minLen);
        }
        {
            byte[] array = arrayPool.acquire(1023);
            Assertions.assertEquals(array.length, 1023);
//            arrayPool.returnOne(array); // 不放入池中，以测试后续是否返回1024的数组
        }
        {
            byte[] array = arrayPool.acquire(maxLen);
            Assertions.assertEquals(array.length, maxLen);
            arrayPool.release(array);
        }
        {
            byte[] array = arrayPool.acquire(1023);
            Assertions.assertEquals(array.length, maxLen);
            arrayPool.release(array);
        }
    }

    @Test
    void testClear() {
        Class<?>[] primitiveArray = new Class<?>[]{
                byte[].class, char[].class,
                int[].class, long[].class,
                float[].class, double[].class,
                short[].class, boolean[].class
        };
        for (Class<?> arrayType : primitiveArray) {
            testClear(arrayType);
        }
    }

    private <T> void testClear(Class<T> arrayType) {
        int minLen = 64;
        int maxLen = 1024;
        SimpleArrayPool<T> arrayPool = new SimpleArrayPool<>(arrayType, 4, minLen, maxLen);
        T object = arrayPool.acquire(1024, true);
        arrayPool.release(object, true);
    }
}