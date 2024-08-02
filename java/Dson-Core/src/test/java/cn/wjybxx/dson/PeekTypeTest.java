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

import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.RandomUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.List;

/**
 * 测试{@link DsonReader#peekDsonType()}
 *
 * @author wjybxx
 * date - 2023/8/9
 */
@SuppressWarnings("deprecation")
public class PeekTypeTest {

    private static List<DsonReader> createReaders(DsonObject<String> dsonObject) {
        byte[] buffer = new byte[4096];
        DsonOutput dsonOutput = DsonOutputs.newInstance(buffer);
        try (DsonBinaryWriter writer = new DsonBinaryWriter(DsonWriterSettings.DEFAULT, dsonOutput)) {
            Dsons.writeTopDsonValue(writer, dsonObject, ObjectStyle.INDENT);
        }

        String dsonString = Dsons.toDson(dsonObject, ObjectStyle.INDENT);
        return List.of(new DsonCollectionReader(DsonReaderSettings.DEFAULT, new DsonArray<String>(1).append(dsonObject)),
                new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString),
                new DsonBinaryReader(DsonReaderSettings.DEFAULT, DsonInputs.newInstance(buffer, 0, dsonOutput.getPosition())));
    }

    @Test
    void test() {
        DsonObject<String> dsonObject = DsonCodecTest.genRandObject();
        List<DsonReader> dsonReaders = createReaders(dsonObject);

        for (DsonReader reader : dsonReaders) {
            DsonObject<String> object = new DsonObject<>();
            try (reader) {
                Assertions.assertEquals(DsonType.OBJECT, reader.peekDsonType());
                Assertions.assertEquals(DsonType.OBJECT, reader.readDsonType());
                readToObject(reader, object);
                Assertions.assertEquals(dsonObject, object);
            }
        }
    }

    @Test
    void test1() {
        try (DsonReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, DsonTextReaderTest.dsonString)) {
            DsonHeader<String> object = new DsonHeader<>();
            Assertions.assertEquals(DsonType.HEADER, reader.peekDsonType());
            Assertions.assertEquals(DsonType.HEADER, reader.readDsonType());
            readToHeader(reader, object);
        }
    }

    @Test
    void test2() {
        try (DsonReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, DsonTextReaderTest2.dsonString)) {
            DsonObject<String> object = new DsonObject<>();
            Assertions.assertEquals(DsonType.OBJECT, reader.peekDsonType());
            Assertions.assertEquals(DsonType.OBJECT, reader.readDsonType());
            readToObject(reader, object);
        }
    }

    private static void readToObject(DsonReader reader, DsonObject<String> object) {
        reader.readStartObject();
        while (true) {
            DsonType dsonType;
            if (RandomUtils.nextBoolean()) {
                DsonType peekedDsonType = reader.peekDsonType();
                dsonType = reader.readDsonType();
                Assertions.assertEquals(peekedDsonType, dsonType);
            } else {
                dsonType = reader.readDsonType();
            }
            if (dsonType == DsonType.END_OF_OBJECT) {
                break;
            }
            if (dsonType == DsonType.HEADER) {
                readToHeader(reader, object.getHeader());
                continue;
            }
            String name = reader.readName();
            DsonValue dsonValue;
            if (dsonType == DsonType.ARRAY) {
                DsonArray<String> childObject = new DsonArray<>();
                readToArray(reader, childObject);
                dsonValue = childObject;
            } else if (dsonType == DsonType.OBJECT) {
                DsonObject<String> childObject = new DsonObject<>();
                readToObject(reader, childObject);
                dsonValue = childObject;
            } else {
                dsonValue = readDsonValue(reader);
            }
            object.put(name, dsonValue);
        }
        reader.readEndObject();
    }

    private static void readToArray(DsonReader reader, DsonArray<String> array) {
        reader.readStartArray();
        while (true) {
            DsonType dsonType;
            if (RandomUtils.nextBoolean()) {
                DsonType peekedDsonType = reader.peekDsonType();
                dsonType = reader.readDsonType();
                Assertions.assertEquals(peekedDsonType, dsonType);
            } else {
                dsonType = reader.readDsonType();
            }
            if (dsonType == DsonType.END_OF_OBJECT) {
                break;
            }
            if (dsonType == DsonType.HEADER) {
                readToHeader(reader, array.getHeader());
                continue;
            }
            DsonValue dsonValue;
            if (dsonType == DsonType.ARRAY) {
                DsonArray<String> childObject = new DsonArray<>();
                readToArray(reader, childObject);
                dsonValue = childObject;
            } else if (dsonType == DsonType.OBJECT) {
                DsonObject<String> childObject = new DsonObject<>();
                readToObject(reader, childObject);
                dsonValue = childObject;
            } else {
                dsonValue = readDsonValue(reader);
            }
            array.add(dsonValue);
        }
        reader.readEndArray();
    }

    private static void readToHeader(DsonReader reader, DsonHeader<String> header) {
        reader.readStartHeader();
        while (true) {
            DsonType dsonType;
            if (RandomUtils.nextBoolean()) {
                DsonType peekedDsonType = reader.peekDsonType();
                dsonType = reader.readDsonType();
                Assertions.assertEquals(peekedDsonType, dsonType);
            } else {
                dsonType = reader.readDsonType();
            }
            if (dsonType == DsonType.END_OF_OBJECT) {
                break;
            }
            if (dsonType == DsonType.HEADER) {
                throw new IllegalStateException("nested header");
            }
            String name = reader.readName();
            DsonValue dsonValue;
            if (dsonType == DsonType.ARRAY) {
                DsonArray<String> childObject = new DsonArray<>();
                readToArray(reader, childObject);
                dsonValue = childObject;
            } else if (dsonType == DsonType.OBJECT) {
                DsonObject<String> childObject = new DsonObject<>();
                readToObject(reader, childObject);
                dsonValue = childObject;
            } else {
                dsonValue = readDsonValue(reader);
            }
            header.put(name, dsonValue);
        }
        reader.readEndHeader();
    }

    public static DsonValue readDsonValue(DsonReader reader) {
        DsonType dsonType = reader.getCurrentDsonType();
        reader.skipName();
        final String name = "";
        return switch (dsonType) {
            case INT32 -> new DsonInt32(reader.readInt32(name));
            case INT64 -> new DsonInt64(reader.readInt64(name));
            case FLOAT -> new DsonFloat(reader.readFloat(name));
            case DOUBLE -> new DsonDouble(reader.readDouble(name));
            case BOOL -> new DsonBool(reader.readBool(name));
            case STRING -> new DsonString(reader.readString(name));
            case NULL -> {
                reader.readNull(name);
                yield DsonNull.NULL;
            }
            case BINARY -> new DsonBinary(reader.readBinary(name));
            case POINTER -> new DsonPointer(reader.readPtr(name));
            case LITE_POINTER -> new DsonLitePointer(reader.readLitePtr(name));
            case DATETIME -> new DsonDateTime(reader.readDateTime(name));
            case TIMESTAMP -> new DsonTimestamp(reader.readTimestamp(name));
            default -> throw new AssertionError();
        };
    }
}