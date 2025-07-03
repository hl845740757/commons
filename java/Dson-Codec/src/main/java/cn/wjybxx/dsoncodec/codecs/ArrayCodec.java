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

import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;

import javax.annotation.Nonnull;
import java.lang.reflect.Array;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Supplier;

/**
 * 该实例仅支持引用类型数组，
 * 基础类型数组走定制Codec实现。
 *
 * @author wjybxx
 * date - 2024/9/25
 */
public final class ArrayCodec<T> implements DsonCodec<T[]> {

    private final TypeInfo encoderType;
    private final TypeInfo elementTypeInfo;

    public ArrayCodec(TypeInfo encoderType) {
        assert encoderType.isArrayType();
        this.encoderType = encoderType;
        this.elementTypeInfo = encoderType.getComponentType(); // 缓存实例
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T[] inst, TypeInfo declaredType, ObjectStyle style) {
        // declaredType只影响inst是否写入类型，不影响数组元素是否写入类型
        TypeInfo elementTypeInfo = this.elementTypeInfo;

        writer.writeStartArray(style, encoderType, declaredType, inst.length);
        for (int i = 0; i < inst.length; i++) {
            writer.writeObject(null, inst[i], elementTypeInfo);
        }
        writer.writeEndArray();
    }

    @Override
    public T[] readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends T[]> factory) {
        TypeInfo elementTypeInfo = this.elementTypeInfo;
        // count非精确值，不可以直接创建数组
        int count = reader.readStartArray();
        List<T> result = new ArrayList<>(count);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            T value = reader.readObject(null, elementTypeInfo, null);
            result.add(value);
        }
        reader.readEndArray();

        @SuppressWarnings("unchecked") T[] array = (T[]) Array.newInstance(elementTypeInfo.rawType, result.size());
        result.toArray(array);
        return array;
    }
}
