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
 * @author wjybxx
 * date 2023/4/4
 */
@SuppressWarnings("rawtypes")
@DsonCodecScanIgnore
public class CollectionCodec<T extends Collection> implements DsonCodec<T> {

    final Class<T> clazz;
    final Supplier<? extends T> factory;

    @SuppressWarnings("unchecked")
    public CollectionCodec() {
        this.clazz = (Class<T>) Collection.class;
        this.factory = null;
    }

    public CollectionCodec(Class<T> clazz, Supplier<? extends T> factory) {
        this.clazz = Objects.requireNonNull(clazz);
        this.factory = factory;
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return clazz;
    }

    @SuppressWarnings("unchecked")
    private Collection<Object> newCollection(TypeInfo<?> typeInfo, Supplier<? extends T> factory) {
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

    private static TypeInfo<?> getElementTypeInfo(TypeInfo<?> typeInfo) {
        if (typeInfo.isGenericType()) {
            return typeInfo.getGenericArgument(0);
        }
        return DsonConverterUtils.getElementActualTypeInfo(typeInfo.rawType);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        // 理论上declaredType只影响当前inst是否写入类型，因此应当优先从inst的真实类型中查询K,V的类型，但Java是伪泛型...
        TypeInfo<?> componentArgInfo = getElementTypeInfo(typeInfo);
        for (Object e : instance) {
            writer.writeObject(null, e, componentArgInfo, null);
        }
    }

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends T> factory) {
        TypeInfo<?> componentArgInfo = getElementTypeInfo(typeInfo);
        Collection<Object> result = newCollection(typeInfo, factory);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, componentArgInfo));
        }
        CollectionConverter collectionConverter = reader.options().collectionConverter;
        if (collectionConverter != null) {
            result = collectionConverter.convertCollection(typeInfo, result);
        }
        return clazz.cast(result);
    }

}