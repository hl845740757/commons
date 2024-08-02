package cn.wjybxx.dson;

import cn.wjybxx.dson.types.ExtDateTime;
import cn.wjybxx.dson.types.Timestamp;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/28
 */
public class DsonTimeTest {

    private static final String dsonTimeString = """
            [
              @dt 2023-06-17T18:37:00,
              {@dt date: 2023-06-17, time: 18:37:00},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000}
            ]
            """;

    @Test
    void testDateTime() {
        DsonArray<String> dsonArray = Dsons.fromDson(dsonTimeString).asArray();
        Assertions.assertEquals(dsonArray.get(0), dsonArray.get(1));

        ExtDateTime time3 = dsonArray.get(2).asDateTime();
        ExtDateTime time4 = dsonArray.get(3).asDateTime();
        ExtDateTime time5 = dsonArray.get(4).asDateTime();
        // 偏移相同
        Assertions.assertEquals(time3.getOffset(), time4.getOffset());
        Assertions.assertEquals(time3.getOffset(), time5.getOffset());
        // 纳秒部分相同
        Assertions.assertEquals(time4.getNanos(), time5.getNanos());

        // 测试编解码
        String dsonString2 = Dsons.toDson(dsonArray);
        System.out.println(dsonString2);
        DsonValue dsonArray2 = Dsons.fromDson(dsonString2);
        Assertions.assertEquals(dsonArray, dsonArray2);
    }

    private static final String dsonTimestampString = """
            [
              @ts 1715659200,
              @ts 1715659200100ms,
              {@ts seconds: 1715659200, millis: 100},
              {@ts seconds: 1715659200, nanos: 100_000_000}
            ]
            """;

    @Test
    void testTimestamp() {
        DsonArray<String> dsonArray = Dsons.fromDson(dsonTimestampString).asArray();

        Timestamp timestamp1 = dsonArray.get(0).asTimestamp();
        Timestamp timestamp2 = dsonArray.get(1).asTimestamp();
        Timestamp timestamp3 = dsonArray.get(2).asTimestamp();
        Timestamp timestamp4 = dsonArray.get(3).asTimestamp();

        // 秒部分相同
        Assertions.assertEquals(timestamp1.getSeconds(), timestamp2.getSeconds());
        Assertions.assertEquals(timestamp1.getSeconds(), timestamp3.getSeconds());
        Assertions.assertEquals(timestamp1.getSeconds(), timestamp4.getSeconds());
        // 纳秒部分相同
        Assertions.assertEquals(timestamp2.getNanos(), timestamp3.getNanos());
        Assertions.assertEquals(timestamp2.getNanos(), timestamp4.getNanos());

        // 测试编解码
        String dsonString2 = Dsons.toDson(dsonArray);
        System.out.println(dsonString2);
        DsonValue dsonArray2 = Dsons.fromDson(dsonString2);
        Assertions.assertEquals(dsonArray, dsonArray2);
    }

}