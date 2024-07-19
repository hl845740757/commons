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
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ThreadLocalRandom;

/**
 * @author wjybxx
 * date - 2024/1/6
 */
public class ConcurrentArrayPoolTest {

    @Test
    void testSpsc() {
        int minLen = 100;
        int maxLen = 1500;

        ConcurrentArrayPool<byte[]> arrayPool = ConcurrentArrayPool.newBuilder(byte[].class)
                .setDefCapacity(minLen)
                .setMaxCapacity(maxLen)
                .setBucketGrowFactor(0.75)
                .setClear(false)
                .build();

        testImpl(arrayPool);
    }

    @RepeatedTest(5)
    void testMpmc() {
        int minLen = 100;
        int maxLen = 1500;

        ConcurrentArrayPool<byte[]> arrayPool = ConcurrentArrayPool.newBuilder(byte[].class)
                .setDefCapacity(minLen)
                .setMaxCapacity(maxLen)
                .setClear(false)
                .build();

        int treadCount = 8;
        List<Thread> threads = new ArrayList<>(treadCount);
        for (int i = 0; i < treadCount; i++) {
            threads.add(new Thread(() -> testImpl(arrayPool)));
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
    }

    @Test
    void testConfig() {
        ConcurrentArrayPool<byte[]> arrayPool = ConcurrentArrayPool.newBuilder(byte[].class)
                .addBucket(256, 100)
                .addBucket(1024, 100)
                .addBucket(2048, 80)
                .addBucket(4096,60)
                .addBucket(8192, 50)
                .addBucket(10000, 20)
                .setClear(false)
                .build();
        testImpl(arrayPool);
    }

    private static void testImpl(ConcurrentArrayPool<byte[]> arrayPool) {
        ThreadLocalRandom random = ThreadLocalRandom.current();
        for (int j = 0; j < 100000; j++) {
            int minimumLength = random.nextInt(0, 2048);
            byte[] bytes = arrayPool.acquire(minimumLength);
            Assertions.assertTrue(bytes.length >= minimumLength);
            arrayPool.release(bytes);
        }
    }

}