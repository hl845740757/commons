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

package cn.wjybxx.dson.pb;

import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * 测试与自实现的ArrayOutput的相等性
 *
 * @author wjybxx
 * date - 2023/12/16
 */
public class ArrayCodedTest {

    static final String dsonString = """
            @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            {@{MyStruct}
              name: wjybxx,
              age: 28,
              介绍: "这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长",
              intro: "hello world",
              ptr1: @ptr 17630eb4f916148b,
              ptr2: {@ptr ns: 16148b3b4e7b8923d398, localId: "10001"},
              bin: @bin "35DF2E75E6A4BE9E6F4571C64CB6D08B0D6BC46C1754F6E9EB4A6E57E2FD53",
              bin2: @bin ""
            },
            {@{MyStruct}
              name: wjybxx,
              intro: "hello world",
              ptr1: @ptr 17630eb4f916148b,
              ptr2: {@ptr ns: 16148b3b4e7b8923d398, localId: "10001"},
              lptr1: @lptr 10001,
              lptr2: {@lptr ns: global, localId: 10001}
            },
            [@{localId: "10001"}
            ],
            [@{localId: 17630eb4f916148b}
            ]""";

    @Test
    void arrayOutputTest() {
        DsonArray<String> collection1 = Dsons.fromCollectionDson(dsonString);
        String dsonString1 = Dsons.toCollectionDson(collection1);
//        System.out.println(dsonString1);

        byte[] buffer1 = new byte[2048];
        DsonOutput output1;
        byte[] buffer2 = new byte[2048];
        DsonOutput output2;
        // 编码
        {
            // 自实现
            output1 = DsonOutputs.newInstance(buffer1);
            try (DsonWriter writer = new DsonBinaryWriter(DsonTextWriterSettings.DEFAULT, output1)) {
                Dsons.writeCollection(writer, collection1);
            }
            // pb实现
            output2 = DsonProtobufOutputs.newInstance(buffer2);
            try (DsonWriter writer = new DsonBinaryWriter(DsonTextWriterSettings.DEFAULT, output2)) {
                Dsons.writeCollection(writer, collection1);
            }
            Assertions.assertEquals(output1.getPosition(), output2.getPosition());
            Assertions.assertArrayEquals(buffer1, buffer2);
        }
        // 解码
        {
            // 自实现
            DsonInput input1 = DsonInputs.newInstance(buffer1, 0, output1.getPosition());
            try (DsonReader reader = new DsonBinaryReader(DsonTextReaderSettings.DEFAULT, input1)) {
                DsonArray<String> collection2 = Dsons.readCollection(reader);

                String dsonString2 = Dsons.toCollectionDson(collection2);
                Assertions.assertEquals(dsonString1, dsonString2, "my-BinaryReader/BinaryWriter");
            }
            // pb实现
            DsonInput input2 = DsonProtobufInputs.newInstance(buffer2, 0, output1.getPosition());
            try (DsonReader reader = new DsonBinaryReader(DsonTextReaderSettings.DEFAULT, input2)) {
                DsonArray<String> collection3 = Dsons.readCollection(reader);

                String dsonString2 = Dsons.toCollectionDson(collection3);
                Assertions.assertEquals(dsonString1, dsonString2, "pb-BinaryReader/BinaryWriter");
            }
        }
    }
}