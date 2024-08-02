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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.List;
import java.util.Random;
import java.util.function.Supplier;

/**
 * 测试中间路由节点不解码，直到目的地后解码是否正确
 *
 * @author wjybxx
 * date 2023/4/4
 */
public class LazyCodecTest {

    @Test
    void testLazyCodec() {
        TypeMetaRegistry typeMetaRegistry = TypeMetaRegistries.fromMetas(
                TypeMeta.of(MyStruct.class, ObjectStyle.INDENT, "MyStruct")
        );
        ConverterOptions options = ConverterOptions.newBuilder()
                .setRandomRead(false)
                .build();

        Random random = new Random();
        NestStruct nestStruct = new NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);
        MyStruct myStruct = new MyStruct(RandomStringUtils.random(10), nestStruct);

        // 源端
        final byte[] bytesSource;
        {
            DefaultDsonConverter converter = DefaultDsonConverter.newInstance(
                    typeMetaRegistry,
                    List.of(new MyStructCodec(Role.SOURCE)),
                    options);
            bytesSource = converter.write(myStruct);
        }

        final byte[] routerBytes;
        // 模拟转发 -- 读进来再写
        {
            DefaultDsonConverter converter = DefaultDsonConverter.newInstance(
                    typeMetaRegistry,
                    List.of(new MyStructCodec(Role.ROUTER)),
                    options);
            routerBytes = converter.write(converter.read(bytesSource));
        }

        // 终端
        MyStruct destStruct;
        {
            DefaultDsonConverter converter = DefaultDsonConverter.newInstance(
                    typeMetaRegistry,
                    List.of(new MyStructCodec(Role.DESTINATION)),
                    options);
            destStruct = (MyStruct) converter.read(routerBytes);
        }
        Assertions.assertEquals(myStruct, destStruct);
    }

    private enum Role {
        SOURCE,
        ROUTER,
        DESTINATION
    }

    private static class MyStructCodec implements DsonCodec<MyStruct> {

        private final Role role;

        private MyStructCodec(Role role) {
            this.role = role;
        }

        @Nonnull
        @Override
        public Class<MyStruct> getEncoderClass() {
            return MyStruct.class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, MyStruct instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            writer.writeString("strVal", instance.strVal);
            if (role == Role.ROUTER) {
                writer.writeValueBytes("nestStruct", DsonType.OBJECT, (byte[]) instance.nestStruct);
            } else {
                // 不在编码器里，定制写
                NestStruct nestStruct = (NestStruct) instance.nestStruct;
                writer.writeStartObject("nestStruct", nestStruct, TypeInfo.of(NestStruct.class));
                {
                    writer.writeInt("intVal", nestStruct.intVal);
                    writer.writeLong("longVal", nestStruct.longVal);
                    writer.writeFloat("floatVal", nestStruct.floatVal);
                    writer.writeDouble("doubleVal", nestStruct.doubleVal);
                }
                writer.writeEndObject();
            }
        }

        @Override
        public MyStruct readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends MyStruct> factory) {
            String strVal = reader.readString("strVal");
            Object nestStruct;
            if (role == Role.ROUTER) {
                nestStruct = reader.readValueAsBytes("nestStruct");
            } else {
                reader.readStartObject("nestStruct");
                nestStruct = new NestStruct(
                        reader.readInt("intVal"),
                        reader.readLong("longVal"),
                        reader.readFloat("floatVal"),
                        reader.readDouble("doubleVal"));
                reader.readEndObject();
            }
            return new MyStruct(strVal, nestStruct);
        }
    }

    private record NestStruct(int intVal, long longVal, float floatVal, double doubleVal) {

    }

    private record MyStruct(String strVal, Object nestStruct) {

    }

}