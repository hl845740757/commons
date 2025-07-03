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

package cn.wjybxx.dsoncodec.fastutil;

import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonConverterUtils;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import it.unimi.dsi.fastutil.floats.*;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class FloatCollectionCodec implements DsonCodec<FloatCollection> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends FloatCollection> factory;

    public FloatCollectionCodec(TypeInfo typeInfo) {
        this(typeInfo, null);
    }

    @SuppressWarnings("unchecked")
    public FloatCollectionCodec(TypeInfo typeInfo, Supplier<? extends FloatCollection> factory) {
        Class<? extends FloatCollection> rawType = (Class<? extends FloatCollection>) typeInfo.rawType;
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier(rawType);
        }
        this.typeInfo = typeInfo;
        this.factory = factory;
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    protected FloatCollection newCollection(Supplier<? extends FloatCollection> userFactory, int count) {
        if (userFactory != null) return userFactory.get();
        if (factory != null) return factory.get();
        return new FloatArrayList(count);
    }

    private static FloatCollection toImmutable(FloatCollection result, TypeInfo declaredType) {
        if (!declaredType.rawType.isInterface()) return result;
        if (result instanceof FloatList) {
            return new FloatImmutableList(result);
        }
        return FloatCollections.unmodifiable(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, FloatCollection inst, TypeInfo declaredType, ObjectStyle style) {
        writer.writeStartArray(style, getEncoderType(), declaredType, inst.size());
        for (var itr = inst.iterator(); itr.hasNext(); ) {
            writer.writeFloat(null, itr.nextFloat());
        }
        writer.writeEndArray();
    }

    @Override
    public FloatCollection readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends FloatCollection> factory) {
        int count = reader.readStartArray();
        FloatCollection result = newCollection(factory, count);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readFloat(null));
        }
        reader.readEndArray();
        return reader.options().readAsImmutable ? toImmutable(result, declaredType) : result;
    }
}
