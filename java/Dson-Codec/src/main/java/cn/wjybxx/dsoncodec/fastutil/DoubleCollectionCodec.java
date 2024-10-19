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
import it.unimi.dsi.fastutil.doubles.DoubleArrayList;
import it.unimi.dsi.fastutil.doubles.DoubleCollection;
import it.unimi.dsi.fastutil.doubles.DoubleImmutableList;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class DoubleCollectionCodec implements DsonCodec<DoubleCollection> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends DoubleCollection> factory;

    public DoubleCollectionCodec(TypeInfo typeInfo) {
        this(typeInfo, null);
    }

    @SuppressWarnings("unchecked")
    public DoubleCollectionCodec(TypeInfo typeInfo, Supplier<? extends DoubleCollection> factory) {
        if (factory == null) {
            Class<? extends DoubleCollection> rawType = (Class<? extends DoubleCollection>) typeInfo.rawType;
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

    protected DoubleCollection newCollection() {
        if (factory != null) {
            return factory.get();
        }
        return new DoubleArrayList();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, DoubleCollection inst, TypeInfo declaredType, ObjectStyle style) {
        for (var itr = inst.iterator(); itr.hasNext(); ) {
            writer.writeDouble(null, itr.nextDouble());
        }
    }

    @Override
    public DoubleCollection readObject(DsonObjectReader reader, Supplier<? extends DoubleCollection> factory) {
        DoubleCollection result = factory != null ? factory.get() : newCollection();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readDouble(null));
        }
        return reader.options().readAsImmutable ? new DoubleImmutableList(result) : result;
    }
}
