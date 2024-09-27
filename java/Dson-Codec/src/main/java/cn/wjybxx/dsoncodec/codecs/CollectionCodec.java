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
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Supplier;

/**
 * 集合解码器，动态构造
 *
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DsonCodecScanIgnore
public class CollectionCodec<T extends Collection> implements DsonCodec<T> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends T> factory;
    private final TypeInfo elementTypeInfo;

    @SuppressWarnings("unchecked")
    public CollectionCodec(TypeInfo typeInfo) {
        this.typeInfo = Objects.requireNonNull(typeInfo);

        Class<T> rawType = (Class<T>) typeInfo.rawType;
        this.factory = DsonConverterUtils.tryNoArgConstructorToSupplier(rawType);
        this.elementTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Collection.class, "E");
    }

    @SuppressWarnings("unchecked")
    public CollectionCodec(TypeInfo typeInfo, Supplier<? extends T> factory) {
        this.typeInfo = Objects.requireNonNull(typeInfo);
        this.factory = factory;

        Class<T> rawType = (Class<T>) typeInfo.rawType;
        this.elementTypeInfo = DsonConverterUtils.findTypeParameter(rawType, Collection.class, "E");
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    /** 允许重写 */
    @SuppressWarnings("unchecked")
    protected Collection<Object> newCollection(TypeInfo typeInfo, Supplier<? extends T> factory) {
        if (factory != null) {
            return (Collection<Object>) factory.get();
        }
        if (this.factory != null) {
            return (Collection<Object>) this.factory.get();
        }
        if (Set.class.isAssignableFrom(typeInfo.rawType)) {
            return new LinkedHashSet<>();
        }
        return new ArrayList<>();
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo declaredType, ObjectStyle style) {
        // 理论上declaredType只影响当前inst是否写入类型，因此应当优先从inst的真实类型中查询K,V的类型，但Java是伪泛型...
        TypeInfo elementTypeInfo = this.elementTypeInfo;
        for (Object e : instance) {
            writer.writeObject(null, e, elementTypeInfo, null);
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends T> factory) {
        TypeInfo elementTypeInfo = this.elementTypeInfo;
        Collection<Object> result = newCollection(declaredType, factory);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, elementTypeInfo));
        }
        CollectionConverter collectionConverter = reader.options().collectionConverter;
        if (collectionConverter != null) {
            result = collectionConverter.convertCollection(declaredType, result);
        }
        @SuppressWarnings("unchecked") Class<T> rawType = (Class<T>) typeInfo.rawType;
        return rawType.cast(result);
    }

}