package cn.wjybxx.dson;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/15
 */
public class SetKeyItrTest {

    @Test
    void test() {
        DsonObject<String> dsonObject = new DsonObject<>(3);
        dsonObject.append("3", new DsonString("3"))
                .append("2", new DsonString("2"))
                .append("1", new DsonString("1"));

        List<String> expectedKeyList = new ArrayList<>(dsonObject.keySet());
        Collections.reverse(expectedKeyList);

        try (DsonCollectionReader objectReader = new DsonCollectionReader(DsonReaderSettings.DEFAULT, new DsonArray<String>(1).append(dsonObject))) {
            objectReader.readDsonType();

            objectReader.readStartObject();
            objectReader.setKeyItr(expectedKeyList.iterator(), DsonNull.UNDEFINE);
            int index = 0;
            while (objectReader.readDsonType() != DsonType.END_OF_OBJECT) {
                Assertions.assertEquals(expectedKeyList.get(index++), objectReader.readName());
                objectReader.skipValue();
            }
            objectReader.readEndObject();

            Assertions.assertEquals(DsonType.END_OF_OBJECT, objectReader.readDsonType());
        }
    }

}