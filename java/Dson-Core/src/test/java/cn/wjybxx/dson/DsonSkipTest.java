package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonInputs;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.io.DsonOutputs;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/7/1
 */
public class DsonSkipTest {

    private static List<DsonReader> createReaders() {
        DsonObject<String> dsonObject = DsonCodecTest.genRandObject();

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
        for (DsonReader reader : createReaders()) {
            try (reader) {
                reader.readStartObject();
                reader.skipToEndOfObject();
                reader.readEndObject();
                Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
            }
        }
        for (DsonReader reader : createReaders()) {
            try (reader) {
                reader.readStartObject();
                while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                    reader.skipName();
                    reader.skipValue();
                }
                reader.readEndObject();
                Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
            }
        }
        // 当读取到一个嵌套对象时，跳过整个对象
        for (DsonReader reader : createReaders()) {
            try (reader) {
                reader.readStartObject();
                while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                    if (reader.isAtName()) {
                        String name = reader.readName();
                        if (name.equals("pos")) {
                            reader.skipToEndOfObject();
                            break;
                        }
                        reader.skipValue();
                    } else {
                        reader.skipValue();
                    }
                }
                reader.readEndObject();
                Assertions.assertSame(reader.readDsonType(), DsonType.END_OF_OBJECT);
            }
        }
    }
}