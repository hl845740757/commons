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
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;

/**
 * 测试TypeMeta动态化是否正确
 * <p>
 * 注意：为方便跨语言交互
 * {@link ArrayList}的clsName是{@literal List}，
 * 而{@link List}的clsName是{@literal IList}
 *
 * @author wjybxx
 * date - 2024/10/15
 */
public class TypeMetaRegistryTest {

    private static DynamicTypeMetaRegistry registry;

    @BeforeEach
    void setUp() {
        registry = new DynamicTypeMetaRegistry(TypeMetaConfig.DEFAULT);
    }

    @Test
    void testGeneric() {
        TypeInfo type = TypeInfo.of(ArrayList.class, TypeInfo.STRING);
        TypeMeta typeMeta = registry.ofType(type);
        Assertions.assertNotNull(typeMeta);

        TypeMeta typeMeta2 = registry.ofName("List[s]");
        Assertions.assertSame(typeMeta, typeMeta2);

        // 它俩无法简单解析到同一个TypeMeta？
        // 确实无法，在类型支持别名的情况下，除非我们注册所有的组合情况 -- 实际上没有必要，指向不同的TypeMeta不影响正确性
        TypeMeta typeMeta3 = registry.ofName("List`1[s]");
        // Assert.That(typeMeta3, Is.SameAs(typeMeta));
        Assertions.assertNotNull(typeMeta3);
        Assertions.assertEquals(type, typeMeta3.typeInfo); // 由于TypeMeta可能不同，因此TypeInfo可能不是同一个实例

        // 最终会指向同一个TypeMeta -- 我们实现了动态合并
        typeMeta = registry.ofType(type);
        typeMeta2 = registry.ofName("List[s]");
        typeMeta3 = registry.ofName("List`1[s]");
        Assertions.assertSame(typeMeta, typeMeta2);
        Assertions.assertSame(typeMeta, typeMeta3);
    }

    @Test
    public void testArray() {
        TypeInfo type = TypeInfo.ARRAY_STRING;
        TypeMeta typeMeta = registry.ofType(type);
        Assertions.assertNotNull(typeMeta);

        TypeMeta typeMeta2 = registry.ofName("s[]");
        Assertions.assertSame(typeMeta, typeMeta2);
    }

    /** 通过Type查找TypeMeta */
    @Test
    public void testGenericArray() {
        TypeInfo type = TypeInfo.of(ArrayList.class,
                        TypeInfo.of(ArrayList.class, TypeInfo.STRING))
                .makeArrayType();
        TypeMeta typeMeta = registry.ofType(type);
        Assertions.assertNotNull(typeMeta);

        String clsName = "List[List[s]][]";
        Assertions.assertEquals(clsName, typeMeta.mainClsName());

        TypeMeta typeMeta2 = registry.ofName(clsName);
        Assertions.assertSame(typeMeta, typeMeta2);
    }

    /** 通过clsName查找TypeMeta */
    @Test
    public void testGenericArray2() {
        String clsName = "List[List[i]][]";
        TypeMeta typeMeta = registry.ofName(clsName);
        Assertions.assertNotNull(typeMeta);
        Assertions.assertEquals(clsName, typeMeta.mainClsName());

        TypeInfo type = TypeInfo.of(ArrayList.class,
                        TypeInfo.of(ArrayList.class, TypeInfo.INT))
                .makeArrayType();
        TypeMeta typeMeta2 = registry.ofType(type);
        Assertions.assertSame(typeMeta, typeMeta2);
    }
}
