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

package cn.wjybxx.dsoncodec;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2024/4/25
 */
public class ClassNameTest {

    private static final String genericTypeName = """
            System.Collections.Generic.Dictionary`2
            [
                System.Int32,
                System.Collections.Generic.Dictionary`2
                [
                    Wjybxx.Dson.Tests.BeanExample,
                    System.String
                ]
            ]
            """;

    private static final String genericTypeName2 = """
            System.Collections.Generic.Dictionary`2
            [
                System.Int32,
                System.String[]
            ][]
            """;

    /** 测试缩写名称 */
    private static final String genericTypeName3 = "List`1[s]";

    @Test
    void parseTypeNameTest() {
        parseTest(genericTypeName);
        parseTest(genericTypeName2);
        parseTest(genericTypeName3);
    }

    private static void parseTest(String dsonClassName) {
        ClassName className = ClassName.parse(dsonClassName);
        String formatted = className.toString();
        System.out.println(formatted);

        ClassName cloned = ClassName.parse(formatted); // 解析格式化导出的文本
        Assertions.assertEquals(className, cloned);
    }
}