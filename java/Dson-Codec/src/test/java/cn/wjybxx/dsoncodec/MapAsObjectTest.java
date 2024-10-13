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

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * 测试Map按照Object类型编码
 *
 * @author wjybxx
 * date - 2023/9/13
 */
public class MapAsObjectTest {

    @Test
    void test() {
        ConverterOptions options = ConverterOptions.newBuilder()
                .setWriteMapAsDocument(true)
                .build();

        DsonConverter converter = new DsonConverterBuilder()
                .setOptions(options)
                .build();

        Map<String, Object> map = new LinkedHashMap<>();
        map.put("one", "1");
        map.put("two", 2.0); // 默认解码是double

        String dson = converter.writeAsDson(map);
//        System.out.println(dson);

        LinkedHashMap<String, Object> copied = converter.readFromDson(dson, TypeInfo.STRING_LINKED_HASHMAP);
        Assertions.assertEquals(map, copied);
    }
}
