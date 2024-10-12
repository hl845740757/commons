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
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import java.util.IdentityHashMap;

/**
 * {@code  IdentityHashMap.size}不是transient，也不可直接访问，我们需要将其跳过
 *
 * @author wjybxx
 * date 2023/4/14
 */
@DsonSerializable(
        skipFields = { // 这三种方式在这里等价
                "size",
                "IdentityHashMap.size",
                "java.util.IdentityHashMap.size"
        },
        annotations = DsonCodecScanIgnore.class)
public class CustomMapCodecTest<K, V> extends IdentityHashMap<K, V> {

    public CustomMapCodecTest(DsonObjectReader reader, TypeInfo typeInfo) {
    }

    public void beforeEncode(ConverterOptions options) {

    }

    public void writeObject(DsonObjectWriter writer) {
        TypeInfo encoderType = writer.getEncoderType();
        TypeInfo keyType = encoderType.genericArgs.get(0);
        TypeInfo valueType = encoderType.genericArgs.get(1);
        for (Entry<K, V> entry : this.entrySet()) {
            writer.writeObject(null, entry.getKey(), keyType);
            writer.writeObject(null, entry.getValue(), valueType);
        }
    }

    public void readObject(DsonObjectReader reader) {
        TypeInfo encoderType = reader.getEncoderType();
        TypeInfo keyType = encoderType.genericArgs.get(0);
        TypeInfo valueType = encoderType.genericArgs.get(1);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            K k = reader.readObject(null, keyType);
            V v = reader.readObject(null, valueType);
            put(k, v);
        }
    }

    public void afterDecode(ConverterOptions options) {

    }

}