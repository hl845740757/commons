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

import cn.wjybxx.base.TypeInfo;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * 测试Map按照Object类型编码
 *
 * @author wjybxx
 * date - 2023/9/13
 */
public class MapAsObjectTest {

    private static DsonConverter converter;

    @BeforeAll
    static void setUp() {
        ConverterOptions options = ConverterOptions.newBuilder()
                .setMapEncodePolicy(MapEncodePolicy.DOCUMENT)
                .build();

        converter = new DsonConverterBuilder()
                .setOptions(options)
                .build();
    }

    @Test
    void test() {

        Map<String, Object> map = new LinkedHashMap<>();
        map.put("one", "1");
        map.put("two", 2.0); // 默认解码是double

        TypeInfo declaredType = TypeInfo.of(Map.class, TypeInfo.STRING, TypeInfo.OBJECT);
        String dson = converter.writeAsDson(map, declaredType);
        System.out.println(dson);

        LinkedHashMap<String, Object> copied = converter.readFromDson(dson, declaredType);
        Assertions.assertEquals(map, copied);
    }

    @Test
    void testInt() {
        Map<Integer, Object> map = new LinkedHashMap<>();
        map.put(1, "1");
        map.put(2, 2.0); // 默认解码是double

        TypeInfo declaredType = TypeInfo.of(Map.class, TypeInfo.INT, TypeInfo.OBJECT);
        String dson = converter.writeAsDson(map, declaredType);
        System.out.println(dson);

        LinkedHashMap<Integer, Object> copied = converter.readFromDson(dson, declaredType);
        Assertions.assertEquals(map, copied);
    }
}
