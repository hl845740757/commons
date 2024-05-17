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

package cn.wjybxx.base.reflect;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/5/17
 */
public class TypeParameterFinderTest {


    @Test
    void test() {
        Class<?> listTypeVar = TypeParameterFinder.findTypeParameterUnsafe(StringList.class, ArrayList.class, "E");
        Assertions.assertEquals(String.class, listTypeVar);

        Class<?> supplierTypeVar = TypeParameterFinder.findTypeParameterUnsafe(StringSupplier.class, Supplier.class, "T");
        Assertions.assertEquals(String.class, supplierTypeVar);
    }

    private static class StringList extends ArrayList<String> {

    }

    private static class StringSupplier implements Supplier<String> {
        @Override
        public String get() {
            return "StringSupplier";
        }
    }
}
