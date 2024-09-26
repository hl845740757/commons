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

package cn.wjybxx.dsoncodec.codecs;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/9/25
 */
public class ArrayCodec<T> implements DsonCodec<T> {

    private final TypeInfo typeInfo;

    public ArrayCodec(TypeInfo typeInfo) {
        assert typeInfo.isArray();
        this.typeInfo = typeInfo;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo declaredType, ObjectStyle style) {
        // declaredType只影响inst是否写入类型，不影响数组元素是否写入类型，但Java是伪泛型，我们尝试从用户信息中获取泛型信息
        TypeInfo eleDeclaredType = getEleDeclaredType(declaredType);

        // 基础类型数组被特殊处理了，因此这里一定能强转 -- 强转以避免反射接口
        Object[] array = (Object[]) instance;
        writer.writeStartArray(instance, declaredType, style);
        for (int i = 0; i < array.length; i++) {
            writer.writeObject(null, array[i], eleDeclaredType);
        }
        writer.writeEndArray();
    }

    private TypeInfo getEleDeclaredType(TypeInfo declaredType) {
        if (typeInfo.hasGenericArgs()) {
            return typeInfo.getGenericArgument(0);
        }
        return declaredType.isGenericType() ? declaredType.getGenericArgument(0) : TypeInfo.OBJECT;
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends T> factory) {
        TypeInfo eleDeclaredType = getEleDeclaredType(declaredType);

        // 由于长度未知，只能先存储为List再转...
        List<Object> result = new ArrayList<>();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            Object value = reader.readObject(null, eleDeclaredType, null);
            result.add(value);
        }

        @SuppressWarnings("unchecked") Class<T> arrayType = (Class<T>) typeInfo.rawType;
        return DsonConverterUtils.convertList2Array(result, arrayType);
    }
}
