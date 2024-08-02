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

import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/4
 */
public class DsonTextReaderTest {

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
              @bin "FFFA",
              @bin ""
            ],
            [@{localId: 17630eb4f916148b}]
            """;

    /**
     * 程序生成的无法保证和手写的文本相同
     * 但程序反复读写，以及不同方式之间的读写结果应当相同。
     */
    @Test
    void test_equivalenceOfAllReaders() {
        DsonArray<String> collection1 = Dsons.fromCollectionDson(dsonString);
        String dsonString1 = Dsons.toCollectionDson(collection1);
//        System.out.println(dsonString1);
//        Assertions.assertEquals(dsonString, dsonString1.replace("\r\n", "\n")); // 统一换行符为\n

        // Binary
        {
            byte[] buffer = new byte[8192];
            DsonOutput output = DsonOutputs.newInstance(buffer);
            try (DsonWriter writer = new DsonBinaryWriter(DsonTextWriterSettings.DEFAULT, output)) {
                Dsons.writeCollection(writer, collection1);
            }
            DsonInput input = DsonInputs.newInstance(buffer, 0, output.getPosition());
            try (DsonReader reader = new DsonBinaryReader(DsonTextReaderSettings.DEFAULT, input)) {
                DsonArray<String> collection2 = Dsons.readCollection(reader);
                Assertions.assertEquals(collection1, collection2, "BinaryReader/BinaryWriter");

                String dsonString2 = Dsons.toCollectionDson(collection2);
                Assertions.assertEquals(dsonString1, dsonString2, "BinaryReader/BinaryWriter");
            }
        }
        // Object
        {
            DsonArray<String> outList = new DsonArray<>();
            try (DsonWriter writer = new DsonCollectionWriter(DsonTextWriterSettings.DEFAULT, outList)) {
                Dsons.writeCollection(writer, collection1);
            }
            try (DsonReader reader = new DsonCollectionReader(DsonTextReaderSettings.DEFAULT, outList)) {
                DsonArray<String> collection3 = Dsons.readCollection(reader);
                Assertions.assertEquals(collection1, collection3, "ObjectReader/ObjectWriter");

                String dsonString3 = Dsons.toCollectionDson(collection3);
                Assertions.assertEquals(dsonString1, dsonString3, "ObjectReader/ObjectWriter");
            }
        }
        // text
        {
            StringBuilderWriter stringWriter = new StringBuilderWriter();
            try (DsonWriter writer = new DsonTextWriter(DsonTextWriterSettings.DEFAULT, stringWriter)) {
                Dsons.writeCollection(writer, collection1);
            }
            try (DsonReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, stringWriter.toString())) {
                DsonArray<String> collection4 = Dsons.readCollection(reader);
                Assertions.assertEquals(collection1, collection4, "TextReader/TextWriter");

                String dsonString4 = Dsons.toCollectionDson(collection4);
                Assertions.assertEquals(dsonString1, dsonString4, "TextReader/TextWriter");
            }
        }
    }

    @Test
    void testRef() {
        DsonRepository repository = DsonRepository
                .fromDson(new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString))
                .resolveReference();
        Assertions.assertInstanceOf(DsonArray.class, repository.find("10001"));
        Assertions.assertInstanceOf(DsonArray.class, repository.find("17630eb4f916148b"));
    }

}