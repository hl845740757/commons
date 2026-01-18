package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/7/1
 */
public class DsonNumberTest {

    static final String numberString = """
            {
              value1: 10001,
              value2: 1.05,
              value3: @i 0xFF,
              value4: @i 0b10010001,
              value5: @i 100_000_000,
              value6: @d 1.05E-15,
              value7: @d Infinity,
              value8: @d NaN,
              value9: @i -0xFF,
              value10: @i -0b10010001,
              value11: @d -1.05E-15,
              value12: @d -1.123456789
            }
            """;

    @Test
    void testNumber() {
        DsonObject<String> dsonObject = Dsons.fromDson(numberString).asObject();
        // 必须带类型，否则无法精确反序列化，断言会失败
        List<NumberStyle> styleList = List.of(
                NumberStyle.TYPED, NumberStyle.UNSIGNED,
                NumberStyle.HEX, NumberStyle.UNSIGNED_HEX, NumberStyle.FIXED_HEX,
                NumberStyle.BINARY, NumberStyle.UNSIGNED_BINARY, NumberStyle.FIXED_BINARY,
                NumberStyle.NO_EXPONENT3, NumberStyle.NO_EXPONENT7);

        for (NumberStyle style : styleList) {
            final boolean supportFloat = supportFloat(style);
            final StringWriter stringWriter = new StringWriter(120);
            try (DsonTextWriter writer = new DsonTextWriter(DsonTextWriterSettings.DEFAULT, stringWriter)) {
                writer.writeStartObject(ObjectStyle.INDENT);
                for (int i = 1; i <= dsonObject.size(); i++) {
                    String name = "value" + i;
                    DsonValue dsonValue = dsonObject.get(name);
                    if (dsonValue == null) {
                        break;
                    }
                    DsonNumber dsonNumber = dsonValue.asNumber();
                    switch (dsonNumber.getDsonType()) {
                        case INT32 -> writer.writeInt32(name, dsonNumber.intValue(), style);
                        case INT64 -> writer.writeInt64(name, dsonNumber.longValue(), style);
                        case FLOAT ->
                                writer.writeFloat(name, dsonNumber.floatValue(), supportFloat ? style : NumberStyle.TYPED);
                        case DOUBLE ->
                                writer.writeDouble(name, dsonNumber.doubleValue(), supportFloat ? style : NumberStyle.SIMPLE);
                    }
                }
                writer.writeEndObject();
            }
            String dsonString2 = stringWriter.toString();
            System.out.println(style);
            System.out.println(dsonString2);
            // 截断后无法保证相等性
            if (style == NumberStyle.NO_EXPONENT3 || style == NumberStyle.NO_EXPONENT7) {
                continue;
            }
            Assertions.assertEquals(dsonObject, Dsons.fromDson(dsonString2));
        }
    }

    private static boolean supportFloat(NumberStyle style) {
        return style == NumberStyle.SIMPLE
                || style == NumberStyle.TYPED
                || style == NumberStyle.NO_EXPONENT3
                || style == NumberStyle.NO_EXPONENT7;
    }
}