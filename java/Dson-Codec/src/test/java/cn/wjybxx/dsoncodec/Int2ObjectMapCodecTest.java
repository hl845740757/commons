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
import cn.wjybxx.dsoncodec.codecs.CollectionCodec;
import cn.wjybxx.dsoncodec.codecs.MapCodec;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;
import it.unimi.dsi.fastutil.ints.IntArrayList;
import it.unimi.dsi.fastutil.ints.IntList;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

/**
 * @author wjybxx
 * date - 2024/5/17
 */
public class Int2ObjectMapCodecTest {

    private static DsonConverter converter;

    @BeforeEach
    void setUp() {
        converter = DefaultDsonConverter.newInstance(
                TypeMetaRegistries.fromMetas(
                        TypeMeta.of(Int2ObjectMap.class, ObjectStyle.INDENT),
                        TypeMeta.of(Int2ObjectOpenHashMap.class, ObjectStyle.INDENT),
                        TypeMeta.of(IntList.class, ObjectStyle.FLOW),
                        TypeMeta.of(IntArrayList.class, ObjectStyle.FLOW)
                ),
                List.of(
                        new MapCodec<>(Int2ObjectMap.class, Int2ObjectOpenHashMap::new),
                        new MapCodec<>(Int2ObjectOpenHashMap.class, Int2ObjectOpenHashMap::new),
                        new CollectionCodec<>(IntList.class, IntArrayList::new),
                        new CollectionCodec<>(IntArrayList.class, IntArrayList::new)
                ),
                ConverterOptions.DEFAULT.toBuilder()
                        .setWriteMapAsDocument(true)
                        .build());
    }

    @Test
    void testInt2ObjMap() {
        Int2ObjectMap<String> srcMap = new Int2ObjectOpenHashMap<>();
        srcMap.put(1, "a");
        srcMap.put(2, "b");
        srcMap.put(3, "3");

        @SuppressWarnings("rawtypes") TypeInfo<Int2ObjectMap> typeInfo = TypeInfo.of(Int2ObjectMap.class, String.class);
        String dsonString = converter.writeAsDson(srcMap, typeInfo);
        System.out.println(dsonString);

        // 根据真实类型查询Codec
        @SuppressWarnings("unchecked") Int2ObjectMap<String> copied = converter.readFromDson(dsonString, typeInfo);
        Assertions.assertEquals(srcMap, copied);

        // 根据声明类型查询Codec
        @SuppressWarnings("unchecked") Int2ObjectMap<String> copied2 = converter.readFromDson(dsonString, typeInfo, Int2ObjectOpenHashMap::new);
        Assertions.assertEquals(srcMap, copied2);
    }

    @Test
    void testIntList() {
        IntList srcList = new IntArrayList(3);
        srcList.add(3);
        srcList.add(1);
        srcList.add(2);

        TypeInfo<IntList> typeInfo = TypeInfo.of(IntList.class);
        String dsonString = converter.writeAsDson(srcList, typeInfo);
        System.out.println(dsonString);

        IntList copied = converter.readFromDson(dsonString, typeInfo);
        Assertions.assertEquals(srcList, copied);

        // 根据声明类型查询Codec
        IntList copied2 = converter.readFromDson(dsonString, typeInfo, IntArrayList::new);
        Assertions.assertEquals(srcList, copied2);
    }
}
