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
import it.unimi.dsi.fastutil.ints.*;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class IntCollectionCodec implements DsonCodec<IntCollection> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends IntCollection> factory;
    private final boolean isSet;

    @SuppressWarnings("unchecked")
    public IntCollectionCodec(TypeInfo typeInfo, Supplier<? extends IntCollection> factory) {
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends IntCollection>) typeInfo.rawType);
        }
        this.typeInfo = Objects.requireNonNull(typeInfo);
        this.factory = factory;
        this.isSet = IntSet.class.isAssignableFrom(typeInfo.rawType);
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    protected IntCollection newCollection() {
        if (factory != null) return factory.get();
        return isSet ? new IntLinkedOpenHashSet() : new IntArrayList();
    }

    private static IntCollection toImmutable(IntCollection result) {
        if (result instanceof IntSet intSet) {
            return IntSets.unmodifiable(intSet);
        }
        return new IntImmutableList(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, IntCollection inst, TypeInfo declaredType, ObjectStyle style) {
        for (var itr = inst.iterator(); itr.hasNext(); ) {
            writer.writeInt(null, itr.nextInt());
        }
    }

    @Override
    public IntCollection readObject(DsonObjectReader reader, Supplier<? extends IntCollection> factory) {
        IntCollection result = factory != null ? factory.get() : newCollection();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readInt(null));
        }
        return reader.options().readAsImmutable ? toImmutable(result) : result;
    }
}