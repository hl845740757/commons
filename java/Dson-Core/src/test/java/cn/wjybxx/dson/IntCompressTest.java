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

package cn.wjybxx.dson;

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * 整数压缩率测试
 *
 * @author wjybxx
 * date - 2023/7/17
 */
public class IntCompressTest {

    private static final int COUNT = 100000;

    @Test
    void int32Test() {
        byte[] buffer = new byte[5 * COUNT];
        int[] valueArray = new int[COUNT];
        int totalSize = 0;
        int varInt = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                int v = MathCommon.SHARED_RANDOM.nextInt(-10000, 100 * 10000);
                valueArray[i] = v;
                WireType wireType = WireType.bestOfInt32(v);
                wireType.writeInt32(dsonOutput, v);
                if (wireType != WireType.FIXED) {
                    varInt++;
                }
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                int v = valueArray[i];
                WireType wireType = WireType.bestOfInt32(v);
                int v2 = wireType.readInt32(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.printf("int32 totalSize: %d, saved: %d, varInt: %d%n", totalSize, (4 * COUNT - totalSize), varInt);
    }

    @Test
    void int64Test() {
        byte[] buffer = new byte[10 * COUNT];
        long[] valueArray = new long[COUNT];
        int totalSize = 0;
        int varInt = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                long v = MathCommon.SHARED_RANDOM.nextLong(-100000, 20L * Integer.MAX_VALUE);
                valueArray[i] = v;
                WireType wireType = WireType.bestOfInt64(v);
                wireType.writeInt64(dsonOutput, v);
                if (wireType != WireType.FIXED) {
                    varInt++;
                }
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                long v = valueArray[i];
                WireType wireType = WireType.bestOfInt64(v);
                long v2 = wireType.readInt64(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.printf("int64 totalSize: %d, saved: %d, varInt: %d%n", totalSize, (8 * COUNT - totalSize), varInt);
    }

}