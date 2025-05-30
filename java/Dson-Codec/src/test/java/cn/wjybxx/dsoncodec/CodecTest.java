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
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.*;

/**
 * 基础读写测试
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class CodecTest {

    private static DsonConverter converter;

    @BeforeAll
    static void setUp() {
        ConverterOptions options = ConverterOptions.newBuilder()
                .setMapEncodePolicy(MapEncodePolicy.DOCUMENT)
                .setWriteEnumAsString(true)
                .build();
        converter = new DsonConverterBuilder()
                .addTypeMetas(
                        TypeMeta.of(CodecStructs.MyStruct.class, ObjectStyle.INDENT, "MyStruct"),
                        TypeMeta.of(Sex.class, ObjectStyle.INDENT, "Sex"),
                        TypeMeta.of(Vector3.class, ObjectStyle.FLOW, "Vector3", "V3")
                ).addCodecs(
                        new CodecStructs.MyStructCodec(),
                        new Vector3.Vector3Codec()
                ).setOptions(options)
                .build();
    }

    @Test
    public void TestDictionaryVector3() {
        TestDictionaryVector3(MapEncodePolicy.ARRAY);
    }

    @Test
    public void TestDictionaryVector3AsDocument() {
        TestDictionaryVector3(MapEncodePolicy.DOCUMENT);
    }

    @Test
    public void TestDictionaryVector3AsPairDocument() {
        TestDictionaryVector3(MapEncodePolicy.PAIR_AS_DOCUMENT);
    }

    @Test
    public void TestDictionaryVector3AsPariArray() {
        TestDictionaryVector3(MapEncodePolicy.PAIR_AS_ARRAY);
    }

    private void TestDictionaryVector3(MapEncodePolicy mapEncodePolicy) {
        Map<Integer, Vector3> dictionary = new LinkedHashMap<Integer, Vector3>();
        for (int i = 1; i <= 5; i++) {
            dictionary.put(i, new Vector3(i - 0.5f, i, i + 0.5f));
        }

        ConverterOptions.Builder builder = converter.options().toBuilder();
        builder.setMapEncodePolicy(mapEncodePolicy);
        DsonConverter converter2 = converter.withOptions(builder.build());

        TypeInfo declaredType = TypeInfo.of(Map.class, TypeInfo.BOXED_INT, TypeInfo.of(Vector3.class));
        // 和C#的测试不同，我们这里需要传入Key和Value的信息，否则Key会被识别为Object
        String dson = converter2.writeAsDson(dictionary, declaredType);
        System.out.println(dson);

        Map<Integer, Vector3> copied = converter2.readFromDson(dson, declaredType);
        Assertions.assertEquals(copied, dictionary);
    }

    @Test
    void docCodecTest() {
        CodecStructs.MyStruct myStruct = createStruct();
        String dsonString = converter.writeAsDson(myStruct);
        System.out.println(dsonString);

        TypeInfo typeInfo = TypeInfo.of(CodecStructs.MyStruct.class);
        CodecStructs.MyStruct clonedObject = converter.cloneObject(myStruct, typeInfo, typeInfo);
        Assertions.assertEquals(myStruct, clonedObject);
    }

    private static CodecStructs.MyStruct createStruct() {
        Random random = new Random();
        CodecStructs.NestStruct nestStruct = new CodecStructs.NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);

        CodecStructs.MyStruct myStruct = new CodecStructs.MyStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100,
                random.nextBoolean(),
                RandomStringUtils.random(10),
                new byte[5],
                Sex.MALE,
                new HashMap<>(),
                new ArrayList<>(),
                nestStruct);

        random.nextBytes(myStruct.bytes);

        myStruct.list.add(RandomStringUtils.random(5));
        myStruct.list.add(RandomStringUtils.random(7));

        myStruct.map.put(String.valueOf(myStruct.intVal), random.nextFloat() * 100);
        myStruct.map.put(String.valueOf(myStruct.longVal), random.nextDouble() * 100);
        return myStruct;
    }
}