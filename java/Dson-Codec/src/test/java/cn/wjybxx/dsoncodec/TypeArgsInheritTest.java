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

import java.util.ArrayList;
import java.util.List;

/**
 * 测试泛型参数继承
 *
 * @author wjybxx
 * date - 2024/5/17
 */
public class TypeArgsInheritTest {

    @Test
    void testList() {
        Assertions.assertTrue(DsonConverterUtils.canInheritTypeArgs(ArrayList.class, List.class));

        // typeInfo必须有泛型参数时才可继承
        Assertions.assertFalse(DsonConverterUtils.canInheritTypeArgs(ArrayList.class, TypeInfo.of(List.class)));
        Assertions.assertTrue(DsonConverterUtils.canInheritTypeArgs(ArrayList.class, TypeInfo.of(List.class, String.class)));
    }
}
