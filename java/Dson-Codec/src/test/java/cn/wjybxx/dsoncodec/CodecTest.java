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

import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Random;

/**
 * 基础读写测试
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class CodecTest {

    private CodecStructs.MyStruct myStruct;

    @BeforeEach
    void setUp() {
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

        this.myStruct = myStruct;
    }

    @Test
    void docCodecTest() {
        ConverterOptions options = ConverterOptions.newBuilder()
                .setWriteMapAsDocument(true)
                .setWriteEnumAsString(true)
                .build();
        DsonConverter converter = new DsonConverterBuilder()
                .addTypeMetas(
                        TypeMeta.of(CodecStructs.MyStruct.class, ObjectStyle.INDENT, "MyStruct"),
                        TypeMeta.of(Sex.class, ObjectStyle.INDENT, "Sex")
                ).addCodecs(
                        new CodecStructs.MyStructCodec()
                ).setOptions(options)
                .build();
        String dsonString = converter.writeAsDson(myStruct);
        System.out.println(dsonString);

        TypeInfo typeInfo = TypeInfo.of(CodecStructs.MyStruct.class);
        CodecStructs.MyStruct clonedObject = converter.cloneObject(myStruct, typeInfo, typeInfo);
        Assertions.assertEquals(myStruct, clonedObject);
    }

}