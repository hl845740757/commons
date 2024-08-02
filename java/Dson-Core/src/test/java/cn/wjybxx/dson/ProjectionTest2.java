package cn.wjybxx.dson;

import org.junit.jupiter.api.Test;

/**
 * 测试
 *
 * @author wjybxx
 * date - 2023/12/31
 */
public class ProjectionTest2 {

    private static final String dsonString = """
            @{clsName: MyStruct, collectionName: Authors}
                        
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
            -
            {@{clsName:MyClassInfo, guid :10002, flags: 0}
              name : wjybxx,
              age: 29,
              pos :{@{Vector3} x: 1, y: 2, z: 3},
              address: [
                beijing,
                chengdu,
                shanghai
              ],
              posArr: [@{"V3[]"}
               {@{V3} x: 1, y: 1, z: 1},
               {@{V3} x: 2, y: 2, z: 2},
               {@{V3} x: 3, y: 3, z: 3}
              ],
              url: @sL https://www.github.com/hl845740757
            }
            """;

    private static final String projectInfo = """
            {
              $header : 1,
              $slice: [0, 1],
              $elem:  {
                  $header: 1,
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
                    $slice: 0, // 返回全部元素
                    $elem: {  //投影数组元素的x和y
                      x: 1,
                      z: 1
                    }
                  }
                }
            }
            """;

    @Test
    void test() {
        DsonArray<String> value = Dsons.project(dsonString, projectInfo).asArray();
        System.out.println(Dsons.toCollectionDson(value));
    }
}