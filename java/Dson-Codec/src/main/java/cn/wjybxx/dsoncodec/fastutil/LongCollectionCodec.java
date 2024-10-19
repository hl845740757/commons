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
import it.unimi.dsi.fastutil.longs.*;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/8
 */
public class LongCollectionCodec implements DsonCodec<LongCollection> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends LongCollection> factory;
    private final boolean isSet;

    public LongCollectionCodec(TypeInfo typeInfo) {
        this(typeInfo, null);
    }

    @SuppressWarnings("unchecked")
    public LongCollectionCodec(TypeInfo typeInfo, Supplier<? extends LongCollection> factory) {
        if (factory == null) {
            Class<? extends LongCollection> rawType = (Class<? extends LongCollection>) typeInfo.rawType;
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier(rawType);
        }
        this.typeInfo = typeInfo;
        this.factory = factory;
        this.isSet = LongSet.class.isAssignableFrom(typeInfo.rawType);
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    private LongCollection newCollection() {
        if (factory != null) return factory.get();
        return isSet ? new LongLinkedOpenHashSet() : new LongArrayList();
    }

    private static LongCollection toImmutable(LongCollection result) {
        if (result instanceof LongSet intSet) {
            return LongSets.unmodifiable(intSet);
        }
        return new LongImmutableList(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, LongCollection inst, TypeInfo declaredType, ObjectStyle style) {
        for (var itr = inst.iterator(); itr.hasNext(); ) {
            writer.writeLong(null, itr.nextLong());
        }
    }

    @Override
    public LongCollection readObject(DsonObjectReader reader, Supplier<? extends LongCollection> factory) {
        LongCollection result = factory != null ? factory.get() : newCollection();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readLong(null));
        }
        return reader.options().readAsImmutable ? toImmutable(result) : result;
    }
}
