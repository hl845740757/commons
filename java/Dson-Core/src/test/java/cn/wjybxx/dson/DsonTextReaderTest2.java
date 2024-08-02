package cn.wjybxx.dson;

import cn.wjybxx.dson.text.DsonTextWriterSettings;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/15
 */
public class DsonTextReaderTest2 {

    static final String dsonString = """
            // 以下是一个简单的DsonObject示例
            {@{clsName:MyClassInfo, guid :10001, flags: 0}
              name : wjybxx,
              age: 28,
              pos :{@{Vector3} x: 0, y: 0, z: 0},
              address: [
                beijing,
                chengdu
              ],
              intro:
              \"""
                我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，你可以通过github联系到我。
                thanks
              \"""
              , url: @sL https://www.github.com/hl845740757
              , time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, offset: +08:00}
            }
            """;

    @Test
    void testIndentStyle() {
        DsonObject<String> dsonObject = Dsons.fromDson(dsonString).asObject();
        // 纯文本左对齐
        {
            String dsonString2 = Dsons.toDson(dsonObject, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                    .setSoftLineLength(50)
                    .setTextStringLength(50)
                    .setTextAlignLeft(true)
                    .build());
            System.out.println("IndentStyle, alignLeft: true");
            System.out.println(dsonString2);
            DsonValue dsonObject2 = Dsons.fromDson(dsonString2);
            Assertions.assertEquals(dsonObject, dsonObject2);
        }
        System.out.println();
        // 纯文本不对齐
        {
            String dsonString2 = Dsons.toDson(dsonObject, ObjectStyle.INDENT, DsonTextWriterSettings.newBuilder()
                    .setSoftLineLength(60)
                    .setTextStringLength(50)
                    .setTextAlignLeft(false)
                    .build());
            System.out.println("IndentStyle, alignLeft:false");
            System.out.println(dsonString2);
            DsonValue dsonObject2 = Dsons.fromDson(dsonString2);
            Assertions.assertEquals(dsonObject, dsonObject2);
        }
        System.out.println();
    }

    /** flow样式需要较长的行，才不显得拥挤 */
    @Test
    void testFlowStyle() {
        DsonObject<String> dsonObject = Dsons.fromDson(dsonString).asObject();
        {
            String dsonString2 = Dsons.toDson(dsonObject, ObjectStyle.FLOW, DsonTextWriterSettings.newBuilder()
                    .setSoftLineLength(120)
                    .setTextStringLength(50)
                    .setEnableText(false)
                    .setTextAlignLeft(false)
                    .build());
            System.out.println("FlowStyle, alignLeft:false");
            System.out.println(dsonString2);
            DsonValue dsonObject2 = Dsons.fromDson(dsonString2);
            Assertions.assertEquals(dsonObject, dsonObject2);
        }
        System.out.println();
        {
            String dsonString3 = Dsons.toDson(dsonObject, ObjectStyle.FLOW, DsonTextWriterSettings.newBuilder()
                    .setSoftLineLength(120)
                    .setEnableText(true)
                    .setTextAlignLeft(true)
                    .build());
            System.out.println("FlowStyle, alignLeft:true");
            System.out.println(dsonString3);
            DsonValue dsonObject3 = Dsons.fromDson(dsonString3);
            Assertions.assertEquals(dsonObject, dsonObject3);
        }
        System.out.println();
    }
}