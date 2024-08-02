package cn.wjybxx.dson;

import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/12/31
 */
public class ProjectionTest {

    private static final String dsonString = """
            {@{clsName:MyClassInfo, guid :10001, flags: 0}
              name : wjybxx,
              age: 28,
              pos :{@{Vector3} x: 1, y: 2, z: 3},
              address: [
                beijing,
                chengdu,
                shanghai
              ],
              posArr: [
               {@{V3} x: 1, y: 1, z: 1},
               {@{V3} x: 2, y: 2, z: 2},
               {@{V3} x: 3, y: 3, z: 3}
              ],
              url: @sL https://www.github.com/hl845740757
            }
            """;

    private static final String projectInfo = """
            {
              name: 1,
              age: 1,
              pos: {
                $header: 1,
                $all: 1,
                z: 0
              },
              address: {
                $slice : [1, 2] //跳过第一个元素，然后返回两个元素
              },
              posArr: {
                $header : 1, //返回数组的header
                $slice : 0, // 返回全部元素
                $elem: {  //投影数组元素的x和y
                  x: 1,
                  z: 1
                }
              }
            }
            """;

    @Test
    void test() {
        DsonObject<String> expected = new DsonObject<>();
        {
            DsonObject<String> dsonObject = Dsons.fromDson(dsonString).asObject();
            transfer(expected, dsonObject, "name");
            transfer(expected, dsonObject, "age");
            {
                DsonObject<String> rawPos = dsonObject.get("pos").asObject();
                DsonObject<String> newPos = new DsonObject<>();
                transfer(newPos, rawPos, "x");
                transfer(newPos, rawPos, "y");
                expected.put("pos", newPos);
            }
            {
                DsonArray<String> rawAddress = dsonObject.get("address").asArray();
                DsonArray<String> newAddress = rawAddress.slice(1);
                expected.put("address", newAddress);
            }

            DsonArray<String> rawPosArr = dsonObject.get("posArr").asArray();
            DsonArray<String> newPosArr = new DsonArray<>(3);
            for (DsonValue ele : rawPosArr) {
                DsonObject<String> rawPos = ele.asObject();
                DsonObject<String> newPos = new DsonObject<>();
                transfer(newPos, rawPos, "x");
                transfer(newPos, rawPos, "z");
                newPosArr.add(newPos);
            }
            expected.put("posArr", newPosArr);
        }

        DsonObject<String> value = Dsons.project(dsonString, projectInfo).asObject();
        System.out.println(Dsons.toDson(value, ObjectStyle.INDENT));
        Assertions.assertEquals(expected, value);
    }

    private static void transfer(DsonObject<String> expected, DsonObject<String> dsonObject, String key) {
        expected.put(key, dsonObject.get(key));
    }
}