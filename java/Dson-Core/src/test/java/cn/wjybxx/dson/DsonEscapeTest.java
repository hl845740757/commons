package cn.wjybxx.dson;

import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/22
 */
public class DsonEscapeTest {

    private static final String regExp = "^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$";

    /** Java """ 中会执行'\'转义，c#中不会.... */
    static final String dsonString = """
            {
              // @ss 纯文本模式下输入正则表达式
              reg1:
              @\"""
              @- ^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$
              @\""",
              // 在纯文本模式插入转义版本的正则表达式
              reg2:
              @\"""
              @^ ^[\\\\u4e00-\\\\u9fa5_a-zA-Z0-9]+$
              @\""",
              // @sL 单行纯文本模式
              reg3: @sL ^[\\u4e00-\\u9fa5_a-zA-Z0-9]+$
            }
            """;

    @Test
    void test() {
        DsonValue value = Dsons.fromDson(dsonString);
        DsonString reg1 = (DsonString) value.asObject().get("reg1");
        Assertions.assertEquals(regExp, reg1.getValue());

        DsonString reg2 = (DsonString) value.asObject().get("reg2");
        Assertions.assertEquals(regExp, reg2.getValue());

        DsonString reg3 = (DsonString) value.asObject().get("reg3");
        Assertions.assertEquals(regExp, reg3.getValue());

        System.out.println(Dsons.toDson(value, ObjectStyle.INDENT));
    }
}