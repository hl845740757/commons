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

import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.RepeatedTest;

import java.util.Random;

/**
 * 测试数字压缩算法的正确性
 *
 * @author wjybxx
 * date - 2024/11/07
 */
public class NumberCodedTest {

    private static final int COUNT = 100000;
    private static int repeat = 0;
    private static Random random = new Random();

    @BeforeEach
    void setUp() {
        repeat++;
    }

    @RepeatedTest(3)
    void testInt32() {
        WireType wireType = WireType.forNumber(repeat % 3);
        System.out.println("Begin: WireType: " + wireType);

        byte[] buffer = new byte[5 * COUNT];
        int[] valueArray = new int[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                int v = random.nextInt();
                valueArray[i] = v;
                wireType.writeInt32(dsonOutput, v);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                int v = valueArray[i];
                int v2 = wireType.readInt32(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.println("End: WireType: " + wireType);
    }

    @RepeatedTest(3)
    void testInt64() {
        WireType wireType = WireType.forNumber(repeat % 3);
        System.out.println("Begin: WireType: " + wireType);

        byte[] buffer = new byte[10 * COUNT];
        long[] valueArray = new long[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                long v = random.nextLong();
                valueArray[i] = v;
                wireType.writeInt64(dsonOutput, v);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                long v = valueArray[i];
                long v2 = wireType.readInt64(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.println("End: WireType: " + wireType);
    }

    @RepeatedTest(2)
    void testFloat() {
        WireType wireType = (repeat & 1) == 1 ? WireType.UINT : WireType.FIXED;
        System.out.println("Begin: WireType: " + wireType);

        byte[] buffer = new byte[10 * COUNT];
        float[] valueArray = new float[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                float v = random.nextFloat();
                valueArray[i] = v;
                wireType.writeFloat(dsonOutput, v);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                float v = valueArray[i];
                float v2 = wireType.readFloat(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.println("End: WireType: " + wireType);
    }

    @RepeatedTest(2)
    void testDouble() {
        WireType wireType = (repeat & 1) == 1 ? WireType.UINT : WireType.FIXED;
        System.out.println("Begin: WireType: " + wireType);

        byte[] buffer = new byte[10 * COUNT];
        double[] valueArray = new double[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                double v = i * random.nextDouble();
                valueArray[i] = v;
                wireType.writeDouble(dsonOutput, v);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                double v = valueArray[i];
                double v2 = wireType.readDouble(dsonInput);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.println("End: WireType: " + wireType);
    }

}