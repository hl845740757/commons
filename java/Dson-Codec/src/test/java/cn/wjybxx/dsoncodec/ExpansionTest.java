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

package cn.wjybxx.dsoncodec;

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.pool.ArrayPool;
import cn.wjybxx.base.pool.ConcurrentArrayPool;
import cn.wjybxx.dson.internal.CodedUtils;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTexts;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.Arrays;
import java.util.Random;

/**
 * 编码时数组扩容测试
 * <p>
 * 测试的方法很简单，创建两个流，一个扩容，一个不扩容，测试最终的内容相等性
 *
 * @author wjybxx
 * date - 2025/7/1
 */
public class ExpansionTest {

    private static final int MAX_CAPACITY = 2048;
    private static final byte[] _buffer = new byte[2048];

    private static DsonOutput _output;
    private static DsonOutputs.ArrayOutput _growableOutput;

    @BeforeEach
    public void SetUp() {
        Arrays.fill(_buffer, (byte) 0);
        _output = DsonOutputs.newInstance(_buffer);

        ArrayPool<byte[]> bufferPool = ConcurrentArrayPool.SHARED_BYTE_ARRAY_POOL;
        _growableOutput = DsonOutputs.newInstance(bufferPool, 16, 2048);
    }

    @Test
    public void TestNumber() {
        DsonOutputs.ArrayOutput growableOutput = _growableOutput;
        try (growableOutput) {
            while (_output.getPosition() < MAX_CAPACITY - CodedUtils.MAX_VAR_INT32_LENGTH) {
                int v = MathCommon.SHARED_RANDOM.nextInt();
                _output.writeUInt32(v);
                _growableOutput.writeUInt32(v);
            }
            _output.flush();
            _growableOutput.flush();
            Assertions.assertEquals(_output.getPosition(), _growableOutput.getPosition());
            // 数组的长度不一定一致，只比较内容
            byte[] first = Arrays.copyOf(_buffer, _output.getPosition());
            byte[] second = Arrays.copyOf(_growableOutput.getBuffer(), _growableOutput.getPosition());
            Assertions.assertArrayEquals(first, second);
        }
    }

    @RepeatedTest(5)
    public void TestString() {
        // 通过Random.NextBytes() 构建出来的字符串可能包含非法字符
        DsonOutputs.ArrayOutput growableOutput = _growableOutput;
        try (growableOutput) {
            while (true) {
                int len = MathCommon.SHARED_RANDOM.nextInt(5, 256);
                String str = GenerateString(MathCommon.SHARED_RANDOM, len, true, true);
                int byteCount = DsonTexts.getUtf8Length(str);
                if (_output.getPosition() + byteCount + CodedUtils.MAX_VAR_INT32_LENGTH > MAX_CAPACITY) {
                    break;
                }
                _output.writeString(str);
                _growableOutput.writeString(str);
            }
            _output.flush();
            _growableOutput.flush();
            Assertions.assertEquals(_output.getPosition(), _growableOutput.getPosition());
            // 数组的长度不一定一致，只比较内容
            byte[] first = Arrays.copyOf(_buffer, _output.getPosition());
            byte[] second = Arrays.copyOf(_growableOutput.getBuffer(), _growableOutput.getPosition());
            Assertions.assertArrayEquals(first, second);
        }
    }

    public static String GenerateString(Random rand, int length,
                                        boolean includeSymbols, boolean includeChinese) {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            int choice = rand.nextInt(0, 100);
            if (includeChinese && choice < 20) {
                // 20%概率生成汉字 -- 这概率是体育老师教的
                sb.append((char) rand.nextInt(0x4E00, 0x9FA5));
            } else if (includeSymbols && choice < 40) {
                // 20%概率生成符号
                sb.append((char) rand.nextInt(33, 48));
            } else if (choice < 70) {
                // 30%概率生成大写字母
                sb.append((char) rand.nextInt(65, 91));
            } else {
                // 30%概率生成小写字母
                sb.append((char) rand.nextInt(97, 123));
            }
        }
        return sb.toString();
    }
}