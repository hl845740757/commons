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
import org.junit.jupiter.api.Test;

/**
 * 浮点数压缩率测试
 *
 * @author wjybxx
 * date - 2023/7/17
 */
public class FloatCompressTest {

    private static final int COUNT = 100000;
    /**
     * delta如果是不能2进制精确表达的，那么是不能节省字节的；
     * delta如果是能2进制精确表达的，才可以节省存储空间。
     */
    private static final float FLOAT_DELTA = 0.0625F;
    private static final double DOUBLE_DELTA = 0.0625D;

    @Test
    void floatTest() {
        byte[] buffer = new byte[4 * COUNT];
        float[] valueArray = new float[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                float v = i * FLOAT_DELTA;
                valueArray[i] = v;
                int wireType = DsonReaderUtils.wireTypeOfFloat(v);
                DsonReaderUtils.writeFloat(dsonOutput, v, wireType);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                float v = valueArray[i];
                int wireType = DsonReaderUtils.wireTypeOfFloat(v);
                float v2 = DsonReaderUtils.readFloat(dsonInput, wireType);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.printf("float totalSize: %d, saved: %d%n", totalSize, (4 * COUNT - totalSize));
    }

    @Test
    void doubleTest() {
        byte[] buffer = new byte[8 * COUNT];
        double[] valueArray = new double[COUNT];
        int totalSize = 0;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                double v = i * DOUBLE_DELTA;
                valueArray[i] = v;
                int wireType = DsonReaderUtils.wireTypeOfDouble(v);
                DsonReaderUtils.writeDouble(dsonOutput, v, wireType);
            }
            dsonOutput.flush();
            totalSize = dsonOutput.getPosition();
        }
        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                double v = valueArray[i];
                int wireType = DsonReaderUtils.wireTypeOfDouble(v);
                double v2 = DsonReaderUtils.readDouble(dsonInput, wireType);
                Assertions.assertEquals(v, v2);
            }
        }
        System.out.printf("double totalSize: %d, saved: %d%n", totalSize, (8 * COUNT - totalSize));
    }

}