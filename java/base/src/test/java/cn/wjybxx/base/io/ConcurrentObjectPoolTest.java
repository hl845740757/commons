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
import org.junit.jupiter.api.RepeatedTest;

import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2024/1/6
 */
public class ConcurrentObjectPoolTest {

    @RepeatedTest(5)
    void testConcurrentPool() {
        ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.clear();

        int treadCount = 8;
        List<Thread> threads = new ArrayList<>(treadCount);
        for (int i = 0; i < treadCount; i++) {
            threads.add(new Thread(ConcurrentObjectPoolTest::testImpl));
        }
        for (Thread thread : threads) {
            thread.start();
        }
        // 等待退出
        for (Thread t : threads) {
            try {
                t.join();
            } catch (InterruptedException ignore) {
            }
        }
        int availableCount = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.getAvailableCount();
        Assertions.assertEquals(availableCount, treadCount);
    }

    private static void testImpl() {
        ConcurrentObjectPool<StringBuilder> objectPool = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL;
        for (int j = 0; j < 100000; j++) {
            var obj = objectPool.acquire();
            objectPool.release(obj);
        }
    }
}