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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;
import it.unimi.dsi.fastutil.floats.FloatArrayList;
import it.unimi.dsi.fastutil.floats.FloatCollection;
import it.unimi.dsi.fastutil.floats.FloatImmutableList;

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

    protected FloatCollection newCollection() {
        if (factory != null) {
            return factory.get();
        }
        return new FloatArrayList();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, FloatCollection inst, TypeInfo declaredType, ObjectStyle style) {
        for (var itr = inst.iterator(); itr.hasNext(); ) {
            writer.writeFloat(null, itr.nextFloat());
        }
    }

    @Override
    public FloatCollection readObject(DsonObjectReader reader, Supplier<? extends FloatCollection> factory) {
        FloatCollection result = factory != null ? factory.get() : newCollection();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readFloat(null));
        }
        return reader.options().readAsImmutable ? new FloatImmutableList(result) : result;
    }
}
